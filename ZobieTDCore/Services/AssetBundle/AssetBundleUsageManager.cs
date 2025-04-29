using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCore.Services.AssetBundle
{
    /// <summary>
    /// Quản lý việc sử dụng asset reference và kiểm soát việc tự động giải phóng asset bundle.
    /// Mỗi assetRef được theo dõi bằng refCount để xác định còn đang được sử dụng hay không.
    /// </summary>
    internal class AssetBundleUsageManager
    {
        internal class Tracker
        {
            public int refCount;
            public float lastUsedTime;
        }

        private readonly IUnityEngineContract unityEngineContract =
            ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initialized");

        private ConcurrentDictionary<string, Tracker> bundleTrackers 
            = new ConcurrentDictionary<string, Tracker>();
        private ConcurrentDictionary<object, (string bundlePath, int count)> assetRefs 
            = new ConcurrentDictionary<object, (string bundlePath, int count)>();

        private float unloadTimeout = 60f;

        private ConcurrentDictionary<object, ConcurrentBag<string>> debugUsageOwners = new ConcurrentDictionary<object, ConcurrentBag<string>>();
        private string GetCallerInfo() => System.Environment.StackTrace;

        /// <summary>
        /// Đăng ký assetRef được sử dụng, tăng refCount và đánh dấu thời điểm sử dụng cuối cùng.
        /// </summary>
        /// <param name="asset">Asset reference được sử dụng</param>
        /// <param name="bundle">Bundle chứa asset</param>
        public void RegisterAssetReference<T>(object asset, IAssetBundleContract bundle) where T : class
        {
            ForceCheckAssetType<T>(asset);
            if (asset is AssetRef<T> ar &&
                (ar.Ref == null || unityEngineContract.IsDevelopmentBuild && !bundle.Contain(ar.Ref)))
            {
                throw new InvalidOperationException("Asset does not belong to the given bundle!");
            }

            var tracker = bundleTrackers.GetOrAdd(bundle.BundlePath, _ => new Tracker());
            tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;
            System.Threading.Interlocked.Increment(ref tracker.refCount);

            assetRefs.AddOrUpdate(asset,
                _ => (bundle.BundlePath, 1),
                (_, existing) => (existing.bundlePath, existing.count + 1));

            if (unityEngineContract.IsDevelopmentBuild)
            {
                var info = GetCallerInfo();
                var bag = debugUsageOwners.GetOrAdd(asset, _ => new ConcurrentBag<string>());
                bag.Add(info);
            }
        }

        /// <summary>
        /// Giảm refCount assetRef. Khi count = 0, sẽ dọn dẹp khỏi hệ thống.
        /// </summary>
        /// <param name="asset">Asset reference đã release</param>
        public (bool success, int bundleRefCount, int assetRefCount) UnregisterAssetReference<T>(object asset) where T : class
        {
            ForceCheckAssetType<T>(asset);
            if (!assetRefs.TryGetValue(asset, out var entry))
                return (false, -1, -1);

            var bundleName = entry.bundlePath;
            int newAssetRefCount = entry.count - 1;

            if (newAssetRefCount < 0)
                throw new InvalidOperationException("Invalid refCount: should never be negative");

            if (newAssetRefCount == 0)
                assetRefs.TryRemove(asset, out _);
            else
                assetRefs[asset] = (bundleName, newAssetRefCount);

            if (bundleTrackers.TryGetValue(bundleName, out var tracker))
            {
                System.Threading.Interlocked.Decrement(ref tracker.refCount);
                tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;
            }
            else
            {
                throw new InvalidOperationException("Tracker for bundle not found. This should never happen");
            }

            if (unityEngineContract.IsDevelopmentBuild && newAssetRefCount == 0)
            {
                debugUsageOwners.TryRemove(asset, out _);
            }

            return (true, tracker.refCount, newAssetRefCount);
        }

        /// <summary>
        /// Trả về danh sách các bundle đã không được sử dụng quá thời gian timeout.
        /// </summary>
        public List<string> GetNeedToUnloadBundle(bool forceUnloadWithoutTimeout = false)
        {
            var now = unityEngineContract.TimeProvider.TimeNow;
            var toUnload = new List<string>();

            foreach (var kvp in bundleTrackers)
            {
                if (!forceUnloadWithoutTimeout && kvp.Value.refCount == 0 && now - kvp.Value.lastUsedTime > unloadTimeout)
                    toUnload.Add(kvp.Key);
                else if (forceUnloadWithoutTimeout && kvp.Value.refCount == 0)
                    toUnload.Add(kvp.Key);
            }

            return toUnload;
        }

        /// <summary>
        /// In ra danh sách các asset đang được giữ tham chiếu cùng stacktrace (chỉ dev build).
        /// </summary>
        public void DumpUsageStatus()
        {
            if (unityEngineContract.IsDevelopmentBuild)
            {
                foreach (var kvp in debugUsageOwners)
                {
                    var assetName = unityEngineContract.GetUnityObjectName(kvp.Key);
                    Console.WriteLine($"[Usage Debug] Asset: {assetName} is used by:");
                    foreach (var owner in kvp.Value)
                        Console.WriteLine($"   - {owner}");
                }
            }
        }

        private bool ForceCheckAssetType<T>(object asset) where T : class
        {
            if (asset is AssetRef<T>) return true;
            if (asset is Array arr && arr.Length > 0 && arr.GetValue(0) is AssetRef<T>) return true;
            throw new InvalidOperationException("Asset should be type of AssetRef or array of AssetRef");
        }

        /// <summary>
        /// Truy cập nội bộ để test bundle tracker.
        /// </summary>
        internal ConcurrentDictionary<string, Tracker> __GetBundleTrackerForTest() => bundleTrackers;

        /// <summary>
        /// Truy cập nội bộ để test asset reference table.
        /// </summary>
        internal ConcurrentDictionary<object, (string bundlePath, int count)> __GetAssetRefForTest() => assetRefs;
    }
}
