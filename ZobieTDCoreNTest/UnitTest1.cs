#define UNITY_EDITOR

using Newtonsoft.Json.Linq;
using System.IO;
using ZobieTDCore.Services.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.UnityItem;
namespace ZobieTDCoreNTest
{
    public class Tests
    {

        private MockUnityAsset zombie_idle_001_assetRef;
        private MockUnityAsset zombie_idle_002_assetRef;
        private MockBundleReference zombie_idle_bundleRef;
        private MockUnityEngineContract mockUnityEngineContract;
        private string mockBundlePath = "path/to/bundle/zombie_idle";
        private string mockBundleName = "zombie_idle";
        private string mockSprName1 = "zombie_idle_001";
        private string mockSprName2 = "zombie_idle_002";

        [SetUp]
        public void Setup()
        {
            zombie_idle_001_assetRef = new MockUnityAsset(mockSprName1);
            zombie_idle_002_assetRef = new MockUnityAsset(mockSprName2);

            var path = Path.Combine("", mockBundlePath);

            zombie_idle_bundleRef = new MockBundleReference(mockBundleName, new[]
            {
                zombie_idle_001_assetRef,
                zombie_idle_002_assetRef
            }, path, mockBundlePath);
            zombie_idle_bundleRef.Unload(true);
        }


        [Test]
        public void TestDisposeRefFromData()
        {
            var @ref = new Ref() { Id = 1 };
            var data = new Data() { Id = 1, Name = "1", Ref = @ref };
            var data2 = new Data() { Id = 1, Name = "1", Ref = @ref };
            @ref = null;
            data.Ref = null;
            Assert.IsTrue(data.Ref == null);
        }

        private class Ref
        {
            public int Id { get; set; }
        }
        private class Data
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Ref? @Ref { get; set; }
        }


    }
}