using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Contracts.Items;
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

        private readonly ConcurrentDictionary<string, IAssetBundleContract> loadedBundles 
            = new ConcurrentDictionary<string, IAssetBundleContract>();
        private readonly ConcurrentDictionary<IAssetBundleContract, ConcurrentDictionary<(object assetOwner, string bundlePath, object assetRef), byte>> cachedAssetOwner 
            = new ConcurrentDictionary<IAssetBundleContract, ConcurrentDictionary<(object assetOwner, string bundlePath, object assetRef), byte>>();

        private readonly ConcurrentDictionary<AssetRef<T>, IAssetBundleContract> singleAssetToBundle
            = new ConcurrentDictionary<AssetRef<T>, IAssetBundleContract>();
        private readonly ConcurrentDictionary<IAssetBundleContract, ConcurrentDictionary<string, AssetRef<T>>> bundleToSingleAsset 
            = new ConcurrentDictionary<IAssetBundleContract, ConcurrentDictionary<string, AssetRef<T>>>();

        private readonly ConcurrentDictionary<IAssetBundleContract, AssetRef<T>[]> bundleToAnimation 
            = new ConcurrentDictionary<IAssetBundleContract, AssetRef<T>[]>();
        private readonly ConcurrentDictionary<AssetRef<T>[], IAssetBundleContract> animationToBundle
            = new ConcurrentDictionary<AssetRef<T>[], IAssetBundleContract>();

        private readonly AssetBundleUsageManager assetBundleUsageManager 
            = new AssetBundleUsageManager();

        /// <summary>
        /// Load 1 sprite đơn từ assetRef bundle, sử dụng caching và ref tracking theo assetOwner.
        /// </summary>
        public AssetRef<T> LoadSingleSubAsset(object assetOwner, string bundlePath, string spriteName)
        {
            var bundle = LoadAssetBundle(bundlePath);
            var sprMap = bundleToSingleAsset.GetOrAdd(bundle, _ => new ConcurrentDictionary<string, AssetRef<T>>());

            var assetRef = sprMap.GetOrAdd(spriteName, _ =>
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }
                var @ref = bundle.LoadSingleSubAsset(spriteName);
                if (!(@ref is T typedAsset))
                    throw new InvalidCastException($"Asset is not of type {typeof(T).Name}");
                var assetRefLocal = new AssetRef<T>(typedAsset);
                singleAssetToBundle[assetRefLocal] = bundle;
                return assetRefLocal;
            });

            var ownerSet = cachedAssetOwner.GetOrAdd(bundle, _ => new ConcurrentDictionary<(object, string, object), byte>());
            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundlePath, assetRef);
            if (ownerSet.TryAdd(assetOwnerKey, 0))
            {
                assetBundleUsageManager.RegisterAssetReference<T>(assetRef, bundle);
            }

            return assetRef;
        }

        /// <summary>
        /// Load toàn bộ sprite trong 1 assetRef bundle (ví dụ: animation frames).
        /// Cache theo bundle và track ref theo assetOwner.
        /// </summary>
        public AssetRef<T>[] LoadAllSubAssets(object assetOwner, string bundlePath)
        {
            var bundle = LoadAssetBundle(bundlePath);

            var allLoadedSpritesRef = bundleToAnimation.GetOrAdd(bundle, _ =>
            {
                if (bundle.IsUnloaded())
                {
                    bundle.ReloadBundle();
                }
                var assets = bundle.LoadAllSubAssets();
                if (assets == null)
                    throw new InvalidOperationException($"Failed to load all sub assets with type {typeof(T).Name}");

                var refs = new AssetRef<T>[assets.Length];
                for (int i = 0; i < assets.Length; i++)
                {
                    var rawAsset = assets.GetValue(i);
                    if (!(rawAsset is T typedAsset))
                        throw new InvalidCastException($"Asset is not of type {typeof(T).Name}");
                    refs[i] = new AssetRef<T>(typedAsset);
                }
                animationToBundle[refs] = bundle;
                return refs;
            });

            var ownerSet = cachedAssetOwner.GetOrAdd(bundle, _ => new ConcurrentDictionary<(object, string, object), byte>());
            var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundlePath, allLoadedSpritesRef);
            if (ownerSet.TryAdd(assetOwnerKey, 0))
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
        public void ReleaseSpriteAssetRef(object assetOwner, AssetRef<T> assetRef, bool forceCleanUpIfNoRefCount = false)
        {
            if (assetRef.Ref != null && singleAssetToBundle.TryGetValue(assetRef, out var bundle))
            {
                if (cachedAssetOwner.TryGetValue(bundle, out var ownerSet))
                {
                    var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundle.BundlePath, assetRef);
                    if (ownerSet.TryRemove(assetOwnerKey, out _))
                    {
                        var assetName = unityEngineContract.GetUnityObjectName(assetRef.Ref);
                        (var success, var bundleRefCount, var assetRefCount) = assetBundleUsageManager.UnregisterAssetReference<T>(assetRef);

                        if (forceCleanUpIfNoRefCount)
                        {
                            if (!success)
                                throw new InvalidOperationException("Should never happen!");
                            else if (success && bundleRefCount == 0 && assetRefCount == 0)
                            {
                                singleAssetToBundle.TryRemove(assetRef, out _);
                                loadedBundles.TryRemove(bundle.BundlePath, out _);
                                bundleToSingleAsset.TryRemove(bundle, out _);
                                cachedAssetOwner.TryRemove(bundle, out _);
                                if (!bundle.IsUnloaded())
                                    bundle.Unload(true);
                                else
                                    assetRef.Dispose();
                                mLogger.D($"Force release bundle: {bundle.BundlePath} successfully!");
                            }
                            else if (success && bundleRefCount > 0 && assetRefCount == 0)
                            {
                                singleAssetToBundle.TryRemove(assetRef, out _);
                                if (bundleToSingleAsset.TryGetValue(bundle, out var sprMap))
                                {
                                    sprMap.TryRemove(assetName, out _);
                                }
                                assetRef.Dispose();
                                mLogger.D($"Force release assetRef ref: {assetName} successfully!");
                            }
                        }
                    }
                    else
                    {
                        mLogger.D($"Failed to release sprite assets. Not found assetRef owner key.");
                        throw new InvalidOperationException("Asset should belong to the owner!");
                    }
                }
            }
        }

        /// <summary>
        /// Giải phóng nhóm sprite (animation) khỏi hệ thống, nếu assetOwner không còn giữ reference.
        /// </summary>
        public void ReleaseAnimationAssetRef(object assetOwner, AssetRef<T>[] assetRefs, bool forceCleanUpIfNoRefCount = false)
        {
            if (animationToBundle.TryGetValue(assetRefs, out var bundle))
            {
                if (cachedAssetOwner.TryGetValue(bundle, out var ownerSet))
                {
                    var assetOwnerKey = MakeAssetOwnerKey(assetOwner, bundle.BundlePath, assetRefs);
                    if (ownerSet.TryRemove(assetOwnerKey, out _))
                    {
                        (var success, var bundleRefCount, var assetRefCount) = assetBundleUsageManager.UnregisterAssetReference<T>(assetRefs);
                        if (forceCleanUpIfNoRefCount)
                        {
                            if (!success)
                                throw new InvalidOperationException("Should never happen!");
                            else if (success && bundleRefCount == 0 && assetRefCount == 0)
                            {
                                animationToBundle.TryRemove(assetRefs, out _);
                                loadedBundles.TryRemove(bundle.BundlePath, out _);
                                bundleToAnimation.TryRemove(bundle, out _);
                                cachedAssetOwner.TryRemove(bundle, out _);

                                if (!bundle.IsUnloaded())
                                    bundle.Unload(true);
                                else
                                {
                                    foreach (var sprite in assetRefs)
                                        sprite.Dispose();
                                }
                                mLogger.D($"Force release bundle: {bundle.BundlePath} successfully!");
                            }
                            else if (success && bundleRefCount > 0 && assetRefCount == 0)
                            {
                                animationToBundle.TryRemove(assetRefs, out _);
                                bundleToAnimation.TryRemove(bundle, out _);
                                if (!bundle.IsUnloaded())
                                    bundle.Unload(true);
                                else
                                {
                                    foreach (var sprite in assetRefs)
                                        sprite.Dispose();
                                }
                                mLogger.D($"Force release animation ref successfully!");
                            }
                        }
                    }
                    else
                    {
                        mLogger.D($"Failed to release animation assets. Not found assetRef owner key.");
                        throw new InvalidOperationException("Asset should belong to the owner!");
                    }
                }
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
        /// Load bundle từ StreamingAssets nếu chưa có trong cache.
        /// </summary>
        private IAssetBundleContract LoadAssetBundle(string bundlePath)
        {
            return loadedBundles.GetOrAdd(bundlePath, _ =>
            {
                var path = Path.Combine(unityEngineContract.StreamingAssetPath, bundlePath);
                return unityEngineContract.LoadAssetBundleFromFile(path, bundlePath);
            });
        }

        /// <summary>
        /// Force unload 1 bundle khỏi bộ nhớ, bất kể đang sử dụng hay không.
        /// </summary>
        private void ForceUnloadBundle(string bundlePath)
        {
            if (loadedBundles.TryRemove(bundlePath, out var bundle))
            {
                bundle.Unload(unloadAllAsset: true);
                if (bundleToSingleAsset.TryRemove(bundle, out var assetMap))
                {
                    foreach (var asset in assetMap.Values)
                    {
                        singleAssetToBundle.TryRemove(asset, out _);
                    }
                }
                if (bundleToAnimation.TryRemove(bundle, out var animations)) 
                {
                    animationToBundle.TryRemove(animations, out _);
                }

                cachedAssetOwner[bundle].Clear();
                cachedAssetOwner.TryRemove(bundle, out _);
            }
        }

        /// <summary>
        /// Tạo khóa duy nhất cho assetOwner khi tracking assetRef (single hoặc assets).
        /// </summary>
        private (object assetOwner, string bundlePath, object assetRef) MakeAssetOwnerKey(object assetOwner, string bundlePath, object assetRef)
        {
            if (!(assetRef is AssetRef<T>) && !(assetRef is AssetRef<T>[]))
                throw new InvalidOperationException("assetRef must be AssetRef or AssetRef[]");

            return (assetOwner, bundlePath, assetRef);
        }
        /// <summary>
        /// Truy cập instance AssetBundleUsageManager cho unit test.
        /// </summary>
        internal AssetBundleUsageManager __GetBundleUsageManagerForTest() => assetBundleUsageManager;
        internal ConcurrentDictionary<string, IAssetBundleContract> __GetLoadedBundles() => loadedBundles;
        internal ConcurrentDictionary<IAssetBundleContract, ConcurrentDictionary<(object assetOwner, string bundlePath, object assetRef), byte>> __GetCachedAssetOwner() => cachedAssetOwner;
        internal ConcurrentDictionary<AssetRef<T>, IAssetBundleContract> __GetSingleAssetToBundle() => singleAssetToBundle;
        internal ConcurrentDictionary<IAssetBundleContract, ConcurrentDictionary<string, AssetRef<T>>> __GetBundleToSingleAsset() => bundleToSingleAsset;
        internal ConcurrentDictionary<AssetRef<T>[], IAssetBundleContract> __GetAnimationBundleMap() => animationToBundle;
        internal ConcurrentDictionary<IAssetBundleContract, AssetRef<T>[]> __GetBundleToAnimation() => bundleToAnimation;
    }
}
