using System;
using System.Collections.Generic;
using System.Text;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Contracts.Items.TimeProvider;

namespace ZobieTDCore.Contracts.Items
{

    public interface IUnityEngineContract
    {
        public string StreamingAssetPath { get; }

        public IAssetBundleReference LoadAssetBundleFromFile(string filePath);

        public bool IsDevelopmentBuild { get; } 

        public ITimeProvider TimeProvider { get; }
    }
}
