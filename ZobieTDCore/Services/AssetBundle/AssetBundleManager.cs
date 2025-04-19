using System;
using System.Collections.Generic;
using System.IO;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCore.Services.AssetBundle
{
    public class AssetBundleManager
    {
        public static AssetBundleManager Instance { get; } = new AssetBundleManager();

        private Dictionary<string, IAssetBundleReference> loadedBundles = new Dictionary<string, IAssetBundleReference>();
        private Dictionary<IAssetReference, string> assetToBundle = new Dictionary<IAssetReference, string>();

        // Sử dụng cho 1 sprite trong spr sheet
        public IAssetReference LoadSingleAsset(string bundleName, string spriteName)
        {
            var bundle = LoadAssetBundle(bundleName);
            var sprite = bundle.LoadSingleAsset(spriteName);
            assetToBundle[sprite] = bundleName;
            AssetBundleUsageManager.Instance.RegisterAssetReference(sprite, bundle);
            return sprite;
        }

        // Sử dụng cho toàn bộ sprite trong spr sheet để tạo animation 
        public IAssetReference LoadAllAsset(string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);
            var assetRef = bundle.LoadAllAssets();
            assetToBundle[assetRef] = bundleName;
            AssetBundleUsageManager.Instance.RegisterAssetReference(assetRef, bundle);
            return assetRef;
        }


        public void ReleaseAssetRef(IAssetReference assetRef)
        {
            if (assetToBundle.TryGetValue(assetRef, out var bundleName))
            {
                AssetBundleUsageManager.Instance.UnregisterAssetReference(assetRef);
            }
        }


        public string? GetBundleNameOfSprite(IAssetReference sprite)
        {
            return assetToBundle.TryGetValue(sprite, out var bundleName) ? bundleName : null;
        }

        public void UpdateCachedAssetBundle()
        {
            var unityEngineContract = ContractManager.Instance.UnityEngineContract;
            if (unityEngineContract == null)
            {
                throw new InvalidOperationException("Core engine was not initalized");
            }
            var now = unityEngineContract.TimeProvider.TimeNow;
            var toUnload = new List<string>();

            foreach (var bundleName in AssetBundleUsageManager.Instance.GetNeedToUnloadBundle())
            {
                var contract = ContractManager.Instance.UnityEngineContract;
                ForceUnloadBundle(bundleName);
            }
        }

        public List<string> GetLoadedBundles() => new List<string>(loadedBundles.Keys);

        private IAssetBundleReference LoadAssetBundle(string bundleName)
        {
            var unityEngineContract = ContractManager.Instance.UnityEngineContract;
            if (unityEngineContract == null)
            {
                throw new InvalidOperationException("Core engine was not initalized");
            }
            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                var path = Path.Combine(unityEngineContract.StreamingAssetPath, bundleName);
                bundle = unityEngineContract.LoadAssetBundleFromFile(path);
                loadedBundles[bundleName] = bundle;
            }
            return bundle;
        }

        private void ForceUnloadBundle(string bundleName)
        {
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllAsset: true);
                loadedBundles.Remove(bundleName);
            }
        }
    }

}
