using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.UnityItem;

namespace ZobieTDCoreNTest.Contracts.Items.AssetBundle
{
    internal class MockBundleReference : IAssetBundleContract
    {
        private readonly Dictionary<string, MockUnityAsset> assets;
        public string BundleName { get; }

        public MockBundleReference(string name, IEnumerable<MockUnityAsset> refs)
        {
            BundleName = name;
            assets = new Dictionary<string, MockUnityAsset>();
            foreach (var asset in refs)
            {
                assets[asset.name] = asset;
            }
        }

        public void Unload(bool unloadAllAsset)
        {
            assets.Clear();
        }

        public object LoadAllSubSpriteAssets()
        {
            return assets.Values.ToArray();
        }

        public object LoadSingleSubSpriteAsset(string name)
        {
            if (assets.ContainsKey(name))
            {
                return assets[name];
            }
            throw new KeyNotFoundException($"Asset with name '{name}' not found in bundle '{BundleName}'");

        }

        public bool Contain(object asset)
        {
            if (asset is MockUnityAsset masset)
            {
                return assets.ContainsKey(masset.name);
            }
            throw new InvalidOperationException("Contract violationm, asset must be type of Unity Asset");
        }
    }

}
