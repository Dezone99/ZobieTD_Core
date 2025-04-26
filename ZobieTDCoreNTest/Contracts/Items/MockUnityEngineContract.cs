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
using NUnit.Framework;
using ZobieTDCoreNTest.UnityItem;

namespace ZobieTDCoreNTest.Contracts.Items
{
    internal class MockUnityEngineContract : IUnityEngineContract
    {
        public string StreamingAssetPath => "";
        public bool IsDevelopmentBuild { get; set; }
        public ITimeProviderContract TimeProvider { get; set; } = new MockTimeProvider();

        public string PersistentDataPath => "";

        public IAssetBundleContract LoadAssetBundleFromFile(string filePath)
            => MakeNewMockBundleRef?.Invoke(filePath) ?? throw new NotImplementedException();

        public string GetUnityObjectName(object obj)
        {
            if (obj is MockUnityAsset masset)
            {
                return masset.name;
            }
            throw new InvalidOperationException("Contract violationm, asset must be type of Unity Asset");
        }

        public void LogToConsole(string log)
        {
            Console.WriteLine(log);
        }

        public Func<string, MockBundleReference>? MakeNewMockBundleRef;
    }
}
