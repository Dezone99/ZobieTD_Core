using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Contracts.Items.TimeProvider;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Contracts;
using ZobieTDCore.Services.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;
using ZobieTDCoreNTest.Contracts.Items.TimeProvider;

namespace ZobieTDCoreNTest.Services.AssetBundle
{
    public class AssetBundleUsageManagerTests
    {
        private AssetBundleUsageManager usageManager;
        private MockAssetReference zombie_idle_001_assetRef;
        private MockAssetReference zombie_idle_002_assetRef;
        private MockAssetReference zombie_run_001_assetRef;
        private MockBundleReference zombie_idle_bundleRef;
        private MockBundleReference zombie_run_bundleRef;
        private MockAssetReference unknown_assetRef;
        private MockUnityEngineContract mockEngineContract;
        [SetUp]
        public void Setup()
        {
            var mockTime = new MockTimeProvider();
            mockTime.SetTime(0f);

            mockEngineContract = new MockUnityEngineContract
            {
                TimeProvider = mockTime,
                IsDevelopmentBuild = false
            };
            ContractManager.Instance.SetUnityEngineContract(mockEngineContract);

            usageManager = new AssetBundleUsageManager();

            zombie_idle_001_assetRef = new MockAssetReference("zombie_idle_001");
            zombie_idle_002_assetRef = new MockAssetReference("zombie_idle_002");
            zombie_run_001_assetRef = new MockAssetReference("zombie_run_001");
            unknown_assetRef = new MockAssetReference("not_exist_asset");

            zombie_idle_bundleRef = new MockBundleReference("zombie_idle", new[] {
                zombie_idle_001_assetRef,
                zombie_idle_002_assetRef
            });

            zombie_run_bundleRef = new MockBundleReference("zombie_run", new[] {
                zombie_run_001_assetRef
            });
        }

        [Test]
        public void RegisterAssetReference_InvalidInDevBuild_ShouldThrow()
        {
            mockEngineContract.IsDevelopmentBuild = true;
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                usageManager.RegisterAssetReference(unknown_assetRef, zombie_idle_bundleRef);
            });
            Assert.That(ex.Message, Does.Contain("does not belong"));
        }

        [Test]
        public void RegisterMultipleAssetsFromDifferentBundles_ShouldTrackCorrectly()
        {
            usageManager.RegisterAssetReference(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference(zombie_idle_002_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference(zombie_run_001_assetRef, zombie_run_bundleRef);

            var assetRefs = usageManager.__GetAssetRefForTest();
            Assert.That(assetRefs[zombie_idle_001_assetRef].bundleName, Is.EqualTo("zombie_idle"));
            Assert.That(assetRefs[zombie_run_001_assetRef].bundleName, Is.EqualTo("zombie_run"));

            var trackers = usageManager.__GetBundleTrackerForTest();
            Assert.That(trackers["zombie_idle"].refCount, Is.EqualTo(2));
            Assert.That(trackers["zombie_run"].refCount, Is.EqualTo(1));
        }

        [Test]
        public void RegisterAndUnregisterMultipleTimes_ShouldUnloadProperly()
        {
            var mockTime = (MockTimeProvider)ContractManager.Instance.UnityEngineContract.TimeProvider;

            usageManager.RegisterAssetReference(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference(zombie_idle_002_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference(zombie_run_001_assetRef, zombie_run_bundleRef);

            usageManager.UnregisterAssetReference(zombie_idle_001_assetRef);
            usageManager.UnregisterAssetReference(zombie_idle_002_assetRef);
            usageManager.UnregisterAssetReference(zombie_run_001_assetRef);

            mockTime.Advance(65f);
            var toUnload = usageManager.GetNeedToUnloadBundle();
            Assert.That(toUnload, Does.Contain("zombie_idle"));
            Assert.That(toUnload, Does.Contain("zombie_run"));
        }

        [Test]
        public void RegisterAssetRef_MultipleTime()
        {
            usageManager.RegisterAssetReference(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference(zombie_idle_002_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference(zombie_idle_001_assetRef, zombie_idle_bundleRef);

            var trackers = usageManager.__GetBundleTrackerForTest();
            Assert.That(trackers["zombie_idle"].refCount, Is.EqualTo(3));

            var assetRefs = usageManager.__GetAssetRefForTest();
            Assert.That(assetRefs[zombie_idle_001_assetRef].bundleName, Is.EqualTo("zombie_idle"));
            Assert.That(assetRefs[zombie_idle_001_assetRef].count, Is.EqualTo(2));
        }
    }


}
