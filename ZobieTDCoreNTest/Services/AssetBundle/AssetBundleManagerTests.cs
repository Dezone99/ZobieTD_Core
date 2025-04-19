using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Services.AssetBundle;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items.TimeProvider;
using System.Reflection;
using System.Diagnostics.Contracts;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;

namespace ZobieTDCoreNTest.Services.AssetBundle
{
    public class AssetBundleManagerTests
    {
        private AssetBundleManager manager;
        private MockAssetReference zombie_idle_001_assetRef;
        private MockAssetReference zombie_idle_002_assetRef;
        private MockBundleReference zombie_idle_bundleRef;
        private MockUnityEngineContract mockUnityEngineContract;
        [SetUp]
        public void Setup()
        {
            zombie_idle_001_assetRef = new MockAssetReference("zombie_idle_001");
            zombie_idle_002_assetRef = new MockAssetReference("zombie_idle_002");

            zombie_idle_bundleRef = new MockBundleReference("zombie_idle", new[]
            {
                zombie_idle_001_assetRef,
                zombie_idle_002_assetRef
            });

            mockUnityEngineContract = new MockUnityEngineContract();
            ContractManager.Instance.SetUnityEngineContract(mockUnityEngineContract);
            manager = AssetBundleManager.__MakeBundleManagerForTest();
        }

        [Test]
        public void LoadSingleAsset_ShouldReturnCorrectAsset()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) => zombie_idle_bundleRef;
            var asset = manager.LoadSingleSubAsset("zombie_idle", "zombie_idle_001");
            Assert.That(asset.Name, Is.EqualTo("zombie_idle_001"));

            // Because zombie_idle_001_assetRef was cached in mock bundle reference,
            // Client should implement follow this pattern
            Assert.AreEqual(asset, zombie_idle_001_assetRef);
        }

        [Test]
        public void LoadAllAsset_ShouldReturnAssetRef()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) => zombie_idle_bundleRef;
            var allAssetsRef = manager.LoadAllSubAsset("zombie_idle");

            // Vì là load toàn bộ sub asset trong bundle nên sẽ lấy tên của bundle
            Assert.That(allAssetsRef.Name, Is.EqualTo("zombie_idle"));
        }

        [Test]
        public void ReleaseAssetRef_ShouldRemoveUsage()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) => zombie_idle_bundleRef;
            var asset = manager.LoadSingleSubAsset("zombie_idle", "zombie_idle_001");
            manager.ReleaseAssetRef(asset);

            var tracker = manager.__GetBundleUsageManagerForTest().__GetAssetRefForTest();
            Assert.That(tracker.ContainsKey(asset), Is.False);
        }
    }

}
