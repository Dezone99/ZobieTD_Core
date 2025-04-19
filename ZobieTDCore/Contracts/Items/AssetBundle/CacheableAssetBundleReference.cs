using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    //TODO: Implement later
    public abstract class CacheableAssetBundleReference : IAssetBundleReference
    {
        private readonly Dictionary<string, IAssetReference> assets = new Dictionary<string, IAssetReference>();

        public abstract string BundleName { get; }
        public abstract bool Contain(IAssetReference asset);
        public abstract IAssetReference LoadAllSubAssets();

        //public IAssetReference LoadAllSubAssets()
        //{
        //    var subAssets = LoadAllSubAssetInternal();
        //    foreach (var reference in subAssets)
        //    {
        //        var name = reference.name;
        //        var asset = reference.asset;

        //    }
        //}
        public abstract IAssetReference LoadSingleSubAsset(string name);
        public void Unload(bool unloadAllAsset)
        {
            if (unloadAllAsset)
            {
                assets.Clear();
            }
            UnloadInternal(unloadAllAsset);
        }

        protected abstract (string name, object asset)[] LoadAllSubAssetInternal();
        protected abstract void UnloadInternal(bool unloadAllAsset);

    }
}
