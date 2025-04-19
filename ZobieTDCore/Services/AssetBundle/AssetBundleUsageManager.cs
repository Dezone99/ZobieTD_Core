using System;
using System.Collections.Generic;
using System.Text;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCore.Services.AssetBundle
{
    internal class AssetBundleUsageManager
    {
        public static AssetBundleUsageManager Instance { get; } = new AssetBundleUsageManager();

        internal class Tracker
        {
            public int refCount;
            public float lastUsedTime;
        }

        private IUnityEngineContract unityEngineContract = ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initalized");

        // Vì với mỗi bundle name thì chỉ có duy nhất 1 bundle được load
        // nên ta ko thể dùng bundle ref để làm key, vì mỗi name có thể sẽ có nhiều ref đến nó
        // khiến nó không phải là duy nhất
        private Dictionary<string, Tracker> bundleTrackers = new Dictionary<string, Tracker>();

        // Sử dụng count để đếm trong trường hợp có nhiều renderer ref đến cùng 1 asset 
        // trong 1 bundle, ta không thể remove asset đó được mà chỉ giảm count thôi
        // đến khi count bằng 0 thì ta mới chắc chắn là đang ko có ref.
        private Dictionary<IAssetReference, (string bundleName, int count)> assetRefs
            = new Dictionary<IAssetReference, (string bundleName, int count)>();
        private float unloadTimeout = 60f;

        private Dictionary<IAssetReference, HashSet<string>> debugUsageOwners = new Dictionary<IAssetReference, HashSet<string>>();
        private string GetCallerInfo() => System.Environment.StackTrace;

        public void RegisterAssetReference(IAssetReference asset, IAssetBundleReference bundle)
        {
            if (unityEngineContract.IsDevelopmentBuild && !bundle.Contain(asset))
            {
                throw new InvalidOperationException("Asset not belong to the bundle!");
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

        // trả về false khi asset chưa được đăng ký để theo dõi
        // true khi asset được hủy theo dõi thành công
        public bool UnregisterAssetReference(IAssetReference asset)
        {
            if (!assetRefs.TryGetValue(asset, out var entry)) return false;

            var bundleName = entry.bundleName;
            int newCount = entry.count - 1;

            if (newCount == 0)
                assetRefs.Remove(asset);
            else if (newCount < 0)
                throw new InvalidOperationException("Should never happen");
            else
                assetRefs[asset] = (bundleName, newCount);

            if (bundleTrackers.TryGetValue(bundleName, out var tracker))
            {
                tracker.refCount = System.Math.Max(0, tracker.refCount - 1);
                tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;
            }
            else
            {
                throw new InvalidOperationException("Not found bundle tracker. This should never happen");
            }

            if (unityEngineContract.IsDevelopmentBuild)
            {
                var info = GetCallerInfo();
                if (debugUsageOwners.TryGetValue(asset, out var owners))
                {
                    owners.Remove(info);
                    if (owners.Count == 0) debugUsageOwners.Remove(asset);
                }
            }
            return true;
        }

        public List<string> GetNeedToUnloadBundle()
        {
            var toUnload = new List<string>();
            var now = unityEngineContract.TimeProvider.TimeNow;
            foreach (var kvp in bundleTrackers)
            {
                if (kvp.Value.refCount == 0 && now - kvp.Value.lastUsedTime > unloadTimeout)
                    toUnload.Add(kvp.Key);
            }
            return toUnload;
        }

        public void DumpUsageStatus()
        {
            if (unityEngineContract.IsDevelopmentBuild)
            {
                foreach (var kvp in debugUsageOwners)
                {
                    System.Console.WriteLine($"[Usage Debug] Asset: {kvp.Key.Name} is used by:");
                    foreach (var owner in kvp.Value) System.Console.WriteLine($"   - {owner}");
                }
            }
        }

        internal Dictionary<string, Tracker> __GetBundleTrackerForTest()
        {
            return bundleTrackers;
        }

        internal Dictionary<IAssetReference, (string bundle, int count)> __GetAssetRefForTest()
        {
            return assetRefs;
        }
    }
}
