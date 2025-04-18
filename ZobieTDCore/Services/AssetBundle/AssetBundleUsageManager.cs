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

        private class Tracker
        {
            public int refCount;
            public float lastUsedTime;
        }

        private Dictionary<string, Tracker> bundleTrackers = new Dictionary<string, Tracker>();
        private Dictionary<IAssetReference, (string bundleName, int count)> assetRefs = new Dictionary<IAssetReference, (string bundleName, int count)>();
        private float unloadTimeout = 60f;
        private IUnityEngineContract unityEngineContract = ContractManager.Instance.UnityEngineContract ?? throw new InvalidOperationException("Core engine was not initalized");

        private Dictionary<IAssetReference, HashSet<string>> debugUsageOwners = new Dictionary<IAssetReference, HashSet<string>>();
        private string GetCallerInfo() => System.Environment.StackTrace;

        public void RegisterAsset(IAssetReference asset, string bundleName)
        {
            if (!bundleTrackers.TryGetValue(bundleName, out var tracker))
            {
                tracker = new Tracker { lastUsedTime = unityEngineContract.TimeProvider.TimeNow };
                bundleTrackers[bundleName] = tracker;
            }
            tracker.refCount++;
            tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;

            if (assetRefs.TryGetValue(asset, out var entry))
                assetRefs[asset] = (bundleName, entry.count + 1);
            else
                assetRefs[asset] = (bundleName, 1);

            if (unityEngineContract.IsDevelopmentBuild)
            {
                var info = GetCallerInfo();
                if (!debugUsageOwners.ContainsKey(asset))
                    debugUsageOwners[asset] = new HashSet<string>();
                debugUsageOwners[asset].Add(info);
            }
        }

        public void UnregisterAsset(IAssetReference asset)
        {
            if (!assetRefs.TryGetValue(asset, out var entry)) return;

            var bundleName = entry.bundleName;
            int newCount = entry.count - 1;

            if (newCount <= 0)
                assetRefs.Remove(asset);
            else
                assetRefs[asset] = (bundleName, newCount);

            if (bundleTrackers.TryGetValue(bundleName, out var tracker))
            {
                tracker.refCount = System.Math.Max(0, tracker.refCount - 1);
                tracker.lastUsedTime = unityEngineContract.TimeProvider.TimeNow;
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
    }
}
