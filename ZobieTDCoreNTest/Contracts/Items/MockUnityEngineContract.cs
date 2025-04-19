using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Contracts.Items.TimeProvider;
using ZobieTDCore.Contracts.Items;
using ZobieTDCoreNTest.Contracts.Items.TimeProvider;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;

namespace ZobieTDCoreNTest.Contracts.Items
{
    internal class MockUnityEngineContract : IUnityEngineContract
    {
        public string StreamingAssetPath => "";
        public bool IsDevelopmentBuild { get; set; }
        public ITimeProvider TimeProvider { get; set; } = new MockTimeProvider();
        public IAssetBundleReference LoadAssetBundleFromFile(string filePath)
            => MakeNewMockBundleRef?.Invoke(filePath) ?? throw new NotImplementedException();

        public Func<string, MockBundleReference>? MakeNewMockBundleRef;
    }
}
