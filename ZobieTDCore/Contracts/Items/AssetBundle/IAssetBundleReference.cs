using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    public interface IAssetBundleReference
    {
        void Unload(bool unloadAllAsset);
        string BundleName { get; }
        IAssetReference LoadSingleAsset(string name);
        IAssetReference LoadAllAssets();
        bool Contain(IAssetReference asset);
    }
}
