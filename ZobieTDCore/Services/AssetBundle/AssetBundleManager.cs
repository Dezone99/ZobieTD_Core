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
        private Dictionary<IAssetReference, string> spriteToBundle = new Dictionary<IAssetReference, string>();

        public void Test()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Console.WriteLine("Hello");
#endif
        }
        public IAssetReference LoadSprite(string bundleName, string spriteName)
        {
            var bundle = LoadAssetBundle(bundleName);
            var sprite = bundle.LoadSingleAsset(spriteName);
            spriteToBundle[sprite] = bundleName;
            AssetBundleUsageManager.Instance.RegisterAsset(sprite, bundleName);
            return sprite;
        }

        public IEnumerable<IAssetReference> LoadAnimation(string bundleName, string prefix)
        {
            var bundle = LoadAssetBundle(bundleName);
            var sprites = new List<IAssetReference>();
            foreach (var name in bundle.GetAllAssetNames())
            {
                if (name.Contains(prefix))
                {
                    var sprite = bundle.LoadSingleAsset(name);
                    spriteToBundle[sprite] = bundleName;
                    AssetBundleUsageManager.Instance.RegisterAsset(sprite, bundleName);
                    sprites.Add(sprite);
                }
            }
            return sprites;
        }

        public void ReleaseSprite(IAssetReference sprite)
        {
            if (spriteToBundle.TryGetValue(sprite, out var bundleName))
            {
                AssetBundleUsageManager.Instance.UnregisterAsset(sprite);
            }
        }

        public void ReleaseAnimation(IEnumerable<IAssetReference> sprites)
        {
            foreach (var sprite in sprites)
                ReleaseSprite(sprite);
        }

        public string? GetBundleNameOfSprite(IAssetReference sprite)
        {
            return spriteToBundle.TryGetValue(sprite, out var bundleName) ? bundleName : null;
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
