using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts;
using ZobieTDCore.Services.AssetBundle;
using ZobieTDCore.Services.Logger;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;
using ZobieTDCoreNTest.UnityItem;
using ZobieTDCore.Contracts.Items.AssetBundle;
using static ZobieTDCore.Services.AssetBundle.AssetBundleUsageManager;

namespace ZobieTDCoreNTest.Services.AssetBundle
{
    class StressTestBundleRef : MockBundleReference
    {
        public StressTestBundleRef(string name, IEnumerable<MockUnityAsset> refs, string fullBundlePath, string bundlePath) : base(name, refs, fullBundlePath, bundlePath)
        {
        }
        public bool IsLoaded { get; set; }

    }
    class ZombieRunningBundleRef : StressTestBundleRef
    {
        public static readonly string BUNDLE_NAME = "zombie_running";
        public static readonly string BUNDLE_PATH_RELATIVE = "path/to/zombie_running_bundle";
        public static readonly string ASSET_1_NAME = "zombie_running_001";
        public static readonly string ASSET_2_NAME = "zombie_running_002";
        public static readonly string ASSET_3_NAME = "zombie_running_003";

        private static readonly MockUnityAsset zombie_running_001_assetRef = new MockUnityAsset(ASSET_1_NAME);
        private static readonly MockUnityAsset zombie_running_002_assetRef = new MockUnityAsset(ASSET_2_NAME);
        private static readonly MockUnityAsset zombie_running_003_assetRef = new MockUnityAsset(ASSET_3_NAME);
        private static readonly MockUnityAsset[] DefaultAssets = new[]
        {
            zombie_running_001_assetRef,
            zombie_running_002_assetRef,
            zombie_running_003_assetRef
        };

        public ZombieRunningBundleRef(string fullBundlePath, string bundlePath)
            : base(BUNDLE_NAME
                  , DefaultAssets
                  , fullBundlePath
                  , bundlePath)
        {
        }

    }
    class ZombieIdleBundleRef : StressTestBundleRef
    {
        public static readonly string BUNDLE_NAME = "zombie_idle";
        public static readonly string BUNDLE_PATH_RELATIVE = "path/to/zombie_idle_bundle";
        public static readonly string ASSET_1_NAME = "zombie_idle_001";
        public static readonly string ASSET_2_NAME = "zombie_idle_002";
        public static readonly string ASSET_3_NAME = "zombie_idle_003";

        private static readonly MockUnityAsset zombie_idle_001_assetRef = new MockUnityAsset(ASSET_1_NAME);
        private static readonly MockUnityAsset zombie_idle_002_assetRef = new MockUnityAsset(ASSET_2_NAME);
        private static readonly MockUnityAsset zombie_idle_003_assetRef = new MockUnityAsset(ASSET_3_NAME);
        private static readonly MockUnityAsset[] DefaultAssets = new[]
        {
            zombie_idle_001_assetRef,
            zombie_idle_002_assetRef,
            zombie_idle_003_assetRef
        };

        public ZombieIdleBundleRef(string fullBundlePath, string bundlePath)
            : base(BUNDLE_NAME
                  , DefaultAssets
                  , fullBundlePath
                  , bundlePath)
        {
        }
    }

    internal class AssetBundleManager_StressTests
    {
        private AssetBundleManager<MockUnityAsset> manager;
        private ZombieRunningBundleRef zombie_running_bundleRef;
        private ZombieIdleBundleRef zombie_idle_bundleRef;
        private MockUnityEngineContract mockUnityEngineContract;


        private AssetBundleUsageManager assetBundleUsageManager;
        private Dictionary<string, IAssetBundleContract> loadedBundles;
        private Dictionary<IAssetBundleContract, HashSet<(object assetOwner, string bundlePath, object assetRef)>> cachedAssetOwner;
        private Dictionary<AssetRef<MockUnityAsset>, IAssetBundleContract> singleAssetToBundle;
        private Dictionary<IAssetBundleContract, Dictionary<string, AssetRef<MockUnityAsset>>> bundleToSingleAsset;
        private Dictionary<AssetRef<MockUnityAsset>[], IAssetBundleContract> animationToBundle;
        private Dictionary<IAssetBundleContract, AssetRef<MockUnityAsset>[]> bundleToAnimation;
        private Dictionary<string, Tracker> bundleTrackers;
        private Dictionary<object, (string bundlePath, int count)> assetRefsTracker;

        [SetUp]
        public void Setup()
        {
            mockUnityEngineContract = new MockUnityEngineContract();
            ContractManager.Instance.SetUnityEngineContract(mockUnityEngineContract);
            TDLogger.Init(mockUnityEngineContract);
            manager = new AssetBundleManager<MockUnityAsset>();

            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                if (bundlePath == ZombieIdleBundleRef.BUNDLE_PATH_RELATIVE)
                {
                    if (zombie_idle_bundleRef != null
                        && !zombie_idle_bundleRef.IsUnloaded())
                    {
                        throw new Exception("Bundle is loaded already ");
                    }
                    zombie_idle_bundleRef = new ZombieIdleBundleRef(fullPath, bundlePath);
                    return zombie_idle_bundleRef;
                }
                if (bundlePath == ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE)
                {
                    if (zombie_running_bundleRef != null
                        && !zombie_running_bundleRef.IsUnloaded())
                    {
                        throw new Exception("Bundle is loaded already ");
                    }
                    zombie_running_bundleRef = new ZombieRunningBundleRef(fullPath, bundlePath);
                    return zombie_running_bundleRef;
                }
                throw new Exception("Not found bundle");
            };

            assetBundleUsageManager = manager.__GetBundleUsageManagerForTest();
            bundleTrackers = assetBundleUsageManager.__GetBundleTrackerForTest();
            assetRefsTracker = assetBundleUsageManager.__GetAssetRefForTest();
            cachedAssetOwner = manager.__GetCachedAssetOwner();
            singleAssetToBundle = manager.__GetSingleAssetToBundle();
            bundleToSingleAsset = manager.__GetBundleToSingleAsset();
            animationToBundle = manager.__GetAnimationBundleMap();
            loadedBundles = manager.__GetLoadedBundles();
            bundleToAnimation = manager.__GetBundleToAnimation();
        }

        [Test]
        public void StressTest_1()
        {
            var owner = new MockAssetOwner();
            var owner2 = new MockAssetOwner();

            Assert.AreEqual(loadedBundles.Count, 0);
            Assert.AreEqual(cachedAssetOwner.Count, 0);
            Assert.AreEqual(singleAssetToBundle.Count, 0);
            Assert.AreEqual(bundleToSingleAsset.Count, 0);
            Assert.AreEqual(animationToBundle.Count, 0);
            Assert.AreEqual(bundleToAnimation.Count, 0);

            var assetRef = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_1_NAME);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 1);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef)
                ), true);
            Assert.AreEqual(singleAssetToBundle.Count, 1);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(animationToBundle.Count, 0);
            Assert.AreEqual(bundleToAnimation.Count, 0);


            var assetRef2 = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_1_NAME);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 1);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef)
                ), true);
            Assert.AreEqual(singleAssetToBundle.Count, 1);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(animationToBundle.Count, 0);
            Assert.AreEqual(bundleToAnimation.Count, 0);

            var assetRef3 = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_2_NAME);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 2);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef3)
                ), true);
            Assert.AreEqual(singleAssetToBundle.Count, 2);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(bundleToSingleAsset[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(animationToBundle.Count, 0);
            Assert.AreEqual(bundleToAnimation.Count, 0);


            var assetRef4 = manager.LoadAnimationSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 3);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 3);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef3)
                ), true);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef4)
                ), true);
            Assert.AreEqual(singleAssetToBundle.Count, 2);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(bundleToSingleAsset[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(animationToBundle.Count, 1);
            Assert.AreEqual(bundleToAnimation.Count, 1);


            manager.ReleaseAnimationAssetRef(owner, assetRef4, forceCleanUpIfNoRefCount: false);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 2);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef3)
                ), true);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef4)
                ), false);
            Assert.AreEqual(singleAssetToBundle.Count, 2);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(bundleToSingleAsset[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(animationToBundle.Count, 1);
            Assert.AreEqual(bundleToAnimation.Count, 1);

            manager.UpdateCachedAssetBundle(forceUnloadWithoutTimeout: true);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 2);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef3)
                ), true);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef4)
                ), false);
            Assert.AreEqual(singleAssetToBundle.Count, 2);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(bundleToSingleAsset[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(animationToBundle.Count, 1);
            Assert.AreEqual(bundleToAnimation.Count, 1);

            manager.ReleaseSpriteAssetRef(owner, assetRef3, forceCleanUpIfNoRefCount: false);
            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 1);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef3)
                ), false);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                  (owner, zombie_running_bundleRef.BundlePath, assetRef)
                  ), true);
            Assert.AreEqual(singleAssetToBundle.Count, 2);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(bundleToSingleAsset[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(animationToBundle.Count, 1);
            Assert.AreEqual(bundleToAnimation.Count, 1);


            manager.ReleaseSpriteAssetRef(owner, assetRef, forceCleanUpIfNoRefCount: false);
            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 0);
            Assert.AreEqual(loadedBundles.Count, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
            Assert.AreEqual(cachedAssetOwner.Count, 1);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Count, 0);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                (owner, zombie_running_bundleRef.BundlePath, assetRef3)
                ), false);
            Assert.AreEqual(cachedAssetOwner[zombie_running_bundleRef].Contains(
                  (owner, zombie_running_bundleRef.BundlePath, assetRef)
                  ), false);
            Assert.AreEqual(singleAssetToBundle.Count, 2);
            Assert.AreEqual(bundleToSingleAsset.Count, 1);
            Assert.AreEqual(bundleToSingleAsset[zombie_running_bundleRef].Count, 2);
            Assert.AreEqual(animationToBundle.Count, 1);
            Assert.AreEqual(bundleToAnimation.Count, 1);

            manager.UpdateCachedAssetBundle(forceUnloadWithoutTimeout: true);

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 0);
            Assert.AreEqual(loadedBundles.Count, 0);
            Assert.AreEqual(cachedAssetOwner.Count, 0);
            Assert.AreEqual(singleAssetToBundle.Count, 0);
            Assert.AreEqual(bundleToSingleAsset.Count, 0);
            Assert.AreEqual(bundleToSingleAsset.Count, 0);
            Assert.AreEqual(animationToBundle.Count, 0);
            Assert.AreEqual(bundleToAnimation.Count, 0);
        }


        [Test]
        public void StressTest_MultiOwnerLoadRelease()
        {
            var owner1 = new MockAssetOwner();
            var owner2 = new MockAssetOwner();

            var assetRef1 = manager.LoadSingleSubSpriteAsset(owner1
                , ZombieIdleBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieIdleBundleRef.ASSET_1_NAME);

            var assetRef2 = manager.LoadSingleSubSpriteAsset(owner2
                , ZombieIdleBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieIdleBundleRef.ASSET_1_NAME);

            Assert.AreEqual(bundleTrackers[zombie_idle_bundleRef.BundlePath].refCount, 2);

            manager.ReleaseSpriteAssetRef(owner1, assetRef1, forceCleanUpIfNoRefCount: false);
            Assert.AreEqual(bundleTrackers[zombie_idle_bundleRef.BundlePath].refCount, 1);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_idle_bundleRef.BundlePath), true);

            manager.ReleaseSpriteAssetRef(owner2, assetRef2, forceCleanUpIfNoRefCount: false);
            Assert.AreEqual(bundleTrackers[zombie_idle_bundleRef.BundlePath].refCount, 0);

            manager.UpdateCachedAssetBundle(forceUnloadWithoutTimeout: true);
            Assert.AreEqual(loadedBundles.ContainsKey(zombie_idle_bundleRef.BundlePath), false);
        }

        [Test]
        public void StressTest_SwitchLoadDifferentBundles()
        {
            var owner = new MockAssetOwner();

            for (int i = 0; i < 50; i++)
            {
                var assetRefRun = manager.LoadSingleSubSpriteAsset(owner
                    , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                    , ZombieRunningBundleRef.ASSET_1_NAME);

                var assetRefIdle = manager.LoadSingleSubSpriteAsset(owner
                    , ZombieIdleBundleRef.BUNDLE_PATH_RELATIVE
                    , ZombieIdleBundleRef.ASSET_1_NAME);

                manager.ReleaseSpriteAssetRef(owner, assetRefRun, forceCleanUpIfNoRefCount: true);
                manager.ReleaseSpriteAssetRef(owner, assetRefIdle, forceCleanUpIfNoRefCount: true);
            }

            manager.UpdateCachedAssetBundle(forceUnloadWithoutTimeout: true);

            Assert.AreEqual(loadedBundles.Count, 0);
            Assert.AreEqual(cachedAssetOwner.Count, 0);
        }


        [Test]
        public void StressTest_LoadAnimationThenSingleAssets()
        {
            var owner = new MockAssetOwner();

            var animRefs = manager.LoadAnimationSpriteAsset(owner, ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE);

            var singleAssetRef = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_1_NAME);

            Assert.AreEqual(animationToBundle.ContainsKey(animRefs), true);
            Assert.AreEqual(singleAssetToBundle.ContainsKey(singleAssetRef), true);

            manager.ReleaseAnimationAssetRef(owner, animRefs, forceCleanUpIfNoRefCount: false);
            manager.ReleaseSpriteAssetRef(owner, singleAssetRef, forceCleanUpIfNoRefCount: false);

            manager.UpdateCachedAssetBundle(forceUnloadWithoutTimeout: true);

            Assert.AreEqual(loadedBundles.Count, 0);
        }

        [Test]
        public void StressTest_ReleaseTwiceSameAsset()
        {
            var owner = new MockAssetOwner();
            var assetRef = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_1_NAME);

            manager.ReleaseSpriteAssetRef(owner, assetRef, forceCleanUpIfNoRefCount: false);

            Assert.Throws<InvalidOperationException>(() =>
            {
                manager.ReleaseSpriteAssetRef(owner, assetRef, forceCleanUpIfNoRefCount: false);
            });
        }


        [Test]
        public void StressTest_ForceUnloadStillReferenced()
        {
            var owner = new MockAssetOwner();
            var assetRef = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_1_NAME);

            manager.UpdateCachedAssetBundle(forceUnloadWithoutTimeout: true);

            Assert.AreEqual(loadedBundles.ContainsKey(zombie_running_bundleRef.BundlePath), true);
        }

        [Test]
        public void StressTest_OwnerGCBehavior()
        {
            var owner = new MockAssetOwner();
            var assetRef = manager.LoadSingleSubSpriteAsset(owner
                , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                , ZombieRunningBundleRef.ASSET_1_NAME);

            owner = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.AreEqual(bundleTrackers[zombie_running_bundleRef.BundlePath].refCount, 1);

            // Manager không tự giải phóng, chỉ owner mất thì vẫn phải giải quyết thủ công
        }

        [Test]
        public void StressTest_LoadSameAssetMultipleTimes()
        {
            var owner = new MockAssetOwner();
            var refs = new List<AssetRef<MockUnityAsset>>();

            for (int i = 0; i < 10; i++)
            {
                refs.Add(manager.LoadSingleSubSpriteAsset(owner
                    , ZombieRunningBundleRef.BUNDLE_PATH_RELATIVE
                    , ZombieRunningBundleRef.ASSET_1_NAME));
            }

            Assert.AreEqual(refs.Distinct().Count(), 1); // Tất cả load đều ra cùng 1 AssetRef
        }
    }
}
