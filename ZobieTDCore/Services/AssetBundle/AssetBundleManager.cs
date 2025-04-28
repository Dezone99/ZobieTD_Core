using System;
using System.Collections;
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
    /// Quản lý quá trình load, cache và giải phóng assetRef từ assetRef bundle.
    /// Hỗ trợ cả assetRef đơn và nhóm assetRef dạng sprite sheet.
    /// </summary>
    public class AssetBundleManager<T> where T : class
    {
        private readonly static TDLogger mLogger = new TDLogger(typeof(AssetBundleManager<T>).Name);
        private readonly IUnityEngineContract unityEngineContract =
            ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initialized");

        private readonly Dictionary<string, IAssetBundleContract> loadedBundles = new Dictionary<string, IAssetBundleContract>();
        private readonly Dictionary<IAssetBundleContract, HashSet<(object assetOwner, string bundlePath, object assetRef)>> cachedAssetOwner
            = new Dictionary<IAssetBundleContract, HashSet<(object assetOwner, string bundlePath, object assetRef)>>();

        #region Sprite Caching
        private readonly Dictionary<AssetRef<T>, IAssetBundleContract> singleSpriteToBundle = new Dictionary<AssetRef<T>, IAssetBundleContract>();
        private readonly Dictionary<IAssetBundleContract, Dictionary<string, AssetRef<T>>> bundleToSingleSpriteAssets = new Dictionary<IAssetBundleContract, Dictionary<string, AssetRef<T>>>();
        #endregion

        #region Animation Caching
        private readonly Dictionary<IAssetBundleContract, AssetRef<T>[]> bundleToAllLoadedSprites = new Dictionary<IAssetBundleContract, AssetRef<T>[]>();
        private readonly Dictionary<AssetRef<T>[], IAssetBundleContract> animationToBundle = new Dictionary<AssetRef<T>[], IAssetBundleContract>();
        #endregion


        private readonly AssetBundleUsageManager assetBundleUsageManager = new AssetBundleUsageManager();

        /// <summary>
        /// Load 1 sprite đơn từ assetRef bundle, sử dụng caching và ref tracking theo assetOwner.
        /// </summary>
        public AssetRef<T> LoadSingleSubSpriteAsset(object assetOwner, string bundlePath, string spriteName)
        {
            var bundle = LoadAssetBundle(bundlePath);
            if (!bundleToSingleSpriteAssets.ContainsKey(bundle))
            {
                bundleToSingleSpriteAssets[bundle] = new Dictionary<string, AssetRef<T>>();
            }

            var sprMap = bundleToSingleSpriteAssets[bundle];

            if (!sprMap.TryGetValue(spriteName, out var assetRef))
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }

                var @ref = bundle.LoadSingleSubAsset(spriteName);
                if (!(@ref is T typedAsset))
                    throw new InvalidCastException($"Asset is not of type {typeof(T).Name}");
                assetRef = new AssetRef<T>((T)@ref);

                singleSpriteToBundle[assetRef] = bundle;
                sprMap[spriteName] = assetRef;
            }

            if (!cachedAssetOwner.ContainsKey(bundle))
            {
                cachedAssetOwner[bundle] = new HashSet<(object assetOwner, string bundlePath, object assetRef)>();
            }
            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundlePath, assetRef);
            if (cachedAssetOwner[bundle].Add(assetOwnerKey))
            {
                assetBundleUsageManager.RegisterAssetReference<T>(assetRef, bundle);
            }

            return assetRef;
        }

        /// <summary>
        /// Load toàn bộ sprite trong 1 assetRef bundle (ví dụ: animation frames).
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

            if (!cachedAssetOwner.ContainsKey(bundle))
            {
                cachedAssetOwner[bundle] = new HashSet<(object assetOwner, string bundlePath, object assetRef)>();
            }
            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundlePath, allLoadedSpritesRef);
            if (cachedAssetOwner[bundle].Add(assetOwnerKey))
            {
                assetBundleUsageManager.RegisterAssetReference<T>(allLoadedSpritesRef, bundle);
            }

            // When all assetRef are loaded, we can softly release the bundle
            bundle.Unload(false);

            return allLoadedSpritesRef;
        }

        /// <summary>
        /// Giải phóng 1 sprite đơn khỏi hệ thống, nếu assetOwner không còn giữ reference.
        /// </summary>
        public void ReleaseSpriteAssetRef(object assetOwner
            , AssetRef<T> assetRef
            , bool forceCleanUpIfNoRefCount = false)
        {
            if (assetRef.Ref != null && singleSpriteToBundle.TryGetValue(assetRef, out var bundle))
            {
                var assetOwnerKey = (assetOwner, bundle.BundlePath, assetRef);
                if (cachedAssetOwner[bundle].Remove(assetOwnerKey))
                {
                    var assetName = unityEngineContract.GetUnityObjectName(assetRef.Ref);
                    (var sucess, var bundleRefCount, var assetRefCount) = assetBundleUsageManager.UnregisterAssetReference<T>(assetRef);
                    if (forceCleanUpIfNoRefCount)
                    {
                        if (!sucess)
                        {
                            throw new InvalidOperationException("Should never happen!");
                        }
                        else if (sucess && bundleRefCount == 0 && assetRefCount == 0)
                        {
                            singleSpriteToBundle.Remove(assetRef);
                            loadedBundles.Remove(bundle.BundlePath);
                            bundleToSingleSpriteAssets.Remove(bundle);
                            cachedAssetOwner.Remove(bundle);
                            if (!bundle.IsUnloaded())
                            {
                                bundle.Unload(true);
                            }
                            else
                            {
                                assetRef.Dispose();
                            }
                            mLogger.D($"Force release bundle: {bundle.BundlePath} successfully!");
                        }
                        else if (sucess && bundleRefCount > 0 && assetRefCount == 0)
                        {
                            singleSpriteToBundle.Remove(assetRef);
                            bundleToSingleSpriteAssets[bundle].Remove(assetName);
                            assetRef.Dispose();

                            mLogger.D($"Force release assetRef ref: {assetName} successfully!");
                        }
                        else if (sucess && bundleRefCount != assetRefCount)
                        {
                            throw new InvalidOperationException("Should never happen!");
                        }
                    }
                }
                else
                {
                    mLogger.D($"Failed to release sprite assets. Not found assetRef owner key: assetOwnerType={assetOwnerKey.assetOwner.GetType().Name}" +
                        $", bundlePath={assetOwnerKey.BundlePath}" +
                        $", assetRefs={assetOwnerKey.assetRef.ToString()}");
                    throw new InvalidOperationException("Asset should belong to the owner!");
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
                if (cachedAssetOwner[bundle].Remove(assetOwnerKey))
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
                            cachedAssetOwner.Remove(bundle);

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
                            mLogger.D($"Force release bundle: {bundle.BundlePath} successfully!");
                        }
                        // Có thể xảy ra nếu các owner khác sử dụng spr trong animation
                        else if (sucess && bundleRefCount > 0 && assetRefCount == 0)
                        {
                            animationToBundle.Remove(assetRefs);
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
                            mLogger.D($"Force release animation ref successfully!");
                        }
                        else if (sucess && bundleRefCount != assetRefCount)
                        {
                            throw new InvalidOperationException("Should never happen!");
                        }
                    }
                }
                else
                {
                    mLogger.D($"Failed to release animation assets. Not found assetRef owner key: assetOwnerType={assetOwnerKey.assetOwner.GetType().Name}" +
                        $", bundlePath={assetOwnerKey.bundlePath}" +
                        $", assetRefs={assetOwnerKey.assetRef.ToString()}");
                    throw new InvalidOperationException("Asset should belong to the owner!");
                }
            }
            else
            {
                mLogger.D($"Failed to release animation assets. Not found assetRef refs!");
            }
        }


        /// <summary>
        /// Kiểm tra và unload các bundle không còn được sử dụng theo timeout từ AssetBundleUsageManager.
        /// </summary>
        public void UpdateCachedAssetBundle(bool forceUnloadWithoutTimeout)
        {
            foreach (var bundleName in assetBundleUsageManager.GetNeedToUnloadBundle(forceUnloadWithoutTimeout))
            {
                ForceUnloadBundle(bundleName);
            }
        }

        /// <summary>
        /// Trả về danh sách các bundle hiện đang được load.
        /// </summary>
        public List<string> GetLoadedBundles() => new List<string>(loadedBundles.Keys);

        /// <summary>
        /// Tạo khóa duy nhất cho assetOwner khi tracking assetRef (single hoặc assets).
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
                if (bundleToSingleSpriteAssets.ContainsKey(bundle))
                {
                    var assetMap = bundleToSingleSpriteAssets[bundle];
                    foreach (var asset in assetMap.Values)
                    {
                        singleSpriteToBundle.Remove(asset);
                    }
                    bundleToSingleSpriteAssets.Remove(bundle);
                }

                if (bundleToAllLoadedSprites.ContainsKey(bundle))
                {
                    var animations = bundleToAllLoadedSprites[bundle];
                    animationToBundle.Remove(animations);
                    bundleToAllLoadedSprites.Remove(bundle);
                }

                cachedAssetOwner[bundle].Clear();
                cachedAssetOwner.Remove(bundle);
            }
        }

        /// <summary>
        /// Truy cập instance AssetBundleUsageManager cho unit test.
        /// </summary>
        internal AssetBundleUsageManager __GetBundleUsageManagerForTest() => assetBundleUsageManager;


        internal Dictionary<string, IAssetBundleContract> __GetLoadedBundles() => loadedBundles;
        internal Dictionary<IAssetBundleContract, HashSet<(object assetOwner, string bundlePath, object assetRef)>> __GetCachedAssetOwner() => cachedAssetOwner;
        internal Dictionary<AssetRef<T>, IAssetBundleContract> __GetSingleSpriteToBundle() => singleSpriteToBundle;
        internal Dictionary<IAssetBundleContract, Dictionary<string, AssetRef<T>>> __GetBundleToSingleSpriteAssets() => bundleToSingleSpriteAssets;
        internal Dictionary<AssetRef<T>[], IAssetBundleContract> __GetAnimationBundleMap() => animationToBundle;
        internal Dictionary<IAssetBundleContract, AssetRef<T>[]> __GetBundleToAllLoadedSprites() => bundleToAllLoadedSprites;
    }
}
