using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCoreNTest.Contracts.Items.AssetBundle
{
    internal class MockBundleReference : IAssetBundleReference
    {
        private readonly Dictionary<string, IAssetReference> assets;
        public string BundleName { get; }

        public MockBundleReference(string name, IEnumerable<IAssetReference> refs)
        {
            BundleName = name;
            assets = new Dictionary<string, IAssetReference>();
            foreach (var asset in refs)
            {
                assets[asset.Name] = asset;
            }
        }

        public bool Contain(IAssetReference asset) => assets.ContainsKey(asset.Name);
        public IAssetReference LoadAllSubSpriteAssets()
        {
            return new MockAssetReference(BundleName, assets);
        }
        public IAssetReference LoadSingleSubSpriteAsset(string name)
        {
            if (assets.ContainsKey(name))
            {
                return assets[name];
            }
            throw new KeyNotFoundException($"Asset with name '{name}' not found in bundle '{BundleName}'");
        }
        public void Unload(bool unloadAllAsset) { }
    }

}
