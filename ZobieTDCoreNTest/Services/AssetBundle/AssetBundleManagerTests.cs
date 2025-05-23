﻿using ZobieTDCore.Services.AssetBundle;
using ZobieTDCore.Contracts;
using ZobieTDCoreNTest.Contracts.Items.AssetBundle;
using ZobieTDCoreNTest.Contracts.Items;
using ZobieTDCoreNTest.UnityItem;
using ZobieTDCore.Services.Logger;
using System.Linq;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCoreNTest.Services.AssetBundle
{
    public class AssetBundleManagerTests
    {
        private AssetBundleManager<MockUnityAsset> manager;
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

            mockUnityEngineContract = new MockUnityEngineContract();
            var path = Path.Combine(mockUnityEngineContract.StreamingAssetPath, mockBundlePath);
            zombie_idle_bundleRef = new MockBundleReference(mockBundleName, new[]
            {
                zombie_idle_001_assetRef,
                zombie_idle_002_assetRef
            }, path, mockBundlePath);
            ContractManager.Instance.SetUnityEngineContract(mockUnityEngineContract);
            TDLogger.Init(mockUnityEngineContract);
            manager = new AssetBundleManager<MockUnityAsset>();
        }


        [Test]
        public void LoadSingleAsset_ShouldReturnCorrectAsset()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                return zombie_idle_bundleRef;
            };
            var owner = new MockAssetOwner();
            var assetRef = manager.LoadSingleSubAsset(owner, mockBundlePath, "zombie_idle_001");
            var asset = assetRef.Ref as MockUnityAsset;
            Assert.IsNotNull(asset);
            Assert.That(asset.name, Is.EqualTo("zombie_idle_001"));
            Assert.AreEqual(asset, zombie_idle_001_assetRef);
        }

        [Test]
        public void LoadAllAsset_ShouldReturnAssetRef()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                return zombie_idle_bundleRef;
            };
            var owner = new MockAssetOwner();
            var allAssetsRef = manager.LoadAllSubAssets(owner, mockBundlePath);
            Assert.IsNotNull(allAssetsRef);
            Assert.That(allAssetsRef.Length, Is.EqualTo(2));
            Assert.That(allAssetsRef[0].Ref, Is.EqualTo(zombie_idle_001_assetRef));
            Assert.That(allAssetsRef[1].Ref, Is.EqualTo(zombie_idle_002_assetRef));
        }

        [Test]
        public void ReleaseSpriteAssetRef_ShouldRemoveUsage()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                return zombie_idle_bundleRef;
            };
            var owner = new MockAssetOwner();
            var asset = manager.LoadSingleSubAsset(owner, mockBundlePath, "zombie_idle_001");
            var tracker = manager.__GetBundleUsageManagerForTest().__GetAssetRefForTest();

            Assert.That(tracker.ContainsKey(asset), Is.True);
            manager.ReleaseSpriteAssetRef(owner, asset);
            Assert.That(tracker.ContainsKey(asset), Is.False);
        }

        [Test]
        public void ReleaseAnimationAssetRef_ShouldRemoveUsageAndCache()
        {
            // Arrange
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                return zombie_idle_bundleRef;
            };

            var owner = new MockAssetOwner();
            var assets = manager.LoadAllSubAssets(owner, mockBundlePath);

            var bundleUsageManager = manager.__GetBundleUsageManagerForTest();
            var animationToBundleMap = manager.__GetAnimationBundleMap();
            var assetTracker = bundleUsageManager.__GetAssetRefForTest();
            var bundleTracker = bundleUsageManager.__GetBundleTrackerForTest();

            var loadedBundles = manager.__GetLoadedBundles();

            // Assert pre-condition
            Assert.That(assetTracker.Count, Is.GreaterThan(0), "Should have assets tracked before release.");
            Assert.That(bundleTracker.Count, Is.GreaterThan(0), "Should have bundle tracked before release.");
            Assert.That(loadedBundles, Does.ContainKey(mockBundlePath), "Bundle zombie_idle should be loaded before release.");
            Assert.That(animationToBundleMap.ContainsKey(assets), Is.True, "Animation is cached before release.");
            // Act
            manager.ReleaseAnimationAssetRef(owner, assets, forceCleanUpIfNoRefCount: true);

            Assert.That(manager.__GetLoadedBundles(), Does.Not.ContainKey(mockBundlePath), "Bundle zombie_idle should have been unloaded from manager.");
            Assert.That(animationToBundleMap.ContainsKey(assets), Is.False, "Animation is cached before release.");
            Assert.That(assetTracker.ContainsKey(assets), Is.False, $"Animation ssset should have been untracked.");
            foreach (var asset in assets)
            {
                Assert.That(asset.Ref == null, Is.True, $"Ref have been untracked.");
                Assert.That(assetTracker.ContainsKey(asset), Is.False, $"Asset should have been untracked.");
            }
        }

        [Test]
        public void LoadAnimation_ShouldSoftUnloadBundle()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);

            var bundle = manager.__GetAnimationBundleMap()[animSprites];
            Assert.That(bundle.IsUnloaded(), Is.True);
            Assert.True(((MockBundleReference)bundle).IsSoftUnloaded);
        }

        [Test]
        public void LoadAsset_ShouldReloadBundleIfUnloaded()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);

            var bundle = manager.__GetAnimationBundleMap()[animSprites];
            Assert.That(bundle.IsUnloaded(), Is.True);
            Assert.True(((MockBundleReference)bundle).IsSoftUnloaded);

            var sprite = manager.LoadSingleSubAsset(owner, mockBundlePath, mockSprName1);
            Assert.That(bundle.IsUnloaded(), Is.False);
            Assert.False(((MockBundleReference)bundle).IsSoftUnloaded);
        }

        [Test]
        public void MultipleOwnerCallToLoadAsset()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                return zombie_idle_bundleRef;
            };
            var tracker = manager.__GetBundleUsageManagerForTest().__GetAssetRefForTest();
            var owner = new MockAssetOwner();
            var owner2 = new MockAssetOwner();
            var owner3 = new MockAssetOwner();

            var assetRef = manager.LoadSingleSubAsset(owner, mockBundlePath, "zombie_idle_001");
            var assetRef2 = manager.LoadSingleSubAsset(owner2, mockBundlePath, "zombie_idle_001");
            var assetRef3 = manager.LoadSingleSubAsset(owner3, mockBundlePath, "zombie_idle_001");

            Assert.That(assetRef, Is.EqualTo(assetRef2));
            Assert.That(assetRef2, Is.EqualTo(assetRef3));

            // Should equal to 3
            Assert.That(tracker[assetRef].count, Is.EqualTo(3));
            Assert.That(tracker[assetRef].bundlePath, Is.EqualTo(zombie_idle_bundleRef.RelativeBundlePath));
        }

        [Test]
        public void LoadSingleAsset_ShouldUseBundlePathInCacheKey()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();

            var ownerHashset = manager.__GetCachedAssetOwner();
            var singleRefToBundle = manager.__GetSingleAssetToBundle();
            var loadedBundle = manager.__GetLoadedBundles();
            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            var sprite = manager.LoadSingleSubAsset(owner, mockBundlePath, "zombie_idle_001");
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].ContainsKey((owner, zombie_idle_bundleRef.RelativeBundlePath, sprite)));
            Assert.IsTrue(loadedBundle.ContainsKey(zombie_idle_bundleRef.BundlePath));
            Assert.NotNull(sprite.Ref);
        }

        [Test]
        public void LoadAnimationAssets_ShouldUseBundlePathInCacheKey()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var owner2 = new MockAssetOwner();
            var ownerHashset = manager.__GetCachedAssetOwner();
            var singleSprToBundle = manager.__GetSingleAssetToBundle();
            var bundleToSingleSpr = manager.__GetBundleToSingleAsset();
            var animToBundle = manager.__GetAnimationBundleMap();
            var loadedBundle = manager.__GetLoadedBundles();
            var bundleToAnim = manager.__GetBundleToAnimation();

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);


            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);
            Assert.IsTrue(loadedBundle.ContainsKey(zombie_idle_bundleRef.RelativeBundlePath));
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].ContainsKey((owner, zombie_idle_bundleRef.RelativeBundlePath, animSprites)));
            Assert.IsTrue(animToBundle.ContainsKey(animSprites));
            Assert.IsTrue(bundleToAnim[zombie_idle_bundleRef] == animSprites);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            Assert.AreEqual(2, animSprites.Length);

            // Gọi load lần 2 nhưng chung owner
            var animSprites2 = manager.LoadAllSubAssets(owner, mockBundlePath);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            Assert.AreEqual(2, animSprites2.Length);


            var animSprites3 = manager.LoadAllSubAssets(owner2, mockBundlePath);

            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            Assert.AreEqual(2, animSprites3.Length);
        }

        [Test]
        public void ReleaseAnimationAssetRef_WithNoForceToCleanUp_RemoveCacheWhenNoRef()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var ownerHashset = manager.__GetCachedAssetOwner();
            var singleSprToBundle = manager.__GetSingleAssetToBundle();
            var bundleToSingleSpr = manager.__GetBundleToSingleAsset();
            var animToBundle = manager.__GetAnimationBundleMap();
            var loadedBundle = manager.__GetLoadedBundles();
            var bundleToAnim = manager.__GetBundleToAnimation();

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);
            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            foreach (var animSprite in animSprites)
            {
                Assert.IsTrue(animSprite.Ref != null);
            }
            manager.ReleaseAnimationAssetRef(owner, animSprites, forceCleanUpIfNoRefCount: false);

            foreach (var animSprite in animSprites)
            {
                Assert.IsTrue(animSprite.Ref != null);
            }
            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 0);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            Assert.That(animToBundle.ContainsKey(animSprites), Is.True);
            Assert.IsTrue(animToBundle[animSprites] == zombie_idle_bundleRef);
            Assert.IsTrue(bundleToAnim[zombie_idle_bundleRef] == animSprites);
        }

        [Test]
        public void ReleaseAnimationAssetRef_WithForceToCleanUp_RemoveCacheWhenNoRef()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var ownerHashset = manager.__GetCachedAssetOwner();
            var singleSprToBundle = manager.__GetSingleAssetToBundle();
            var bundleToSingleSpr = manager.__GetBundleToSingleAsset();
            var animToBundle = manager.__GetAnimationBundleMap();
            var loadedBundle = manager.__GetLoadedBundles();
            var bundleToAnim = manager.__GetBundleToAnimation();

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);
            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            foreach (var animSprite in animSprites)
            {
                Assert.IsTrue(animSprite.Ref != null);
            }
            manager.ReleaseAnimationAssetRef(owner, animSprites, forceCleanUpIfNoRefCount: true);

            // Disposed all Ref in AssetRef
            foreach (var animSprite in animSprites)
            {
                Assert.IsTrue(animSprite.Ref == null);
            }
            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);
        }

        [Test]
        public void ReleaseAnimationAssetRef_RemoveAnimationOnly_WhenBundleHasOtherRefs()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var owner2 = new MockAssetOwner();
            var ownerHashset = manager.__GetCachedAssetOwner();
            var singleSprToBundle = manager.__GetSingleAssetToBundle();
            var bundleToSingleSpr = manager.__GetBundleToSingleAsset();
            var animToBundle = manager.__GetAnimationBundleMap();
            var loadedBundle = manager.__GetLoadedBundles();
            var bundleToAnim = manager.__GetBundleToAnimation();

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);
            var sprite = manager.LoadSingleSubAsset(owner, mockBundlePath, mockSprName1);
            var sprite2 = manager.LoadSingleSubAsset(owner, mockBundlePath, mockSprName1);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 1);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));

            var sprite3 = manager.LoadSingleSubAsset(owner, mockBundlePath, mockSprName2);
            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 3);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 2);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName2));

            var sprite4 = manager.LoadSingleSubAsset(owner2, mockBundlePath, mockSprName2);
            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 4);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 2);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName2));

            foreach (var animSprite in animSprites)
            {
                Assert.IsTrue(animSprite.Ref != null);
            }
            // Giải phóng asset animation
            manager.ReleaseAnimationAssetRef(owner, animSprites, forceCleanUpIfNoRefCount: true);
            // Chỉ giải phóng ref của animation asset ref
            foreach (var animSprite in animSprites)
            {
                Assert.IsTrue(animSprite.Ref == null);
            }

            // Không giải phóng asset ref của single spr
            Assert.IsTrue(sprite.Ref != null);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 3);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 2);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName2));

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                manager.ReleaseSpriteAssetRef(owner2, sprite, forceCleanUpIfNoRefCount: true);
            });
            Assert.IsTrue(ex.Message == "Asset should belong to the owner!");
            manager.ReleaseSpriteAssetRef(owner, sprite, forceCleanUpIfNoRefCount: true);
            Assert.IsTrue(sprite.Ref == null);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 1);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(!bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName2));


            manager.ReleaseSpriteAssetRef(owner2, sprite4, forceCleanUpIfNoRefCount: false);
            Assert.IsTrue(sprite4.Ref != null);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 1);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(!bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName2));


            manager.ReleaseSpriteAssetRef(owner, sprite3, forceCleanUpIfNoRefCount: true);
            Assert.IsTrue(sprite4.Ref == null);
            Assert.IsTrue(sprite3.Ref == null);

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);
        }

        [Test]
        public void ReleaseAnimationAssetRef_ForceUnloadWithoutTimeout()
        {
            mockUnityEngineContract.MakeNewMockBundleRef = (fullPath, bundlePath) =>
            {
                Assert.AreEqual(fullPath, Path.Combine(mockUnityEngineContract.StreamingAssetPath, zombie_idle_bundleRef.RelativeBundlePath));
                if (zombie_idle_bundleRef.FullPath == fullPath)
                {
                    return zombie_idle_bundleRef;
                }
                return null;
            };
            var owner = new MockAssetOwner();
            var ownerHashset = manager.__GetCachedAssetOwner();
            var singleSprToBundle = manager.__GetSingleAssetToBundle();
            var bundleToSingleSpr = manager.__GetBundleToSingleAsset();
            var animToBundle = manager.__GetAnimationBundleMap();
            var loadedBundle = manager.__GetLoadedBundles();
            var bundleToAnim = manager.__GetBundleToAnimation();

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);

            var animSprites = manager.LoadAllSubAssets(owner, mockBundlePath);
            var sprite = manager.LoadSingleSubAsset(owner, mockBundlePath, mockSprName1);

            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 2);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 1);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));


            manager.ReleaseSpriteAssetRef(owner, sprite, forceCleanUpIfNoRefCount: false);
            Assert.IsTrue(sprite.Ref != null);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 1);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));

            manager.ReleaseAnimationAssetRef(owner, animSprites, forceCleanUpIfNoRefCount: false);
            Assert.IsTrue(ownerHashset[zombie_idle_bundleRef].Count == 0);
            Assert.IsTrue(ownerHashset.Count == 1);
            Assert.IsTrue(loadedBundle.Count == 1);

            Assert.IsTrue(animToBundle.Count == 1);
            Assert.IsTrue(bundleToAnim.Count == 1);

            Assert.IsTrue(singleSprToBundle.Count == 1);
            Assert.IsTrue(bundleToSingleSpr.Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].Count == 1);
            Assert.IsTrue(bundleToSingleSpr[zombie_idle_bundleRef].ContainsKey(mockSprName1));

            manager.UpdateCachedAssetBundle(true);

            Assert.IsTrue(ownerHashset.Count == 0);
            Assert.IsTrue(loadedBundle.Count == 0);

            Assert.IsTrue(animToBundle.Count == 0);
            Assert.IsTrue(bundleToAnim.Count == 0);

            Assert.IsTrue(singleSprToBundle.Count == 0);
            Assert.IsTrue(bundleToSingleSpr.Count == 0);
        }
    }
}
