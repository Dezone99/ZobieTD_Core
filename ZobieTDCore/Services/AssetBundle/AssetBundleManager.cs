using System;
using System.Collections.Generic;
using System.IO;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCore.Services.AssetBundle
{


    public class AssetBundleManager
    {
        public static AssetBundleManager Instance { get; } = new AssetBundleManager();
        private readonly IUnityEngineContract unityEngineContract =
           ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initialized");

        private Dictionary<string, IAssetBundleContract> loadedBundles = new Dictionary<string, IAssetBundleContract>();

        #region Sprite caching properties
        private Dictionary<IAssetBundleContract, AssetRef[]> bundleToAllLoadedSprites = new Dictionary<IAssetBundleContract, AssetRef[]>();
        private Dictionary<AssetRef[], IAssetBundleContract> allLoadedSpritesToBundle = new Dictionary<AssetRef[], IAssetBundleContract>();
        private Dictionary<AssetRef, IAssetBundleContract> singleSpriteToBundle = new Dictionary<AssetRef, IAssetBundleContract>();
        private Dictionary<(string bundleName, string spriteName), AssetRef> cachedSingleSpriteAssets = new Dictionary<(string bundleName, string spriteName), AssetRef>();
        #endregion

        private AssetBundleUsageManager assetBundleUsageManager = new AssetBundleUsageManager();

        public AssetRef LoadSingleSubSpriteAsset(string bundleName, string spriteName)
        {
            var key = (bundleName, spriteName);

            if (cachedSingleSpriteAssets.TryGetValue(key, out var cachedRef))
            {
                return cachedRef;
            }

            var bundle = LoadAssetBundle(bundleName);
            var asset = bundle.LoadSingleSubSpriteAsset(spriteName);
            var assetRef = new AssetRef(asset);

            // Start Caching
            singleSpriteToBundle[assetRef] = bundle;
            cachedSingleSpriteAssets[key] = assetRef;
            // End Caching

            assetBundleUsageManager.RegisterAssetReference(assetRef, bundle);
            return assetRef;
        }

        /// <summary>
        /// Load toàn bộ asset từ 1 bundle. Trả về mảng các IAssetReference.
        /// Mỗi asset là một sprite đơn.
        /// </summary>
        public AssetRef[] LoadAllSubSpriteAsset(string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);

            if (bundleToAllLoadedSprites.TryGetValue(bundle, out var allLoadedSpritesRef))
            {
                return allLoadedSpritesRef;
            }

            object raw = bundle.LoadAllSubSpriteAssets();
            if (raw is Array array)
            {
                var rawLenght = array.Length;
                var assetRefs = new AssetRef[rawLenght];
                var index = 0;
                foreach (var asset in array)
                {
                    var assetRef = new AssetRef(asset);
                    assetRefs[index++] = assetRef;
                }
                bundleToAllLoadedSprites[bundle] = assetRefs;
                allLoadedSpritesToBundle[assetRefs] = bundle;
                assetBundleUsageManager.RegisterAssetReference(assetRefs, bundle);
                return assetRefs;
            }
            else
            {
                throw new InvalidOperationException("LoadAllSubSpriteAssets must return sprite array!");
            }
        }

        public void ReleaseSpriteAssetRef(AssetRef assetRef)
        {
            if (assetRef.Ref != null &&
                singleSpriteToBundle.TryGetValue(assetRef, out var bundle))
            {
                assetBundleUsageManager.UnregisterAssetReference(assetRef);

                var assetName = unityEngineContract.GetUnityObjectName(assetRef.Ref);
                var cachedSingleSpriteAssetsKey = (bundle.BundleName, assetName);
                cachedSingleSpriteAssets.Remove(cachedSingleSpriteAssetsKey);
                singleSpriteToBundle.Remove(assetRef);
            }
        }

        public void ReleaseSpriteAssetRef(AssetRef[] assetRef)
        {
            if (allLoadedSpritesToBundle.TryGetValue(assetRef, out var bundle))
            {
                assetBundleUsageManager.UnregisterAssetReference(assetRef);

                bundleToAllLoadedSprites.Remove(bundle);
                allLoadedSpritesToBundle.Remove(assetRef);
            }
        }


        public string? GetBundleNameOfSprite(AssetRef assetRef)
        {
            return singleSpriteToBundle.TryGetValue(assetRef, out var b) ? b.BundleName : null;
        }

        public void UpdateCachedAssetBundle()
        {
            var unityEngineContract = ContractManager.Instance.UnityEngineContract
                ?? throw new InvalidOperationException("Core engine was not initalized");

            foreach (var bundleName in assetBundleUsageManager.GetNeedToUnloadBundle())
            {
                ForceUnloadBundle(bundleName);
            }
        }

        /// <summary>
        /// Trả về danh sách các bundle đang được load hiện tại.
        /// </summary>
        public List<string> GetLoadedBundles() => new List<string>(loadedBundles.Keys);

        private IAssetBundleContract LoadAssetBundle(string bundleName)
        {
            var unityEngineContract = ContractManager.Instance.UnityEngineContract
                ?? throw new InvalidOperationException("Core engine was not initalized");

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                var path = Path.Combine(unityEngineContract.StreamingAssetPath, bundleName);
                bundle = unityEngineContract.LoadAssetBundleFromFile(path);
                loadedBundles[bundleName] = bundle;
            }

            return bundle;
        }

        /// <summary>
        /// Giải phóng toàn bộ bundle khỏi bộ nhớ (force).
        /// Gọi khi chắc chắn không còn ref nào.
        /// </summary>
        private void ForceUnloadBundle(string bundleName)
        {
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllAsset: true);
                loadedBundles.Remove(bundleName);
            }
        }

        /// <summary>
        /// Tạo đối tượng mới cho test.
        /// </summary>
        internal static AssetBundleManager __MakeBundleManagerForTest() => new AssetBundleManager();

        /// <summary>
        /// Trả về đối tượng quản lý ref cho test.
        /// </summary>
        internal AssetBundleUsageManager __GetBundleUsageManagerForTest() => assetBundleUsageManager;
    }
}