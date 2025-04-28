using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.UnityItem;

namespace ZobieTDCoreNTest.Contracts.Items.AssetBundle
{
    internal class MockBundleReference : BaseAssetBundleContract
    {
        private Dictionary<string, MockUnityAsset> assets;
        private readonly IEnumerable<MockUnityAsset> refs;

        public override string BundleName { get; }
        public string FullPath => fullBundlePath;
        public string RelativeBundlePath => bundlePath;
        public bool IsSoftUnloaded => isSoftUnloaded;

        public MockBundleReference(string name, IEnumerable<MockUnityAsset> refs
            , string fullBundlePath, string bundlePath) : base(fullBundlePath, bundlePath)
        {
            this.refs = refs;
            BundleName = name;
            assets = new Dictionary<string, MockUnityAsset>();
            foreach (var asset in refs)
            {
                assets[asset.name] = asset;
            }
        }

        protected override void UnloadInternal(bool unloadAllAsset)
        {
            assets.Clear();
        }

        public override bool Contain(object asset)
        {
            if (asset is MockUnityAsset masset)
            {
                return assets.ContainsKey(masset.name);
            }
            throw new InvalidOperationException("Contract violationm, asset must be type of Unity Asset");
        }

        public override object LoadSingleSubAsset(string name)
        {
            if (assets.ContainsKey(name))
            {
                return assets[name];
            }
            throw new KeyNotFoundException($"Asset with name '{name}' not found in bundle '{BundleName}'");
        }

        public override object[] LoadAllSubAssets()
        {
            return assets.Values.ToArray();
        }

        public override void ReloadBundleInternal()
        {
            assets = new Dictionary<string, MockUnityAsset>();
            foreach (var asset in refs)
            {
                assets[asset.name] = asset;
            }
        }
    }

}
