using System;
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

        // Vì với mỗi bundle name thì chỉ có duy nhất 1 bundle được load
        // nên ta ko thể dùng bundle ref để làm key, vì mỗi name có thể sẽ có nhiều ref đến nó
        // khiến nó không phải là duy nhất
        private Dictionary<string, Tracker> bundleTrackers = new Dictionary<string, Tracker>();

        // Sử dụng count để đếm trong trường hợp có nhiều renderer ref đến cùng 1 asset 
        // trong 1 bundle, ta không thể remove asset đó được mà chỉ giảm count thôi
        // đến khi count bằng 0 thì ta mới chắc chắn là đang ko có ref.
        private Dictionary<object, (string bundleName, int count)> assetRefs = new Dictionary<object, (string bundleName, int count)>();

        private float unloadTimeout = 60f;

        // Debug: theo dõi stacktrace của object đang sử dụng asset
        private Dictionary<object, HashSet<string>> debugUsageOwners = new Dictionary<object, HashSet<string>>();
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

            if (!bundleTrackers.TryGetValue(bundle.BundleName, out var tracker))
            {
                tracker = new Tracker { lastUsedTime = unityEngineContract.TimeProvider.TimeNow };
                bundleTrackers[bundle.BundleName] = tracker;
            }

            tracker.refCount++;
            tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;

            if (assetRefs.TryGetValue(asset, out var entry))
                assetRefs[asset] = (entry.bundleName, entry.count + 1);
            else
                assetRefs[asset] = (bundle.BundleName, 1);

            if (unityEngineContract.IsDevelopmentBuild)
            {
                var info = GetCallerInfo();
                if (!debugUsageOwners.ContainsKey(asset))
                    debugUsageOwners[asset] = new HashSet<string>();
                debugUsageOwners[asset].Add(info);
            }
        }

        /// <summary>
        /// Giảm refCount assetRef. Khi count = 0, sẽ dọn dẹp khỏi hệ thống.
        /// </summary>
        /// <param name="asset">Asset reference đã release</param>
        /// <returns>True nếu unregister thành công, false nếu asset chưa được đăng ký</returns>
        public bool UnregisterAssetReference<T>(object asset) where T : class
        {
            ForceCheckAssetType<T>(asset);
            if (!assetRefs.TryGetValue(asset, out var entry))
                return false;

            var bundleName = entry.bundleName;
            int newCount = entry.count - 1;

            if (newCount == 0)
                assetRefs.Remove(asset);
            else if (newCount < 0)
                throw new InvalidOperationException("Invalid refCount: should never be negative");
            else
                assetRefs[asset] = (bundleName, newCount);

            if (bundleTrackers.TryGetValue(bundleName, out var tracker))
            {
                tracker.refCount = Math.Max(0, tracker.refCount - 1);
                tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;
            }
            else
            {
                throw new InvalidOperationException("Tracker for bundle not found. This should never happen");
            }

            if (unityEngineContract.IsDevelopmentBuild)
            {
                var info = GetCallerInfo();
                if (debugUsageOwners.TryGetValue(asset, out var owners))
                {
                    owners.Remove(info);
                    if (owners.Count == 0)
                        debugUsageOwners.Remove(asset);
                }
            }

            return true;
        }

        /// <summary>
        /// Trả về danh sách các bundle đã không được sử dụng quá thời gian timeout.
        /// </summary>
        public List<string> GetNeedToUnloadBundle()
        {
            var now = unityEngineContract.TimeProvider.TimeNow;
            var toUnload = new List<string>();

            foreach (var kvp in bundleTrackers)
            {
                if (kvp.Value.refCount == 0 && now - kvp.Value.lastUsedTime > unloadTimeout)
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
            if (asset is AssetRef<T>)
            {
                return true;
            }
            else if (asset is Array arr)
            {
                if (arr.Length > 0 && arr.GetValue(0) is AssetRef<T>)
                {
                    return true;
                }
            }
            throw new InvalidOperationException("Asset should be type of AssetRef or array of AssetRef");
        }

        /// <summary>
        /// Truy cập nội bộ để test bundle tracker.
        /// </summary>
        internal Dictionary<string, Tracker> __GetBundleTrackerForTest() => bundleTrackers;

        /// <summary>
        /// Truy cập nội bộ để test asset reference table.
        /// </summary>
        internal Dictionary<object, (string bundleName, int count)> __GetAssetRefForTest() => assetRefs;
    }
}
