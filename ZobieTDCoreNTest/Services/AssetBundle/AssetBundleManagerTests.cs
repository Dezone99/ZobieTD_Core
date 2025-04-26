using ZobieTDCore.Services.AssetBundle;
using ZobieTDCore.Contracts;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;
using ZobieTDCoreNTest.UnityItem;
using ZobieTDCore.Services.Logger;

namespace ZobieTDCoreNTest.Services.AssetBundle
{
    public class AssetBundleManagerTests
    {
        private AssetBundleManager<MockUnityAsset> manager;
        private MockUnityAsset zombie_idle_001_assetRef;
        private MockUnityAsset zombie_idle_002_assetRef;
        private MockBundleReference zombie_idle_bundleRef;
        private MockUnityEngineContract mockUnityEngineContract;
        [SetUp]
        public void Setup()
        {
            zombie_idle_001_assetRef = new MockUnityAsset("zombie_idle_001");
            zombie_idle_002_assetRef = new MockUnityAsset("zombie_idle_002");

            mockUnityEngineContract = new MockUnityEngineContract();
            var bundleName = "zombie_idle";
            var path = Path.Combine(mockUnityEngineContract.StreamingAssetPath, bundleName);
            zombie_idle_bundleRef = new MockBundleReference(bundleName, new[]
            {
                zombie_idle_001_assetRef,
                zombie_idle_002_assetRef
            }, path);
            ContractManager.Instance.SetUnityEngineContract(mockUnityEngineContract);
            TDLogger.Init(mockUnityEngineContract);
            manager = new AssetBundleManager<MockUnityAsset>();
        }


        [Test]
        public void LoadSingleAsset_ShouldReturnCorrectAsset()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) =>
            {
                Assert.AreEqual(filepath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.BundleName));
                return zombie_idle_bundleRef;
            };
            var owner = new MockAssetOwner();
            var assetRef = manager.LoadSingleSubSpriteAsset(owner, "zombie_idle", "zombie_idle_001");
            var asset = assetRef.Ref as MockUnityAsset;
            Assert.IsNotNull(asset);
            Assert.That(asset.name, Is.EqualTo("zombie_idle_001"));
            Assert.AreEqual(asset, zombie_idle_001_assetRef);
        }

        [Test]
        public void LoadAllAsset_ShouldReturnAssetRef()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) =>
            {
                Assert.AreEqual(filepath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.BundleName));
                return zombie_idle_bundleRef;
            };
            var owner = new MockAssetOwner();
            var allAssetsRef = manager.LoadAnimationSpriteAsset(owner, "zombie_idle");
            Assert.IsNotNull(allAssetsRef);
            Assert.That(allAssetsRef.Length, Is.EqualTo(2));
            Assert.That(allAssetsRef[0].Ref, Is.EqualTo(zombie_idle_001_assetRef));
            Assert.That(allAssetsRef[1].Ref, Is.EqualTo(zombie_idle_002_assetRef));
        }

        [Test]
        public void ReleaseSpriteAssetRef_ShouldRemoveUsage()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) =>
            {
                Assert.AreEqual(filepath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.BundleName));
                return zombie_idle_bundleRef;
            };
            var owner = new MockAssetOwner();
            var asset = manager.LoadSingleSubSpriteAsset(owner, "zombie_idle", "zombie_idle_001");
            var tracker = manager.__GetBundleUsageManagerForTest().__GetAssetRefForTest();

            Assert.That(tracker.ContainsKey(asset), Is.True);
            manager.ReleaseSpriteAssetRef(owner, asset);
            Assert.That(tracker.ContainsKey(asset), Is.False);
        }

        [Test]
        public void ReleaseAnimationAssetRef_ShouldRemoveUsageAndCache()
        {
            // Arrange
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) =>
            {
                Assert.AreEqual(filepath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.BundleName));
                return zombie_idle_bundleRef;
            };

            var owner = new MockAssetOwner();
            var assets = manager.LoadAnimationSpriteAsset(owner, "zombie_idle");

            var bundleUsageManager = manager.__GetBundleUsageManagerForTest();
            var animationToBundleMap = manager.__GetAnimationBundleMap();
            var assetTracker = bundleUsageManager.__GetAssetRefForTest();
            var bundleTracker = bundleUsageManager.__GetBundleTrackerForTest();

            var loadedBundles = manager.GetLoadedBundles();

            // Assert pre-condition
            Assert.That(assetTracker.Count, Is.GreaterThan(0), "Should have assets tracked before release.");
            Assert.That(bundleTracker.Count, Is.GreaterThan(0), "Should have bundle tracked before release.");
            Assert.That(loadedBundles, Does.Contain("zombie_idle"), "Bundle zombie_idle should be loaded before release.");
            Assert.That(animationToBundleMap.ContainsKey(assets), Is.True, "Animation is cached before release.");
            // Act
            manager.ReleaseAnimationAssetRef(owner, assets);

            Assert.That(manager.GetLoadedBundles(), Does.Not.Contain("zombie_idle"), "Bundle zombie_idle should have been unloaded from manager.");
            Assert.That(animationToBundleMap.ContainsKey(assets), Is.False, "Animation is cached before release.");
            Assert.That(assetTracker.ContainsKey(assets), Is.False, $"Animation ssset should have been untracked.");
            foreach (var asset in assets)
            {
                Assert.That(assetTracker.ContainsKey(asset), Is.False, $"Asset {mockUnityEngineContract.GetUnityObjectName(asset.Ref)} should have been untracked.");
            }
        }


        [Test]
        public void MultipleOwnerCallToLoadAsset()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (filepath) =>
            {
                Assert.AreEqual(filepath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.BundleName));
                return zombie_idle_bundleRef;
            };
            var tracker = manager.__GetBundleUsageManagerForTest().__GetAssetRefForTest();
            var owner = new MockAssetOwner();
            var owner2 = new MockAssetOwner();
            var owner3 = new MockAssetOwner();

            var assetRef = manager.LoadSingleSubSpriteAsset(owner, "zombie_idle", "zombie_idle_001");
            var assetRef2 = manager.LoadSingleSubSpriteAsset(owner2, "zombie_idle", "zombie_idle_001");
            var assetRef3 = manager.LoadSingleSubSpriteAsset(owner3, "zombie_idle", "zombie_idle_001");

            Assert.That(assetRef, Is.EqualTo(assetRef2));
            Assert.That(assetRef2, Is.EqualTo(assetRef3));

            // Should equal to 3
            Assert.That(tracker[assetRef].count, Is.EqualTo(3));
            Assert.That(tracker[assetRef].bundleName, Is.EqualTo("zombie_idle"));


        }
    }

}
