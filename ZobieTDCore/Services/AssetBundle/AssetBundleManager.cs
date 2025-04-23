using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCore.Services.AssetBundle
{
    /// <summary>
    /// Quản lý quá trình load, cache và giải phóng asset từ asset bundle.
    /// Hỗ trợ cả asset đơn và nhóm asset dạng sprite sheet.
    /// </summary>
    public class AssetBundleManager<T> where T : class
    {
        private readonly IUnityEngineContract unityEngineContract =
            ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initialized");

        private readonly Dictionary<string, IAssetBundleContract> loadedBundles = new Dictionary<string, IAssetBundleContract>();

        #region Sprite Caching
        private readonly Dictionary<IAssetBundleContract, AssetRef<T>[]> bundleToAllLoadedSprites = new Dictionary<IAssetBundleContract, AssetRef<T>[]>();
        private readonly Dictionary<AssetRef<T>[], IAssetBundleContract> allLoadedSpritesToBundle = new Dictionary<AssetRef<T>[], IAssetBundleContract>();
        private readonly Dictionary<AssetRef<T>, IAssetBundleContract> singleSpriteToBundle = new Dictionary<AssetRef<T>, IAssetBundleContract>();
        private readonly Dictionary<(string bundleName, string spriteName), AssetRef<T>> cachedSingleSpriteAssets = new Dictionary<(string bundleName, string spriteName), AssetRef<T>>();
        private readonly HashSet<(object assetOwner, string bundleName, object assetRef)> cachedAssetOwner = new HashSet<(object assetOwner, string bundleName, object assetRef)>();
        #endregion

        private readonly AssetBundleUsageManager assetBundleUsageManager = new AssetBundleUsageManager();

        /// <summary>
        /// Load 1 sprite đơn từ asset bundle, sử dụng caching và ref tracking theo assetOwner.
        /// </summary>
        public AssetRef<T> LoadSingleSubSpriteAsset(object assetOwner, string bundleName, string spriteName)
        {
            var key = (bundleName, spriteName);
            var bundle = LoadAssetBundle(bundleName);

            if (!cachedSingleSpriteAssets.TryGetValue(key, out var cachedRef))
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }

                var asset = bundle.LoadSingleSubAsset<T>(spriteName);
                if (asset == null)
                    throw new InvalidOperationException($"Failed to load asset with type {typeof(T).Name}");

                cachedRef = new AssetRef<T>(asset);

                singleSpriteToBundle[cachedRef] = bundle;
                cachedSingleSpriteAssets[key] = cachedRef;
            }

            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundleName, cachedRef);
            if (cachedAssetOwner.Add(assetOwnerKey))
            {
                assetBundleUsageManager.RegisterAssetReference<T>(cachedRef, bundle);
            }

            return cachedRef;
        }

        /// <summary>
        /// Load toàn bộ sprite trong 1 asset bundle (ví dụ: animation frames).
        /// Cache theo bundle và track ref theo assetOwner.
        /// </summary>
        public AssetRef<T>[] LoadAllSubSpriteAsset(object assetOwner, string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);

            if (!bundleToAllLoadedSprites.TryGetValue(bundle, out var allLoadedSpritesRef))
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }

                var assets = bundle.LoadAllSubAssets<T>();
                if (assets == null)
                    throw new InvalidOperationException($"Failed to load all sub assets with type {typeof(T).Name}");

                allLoadedSpritesRef = new AssetRef<T>[assets.Length];
                for (int i = 0; i < assets.Length; i++)
                {
                    var rawAsset = assets.GetValue(i);
                    if (!(rawAsset is T typedAsset))
                        throw new InvalidCastException($"Asset is not of type {typeof(T).Name}");
                    allLoadedSpritesRef[i] = new AssetRef<T>(typedAsset);

                    var assetName = unityEngineContract.GetUnityObjectName(typedAsset);
                    var cachedSingleSpriteAssetsKey = (bundleName, assetName);
                    singleSpriteToBundle[allLoadedSpritesRef[i]] = bundle;
                    cachedSingleSpriteAssets[cachedSingleSpriteAssetsKey] = allLoadedSpritesRef[i];
                }

                bundleToAllLoadedSprites[bundle] = allLoadedSpritesRef;
                allLoadedSpritesToBundle[allLoadedSpritesRef] = bundle;

                var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundleName, allLoadedSpritesRef);
                if (cachedAssetOwner.Add(assetOwnerKey))
                {
                    assetBundleUsageManager.RegisterAssetReference<T>(allLoadedSpritesRef, bundle);
                }
            }

            // When all asset are loaded, we can softly release the bundle
            bundle.Unload(false);

            return allLoadedSpritesRef;
        }

        /// <summary>
        /// Giải phóng 1 sprite đơn khỏi hệ thống, nếu assetOwner không còn giữ reference.
        /// </summary>
        public void ReleaseSpriteAssetRef(object assetOwner, AssetRef<T> assetRef)
        {
            if (assetRef.Ref != null && singleSpriteToBundle.TryGetValue(assetRef, out var bundle))
            {
                var assetOwnerKey = (assetOwner, bundle.BundleName, assetRef);
                if (cachedAssetOwner.Remove(assetOwnerKey))
                {
                    assetBundleUsageManager.UnregisterAssetReference<T>(assetRef);
                }
            }
        }

        /// <summary>
        /// Giải phóng nhóm sprite (animation) khỏi hệ thống, nếu assetOwner không còn giữ reference.
        /// </summary>
        public void ReleaseSpriteAssetRef(object assetOwner, AssetRef<T>[] assetRefs)
        {
            if (allLoadedSpritesToBundle.TryGetValue(assetRefs, out var bundle))
            {
                var assetOwnerKey = (assetOwner, bundle.BundleName, assetRefs);
                if (cachedAssetOwner.Remove(assetOwnerKey))
                {
                    assetBundleUsageManager.UnregisterAssetReference<T>(assetRefs);
                }
            }
        }

        /// <summary>
        /// Lấy tên bundle chứa sprite tương ứng.
        /// </summary>
        public string? GetBundleNameOfSprite(AssetRef<T> assetRef) =>
            singleSpriteToBundle.TryGetValue(assetRef, out var bundle) ? bundle.BundleName : null;

        /// <summary>
        /// Kiểm tra và unload các bundle không còn được sử dụng theo timeout từ AssetBundleUsageManager.
        /// </summary>
        public void UpdateCachedAssetBundle()
        {
            foreach (var bundleName in assetBundleUsageManager.GetNeedToUnloadBundle())
            {
                ForceUnloadBundle(bundleName);
            }
        }

        /// <summary>
        /// Trả về danh sách các bundle hiện đang được load.
        /// </summary>
        public List<string> GetLoadedBundles() => new List<string>(loadedBundles.Keys);

        /// <summary>
        /// Tạo khóa duy nhất cho assetOwner khi tracking asset (single hoặc assets).
        /// </summary>
        private (object assetOwner, string bundleName, object assetRef) MakeAssetOwnerKey(object assetOwner, string bundleName, object assetRef)
        {
            if (!(assetRef is AssetRef<T>) && !(assetRef is AssetRef<T>[]))
                throw new InvalidOperationException("assetRef must be AssetRef or AssetRef[]");

            return (assetOwner, bundleName, assetRef);
        }

        /// <summary>
        /// Load bundle từ StreamingAssets nếu chưa có trong cache.
        /// </summary>
        private IAssetBundleContract LoadAssetBundle(string bundleName)
        {
            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                var path = Path.Combine(unityEngineContract.StreamingAssetPath, bundleName);
                bundle = unityEngineContract.LoadAssetBundleFromFile(path);
                loadedBundles[bundleName] = bundle;
            }

            return bundle;
        }

        /// <summary>
        /// Force unload 1 bundle khỏi bộ nhớ, bất kể đang sử dụng hay không.
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
        /// Truy cập instance AssetBundleUsageManager cho unit test.
        /// </summary>
        internal AssetBundleUsageManager __GetBundleUsageManagerForTest() => assetBundleUsageManager;
    }
}
