using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZobieTDCore.Services.AssetBundle.Base;

namespace ZobieTDCore.Services.AssetBundle
{
    public class AssetBundleManager
    {
        private readonly Dictionary<string, IAssetBundleReference> loadedBundles = new Dictionary<string, IAssetBundleReference>();
        private readonly Dictionary<IAssetReference, string> assetToBundle = new Dictionary<IAssetReference, string>();

        public IAssetReference LoadSprite(string bundleName, string spriteName)
        {
            var bundle = loadedBundles[bundleName];
            var asset = bundle.LoadSingle(spriteName);
            assetToBundle[asset] = bundleName;
            return asset;
        }

        public List<IAssetReference> LoadAnimation(string bundleName, string prefix)
        {
            var bundle = loadedBundles[bundleName];
            var assets = bundle.LoadAssets(prefix).ToList();
            foreach (var asset in assets)
                assetToBundle[asset] = bundleName;
            return assets;
        }

        public void ReleaseAsset(IAssetReference asset)
        {
            assetToBundle.Remove(asset);
            // Optional: Notify usage manager
        }

        public void ReleaseAssets(List<IAssetReference> assets)
        {
            foreach (var asset in assets)
                assetToBundle.Remove(asset);
        }

        public string GetBundleNameOf(IAssetReference asset)
            => assetToBundle.TryGetValue(asset, out var name) ? name : null;

        public void RegisterBundle(string name, IAssetBundleReference bundle)
            => loadedBundles[name] = bundle;

        public void UnloadBundle(string name)
            => loadedBundles.Remove(name);

        public List<string> GetLoadedBundleNames() => loadedBundles.Keys.ToList();
    }

}
