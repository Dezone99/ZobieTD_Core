using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Services.AssetBundle.Base
{
    public interface IAssetBundleReference
    {
        string BundleName { get; }
        IEnumerable<IAssetReference> LoadAssets(string filter);
        IAssetReference LoadSingle(string name);
    }
}
