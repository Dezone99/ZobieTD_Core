using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Services.Logger;

namespace ZobieTDCore.Services.AssetBundle
{
    /// <summary>
    /// Quản lý quá trình load, cache và giải phóng asset từ asset bundle.
    /// Hỗ trợ cả asset đơn và nhóm asset dạng sprite sheet.
    /// </summary>
    public class AssetBundleManager<T> where T : class
    {
        private readonly static TDLogger mLogger = new TDLogger(typeof(AssetBundleManager<T>).Name);
        private readonly IUnityEngineContract unityEngineContract =
            ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initialized");

        private readonly Dictionary<string, IAssetBundleContract> loadedBundles = new Dictionary<string, IAssetBundleContract>();
        private readonly HashSet<(object assetOwner, string bundlePath, object assetRef)> cachedAssetOwner = new HashSet<(object assetOwner, string bundlePath, object assetRef)>();

        #region Sprite Caching
        private readonly Dictionary<AssetRef<T>, IAssetBundleContract> singleSpriteToBundle = new Dictionary<AssetRef<T>, IAssetBundleContract>();
        private readonly Dictionary<(string bundlePath, string spriteName), AssetRef<T>> cachedSingleSpriteAssets = new Dictionary<(string bundlePath, string spriteName), AssetRef<T>>();
        #endregion

        #region Animation Caching
        private readonly Dictionary<IAssetBundleContract, AssetRef<T>[]> bundleToAllLoadedSprites = new Dictionary<IAssetBundleContract, AssetRef<T>[]>();
        private readonly Dictionary<AssetRef<T>[], IAssetBundleContract> animationToBundle = new Dictionary<AssetRef<T>[], IAssetBundleContract>();
        #endregion


        private readonly AssetBundleUsageManager assetBundleUsageManager = new AssetBundleUsageManager();

        /// <summary>
        /// Load 1 sprite đơn từ asset bundle, sử dụng caching và ref tracking theo assetOwner.
        /// </summary>
        public AssetRef<T> LoadSingleSubSpriteAsset(object assetOwner, string bundlePath, string spriteName)
        {
            var key = (bundlePath, spriteName);
            var bundle = LoadAssetBundle(bundlePath);

            if (!cachedSingleSpriteAssets.TryGetValue(key, out var cachedRef))
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }

                var asset = bundle.LoadSingleSubAsset(spriteName);
                if (!(asset is T typedAsset))
                    throw new InvalidCastException($"Asset is not of type {typeof(T).Name}");
                cachedRef = new AssetRef<T>(typedAsset);

                singleSpriteToBundle[cachedRef] = bundle;
                cachedSingleSpriteAssets[key] = cachedRef;
            }

            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundlePath, cachedRef);
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
        public AssetRef<T>[] LoadAnimationSpriteAsset(object assetOwner, string bundlePath)
        {
            var bundle = LoadAssetBundle(bundlePath);

            if (!bundleToAllLoadedSprites.TryGetValue(bundle, out var allLoadedSpritesRef))
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }

                var assets = bundle.LoadAllSubAssets();
                if (assets == null)
                    throw new InvalidOperationException($"Failed to load all sub assets with type {typeof(T).Name}");

                allLoadedSpritesRef = new AssetRef<T>[assets.Length];
                for (int i = 0; i < assets.Length; i++)
                {
                    var rawAsset = assets.GetValue(i);
                    if (!(rawAsset is T typedAsset))
                        throw new InvalidCastException($"Asset is not of type {typeof(T).Name}");
                    allLoadedSpritesRef[i] = new AssetRef<T>(typedAsset);
                }

                bundleToAllLoadedSprites[bundle] = allLoadedSpritesRef;
                animationToBundle[allLoadedSpritesRef] = bundle;
            }

            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundlePath, allLoadedSpritesRef);
            if (cachedAssetOwner.Add(assetOwnerKey))
            {
                assetBundleUsageManager.RegisterAssetReference<T>(allLoadedSpritesRef, bundle);
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
                var assetOwnerKey = (assetOwner, bundle.BundlePath, assetRef);
                if (cachedAssetOwner.Remove(assetOwnerKey))
                {
                    assetBundleUsageManager.UnregisterAssetReference<T>(assetRef);
                }
                else
                {
                    mLogger.D($"Failed to release sprite assets. Not found asset owner key: assetOwnerType={assetOwnerKey.assetOwner.GetType().Name}" +
                        $", bundlePath={assetOwnerKey.BundlePath}" +
                        $", assetRefs={assetOwnerKey.assetRef.ToString()}");
                }
            }
        }

        /// <summary>
        /// Giải phóng nhóm sprite (animation) khỏi hệ thống, nếu assetOwner không còn giữ reference.
        /// </summary>
        public void ReleaseAnimationAssetRef(object assetOwner
            , AssetRef<T>[] assetRefs
            , bool forceCleanUpIfNoRefCount = false)
        {
            if (animationToBundle.TryGetValue(assetRefs, out var bundle))
            {
                var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundle.BundlePath, assetRefs);
                if (cachedAssetOwner.Remove(assetOwnerKey))
                {
                    (var sucess, var bundleRefCount, var assetRefCount) = assetBundleUsageManager.UnregisterAssetReference<T>(assetRefs);
                    if (forceCleanUpIfNoRefCount)
                    {
                        if (!sucess)
                        {
                            throw new InvalidOperationException("Should never happen!");
                        }
                        else if (sucess && bundleRefCount == 0 && assetRefCount == 0)
                        {
                            animationToBundle.Remove(assetRefs);
                            loadedBundles.Remove(bundle.BundlePath);
                            bundleToAllLoadedSprites.Remove(bundle);

                            if (!bundle.IsUnloaded())
                            {
                                bundle.Unload(true);
                            }
                            else
                            {
                                foreach (var sprite in assetRefs)
                                {
                                    sprite.Dispose();
                                }
                            }
                        }
                        else if (sucess && bundleRefCount != assetRefCount)
                        {
                            throw new InvalidOperationException("Should never happen!");
                        }
                        mLogger.D($"Force release bundle: {bundle.BundlePath} successfully!");
                    }
                }
                else
                {
                    mLogger.D($"Failed to release animation assets. Not found asset owner key: assetOwnerType={assetOwnerKey.assetOwner.GetType().Name}" +
                        $", bundlePath={assetOwnerKey.bundlePath}" +
                        $", assetRefs={assetOwnerKey.assetRef.ToString()}");
                }
            }
            else
            {
                mLogger.D($"Failed to release animation assets. Not found asset refs!");
            }
        }


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
        private (object assetOwner, string bundlePath, object assetRef) MakeAssetOwnerKey(
            object assetOwner, string bundlePath, object assetRef)
        {
            if (!(assetRef is AssetRef<T>) && !(assetRef is AssetRef<T>[]))
                throw new InvalidOperationException("assetRef must be AssetRef or AssetRef[]");

            return (assetOwner, bundlePath, assetRef);
        }

        /// <summary>
        /// Load bundle từ StreamingAssets nếu chưa có trong cache.
        /// </summary>
        private IAssetBundleContract LoadAssetBundle(string bundlePath)
        {
            if (!loadedBundles.TryGetValue(bundlePath, out var bundle))
            {
                var path = Path.Combine(unityEngineContract.StreamingAssetPath, bundlePath);
                bundle = unityEngineContract.LoadAssetBundleFromFile(path, bundlePath);
                loadedBundles[bundlePath] = bundle;
            }

            return bundle;
        }

        /// <summary>
        /// Force unload 1 bundle khỏi bộ nhớ, bất kể đang sử dụng hay không.
        /// </summary>
        private void ForceUnloadBundle(string bundlePath)
        {
            if (loadedBundles.TryGetValue(bundlePath, out var bundle))
            {
                bundle.Unload(unloadAllAsset: true);
                loadedBundles.Remove(bundlePath);
            }
        }

        /// <summary>
        /// Truy cập instance AssetBundleUsageManager cho unit test.
        /// </summary>
        internal AssetBundleUsageManager __GetBundleUsageManagerForTest() => assetBundleUsageManager;
        internal Dictionary<AssetRef<T>[], IAssetBundleContract> __GetAnimationBundleMap() => animationToBundle;
    }
}
