using System;
using System.Collections.Generic;
using System.IO;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCore.Services.AssetBundle
{
    /// <summary>
    /// Quản lý quá trình load, release và mapping các asset từ asset bundle.
    /// Mọi asset đều được đóng gói dưới dạng IAssetReference (single hoặc group).
    /// </summary>
    public class AssetBundleManager
    {
        public static AssetBundleManager Instance { get; } = new AssetBundleManager();

        private Dictionary<string, IAssetBundleReference> loadedBundles = new Dictionary<string, IAssetBundleReference>();
        private Dictionary<IAssetReference, string> assetToBundle = new Dictionary<IAssetReference, string>();
        private Dictionary<(string bundleName, string spriteName), IAssetReference> cachedSingleSpriteAssets = new Dictionary<(string bundleName, string spriteName), IAssetReference>();
        private AssetBundleUsageManager assetBundleUsageManager = new AssetBundleUsageManager();

        /// <summary>
        /// Load một asset duy nhất từ bundle, thường dùng cho sprite đơn.
        /// Trả về IAssetReference đại diện cho 1 sprite.
        /// </summary>
        public IAssetReference LoadSingleSubSpriteAsset(string bundleName, string spriteName)
        {
            var key = (bundleName, spriteName);

            if (cachedSingleSpriteAssets.TryGetValue(key, out var cachedRef))
            {
                return cachedRef;
            }

            var bundle = LoadAssetBundle(bundleName);
            var asset = bundle.LoadSingleSubSpriteAsset(spriteName);

            assetToBundle[asset] = bundleName;
            assetBundleUsageManager.RegisterAssetReference(asset, bundle);
            cachedSingleSpriteAssets[key] = asset;

            return asset;
        }

        /// <summary>
        /// Load toàn bộ asset từ 1 bundle. Thường dùng để lấy animation (nhiều sprite).
        /// </summary>
        public IAssetReference LoadAllSubSpriteAsset(string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);
            var assetRef = bundle.LoadAllSubSpriteAssets();
            assetToBundle[assetRef] = bundleName;
            assetBundleUsageManager.RegisterAssetReference(assetRef, bundle);
            return assetRef;
        }

        /// <summary>
        /// Giải phóng asset reference khỏi hệ thống, giảm refCount và có thể trigger unload.
        /// </summary>
        public void ReleaseAssetRef(IAssetReference assetRef)
        {
            if (assetToBundle.TryGetValue(assetRef, out var bundleName))
            {
                assetBundleUsageManager.UnregisterAssetReference(assetRef);
                assetToBundle.Remove(assetRef);

                // Xoá khỏi cache nếu có
                var keysToRemove = new List<(string, string)>();
                foreach (var kvp in cachedSingleSpriteAssets)
                {
                    if (kvp.Value == assetRef)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    cachedSingleSpriteAssets.Remove(key);
                }
            }
        }

        /// <summary>
        /// Lấy tên bundle mà assetRef thuộc về (nếu đã load).
        /// </summary>
        public string? GetBundleNameOfSprite(IAssetReference assetRef)
        {
            return assetToBundle.TryGetValue(assetRef, out var bundleName) ? bundleName : null;
        }

        /// <summary>
        /// Duyệt toàn bộ các bundle đã load và tự động giải phóng bundle không còn được sử dụng.
        /// </summary>
        public void UpdateCachedAssetBundle()
        {
            var unityEngineContract = ContractManager.Instance.UnityEngineContract
                ?? throw new InvalidOperationException("Core engine was not initalized");

            foreach (var bundleName in assetBundleUsageManager.GetNeedToUnloadBundle())
            {
                ForceUnloadBundle(bundleName);
            }
        }

        /// <summary>
        /// Trả về danh sách các bundle đang được load hiện tại.
        /// </summary>
        public List<string> GetLoadedBundles() => new List<string>(loadedBundles.Keys);

        /// <summary>
        /// Load bundle từ StreamingAssets nếu chưa có trong bộ nhớ.
        /// </summary>
        private IAssetBundleReference LoadAssetBundle(string bundleName)
        {
            var unityEngineContract = ContractManager.Instance.UnityEngineContract
                ?? throw new InvalidOperationException("Core engine was not initalized");

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                var path = Path.Combine(unityEngineContract.StreamingAssetPath, bundleName);
                bundle = unityEngineContract.LoadAssetBundleFromFile(path);
                loadedBundles[bundleName] = bundle;
            }

            return bundle;
        }

        /// <summary>
        /// Giải phóng toàn bộ bundle khỏi bộ nhớ (force).
        /// Gọi khi chắc chắn không còn ref nào.
        /// </summary>
        private void ForceUnloadBundle(string bundleName)
        {
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                bundle.Unload(unloadAllAsset: true);
                loadedBundles.Remove(bundleName);
            }
        }

        /// <summary>
        /// Tạo đối tượng mới cho test.
        /// </summary>
        internal static AssetBundleManager __MakeBundleManagerForTest() => new AssetBundleManager();

        /// <summary>
        /// Trả về đối tượng quản lý ref cho test.
        /// </summary>
        internal AssetBundleUsageManager __GetBundleUsageManagerForTest() => assetBundleUsageManager;
    }
}