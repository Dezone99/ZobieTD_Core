using ZobieTDCore.Contracts;
using ZobieTDCore.Services.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;
using ZobieTDCoreNTest.Contracts.Items.TimeProvider;
using ZobieTDCoreNTest.UnityItem;

namespace ZobieTDCoreNTest.Services.AssetBundle
{
    public class AssetBundleUsageManagerTests
    {
        private AssetBundleUsageManager usageManager;
        private MockUnityAsset zombie_idle_001_asset;
        private MockUnityAsset zombie_idle_002_asset;
        private MockUnityAsset zombie_run_001_asset;
        private MockUnityAsset unknown_asset;

        private AssetRef<MockUnityAsset> zombie_idle_001_assetRef;
        private AssetRef<MockUnityAsset> zombie_idle_002_assetRef;
        private AssetRef<MockUnityAsset> zombie_run_001_assetRef;
        private AssetRef<MockUnityAsset> unknown_assetRef;

        private MockBundleReference zombie_idle_bundleRef;
        private MockBundleReference zombie_run_bundleRef;
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

            zombie_idle_001_asset = new MockUnityAsset("zombie_idle_001");
            zombie_idle_002_asset = new MockUnityAsset("zombie_idle_002");
            zombie_run_001_asset = new MockUnityAsset("zombie_run_001");
            unknown_asset = new MockUnityAsset("not_exist_asset");

            zombie_idle_001_assetRef = new AssetRef<MockUnityAsset>(zombie_idle_001_asset);
            zombie_idle_002_assetRef = new AssetRef<MockUnityAsset>(zombie_idle_002_asset);
            zombie_run_001_assetRef = new AssetRef<MockUnityAsset>(zombie_run_001_asset);
            unknown_assetRef = new AssetRef<MockUnityAsset>(unknown_asset);

            zombie_idle_bundleRef = new MockBundleReference("zombie_idle", new[] {
                zombie_idle_001_asset,
                zombie_idle_002_asset
            });

            zombie_run_bundleRef = new MockBundleReference("zombie_run", new[] {
                zombie_run_001_asset
            });
        }

        [Test]
        public void RegisterAssetReference_InvalidInDevBuild_ShouldThrow()
        {
            mockEngineContract.IsDevelopmentBuild = true;
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                usageManager.RegisterAssetReference<MockUnityAsset>(unknown_assetRef, zombie_idle_bundleRef);
            });
            Assert.That(ex.Message, Does.Contain("does not belong"));
        }

        [Test]
        public void RegisterMultipleAssetsFromDifferentBundles_ShouldTrackCorrectly()
        {
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_002_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_run_001_assetRef, zombie_run_bundleRef);

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
            var assetRefs = usageManager.__GetAssetRefForTest();
            var trackers = usageManager.__GetBundleTrackerForTest();

            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_002_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_run_001_assetRef, zombie_run_bundleRef);

            usageManager.UnregisterAssetReference<MockUnityAsset>(zombie_idle_001_assetRef);
            Assert.That(assetRefs.ContainsKey(zombie_idle_001_assetRef), Is.EqualTo(false));
            Assert.That(assetRefs[zombie_idle_002_assetRef].bundleName, Is.EqualTo("zombie_idle"));
            Assert.That(assetRefs[zombie_run_001_assetRef].bundleName, Is.EqualTo("zombie_run"));

            usageManager.UnregisterAssetReference<MockUnityAsset>(zombie_idle_002_assetRef);
            usageManager.UnregisterAssetReference<MockUnityAsset>(zombie_run_001_assetRef);
            Assert.That(assetRefs.Count, Is.EqualTo(0));
            Assert.That(trackers["zombie_idle"].refCount, Is.EqualTo(0));
            Assert.That(trackers["zombie_run"].refCount, Is.EqualTo(0));

            var toUnload = usageManager.GetNeedToUnloadBundle();
            Assert.That(toUnload.Count, Is.EqualTo(0));
            mockTime.Advance(65f);
            toUnload = usageManager.GetNeedToUnloadBundle();
            Assert.That(toUnload, Does.Contain("zombie_idle"));
            Assert.That(toUnload, Does.Contain("zombie_run"));
        }

        [Test]
        public void RegisterAssetRef_MultipleTime()
        {
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_002_assetRef, zombie_idle_bundleRef);
            usageManager.RegisterAssetReference<MockUnityAsset>(zombie_idle_001_assetRef, zombie_idle_bundleRef);
            var trackers = usageManager.__GetBundleTrackerForTest();
            
            Assert.That(trackers["zombie_idle"].refCount, Is.EqualTo(3));

            var assetRefs = usageManager.__GetAssetRefForTest();
            Assert.That(assetRefs[zombie_idle_001_assetRef].bundleName, Is.EqualTo("zombie_idle"));
            Assert.That(assetRefs[zombie_idle_001_assetRef].count, Is.EqualTo(2));
        }
    }


}
