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

        /// <summary>
        /// Load một asset duy nhất từ bundle, thường dùng cho sprite đơn.
        /// Trả về IAssetReference đại diện cho 1 sprite.
        /// </summary>
        /// <param name="bundleName">Tên bundle đã build</param>
        /// <param name="spriteName">Tên asset cụ thể</param>
        /// <returns>IAssetReference chứa 1 sprite</returns>
        public IAssetReference LoadSingleAsset(string bundleName, string spriteName)
        {
            var bundle = LoadAssetBundle(bundleName);
            var asset = bundle.LoadSingleAsset(spriteName);
            assetToBundle[asset] = bundleName;
            AssetBundleUsageManager.Instance.RegisterAssetReference(asset, bundle);
            return asset;
        }

        /// <summary>
        /// Load toàn bộ asset từ 1 bundle. Thường dùng để lấy animation (nhiều sprite).
        /// Trả về IAssetReference có thể là danh sách sprite.
        /// </summary>
        /// <param name="bundleName">Tên bundle đã build</param>
        /// <returns>IAssetReference chứa toàn bộ asset trong bundle</returns>
        public IAssetReference LoadAllAsset(string bundleName)
        {
            var bundle = LoadAssetBundle(bundleName);
            var assetRef = bundle.LoadAllAssets();
            assetToBundle[assetRef] = bundleName;
            AssetBundleUsageManager.Instance.RegisterAssetReference(assetRef, bundle);
            return assetRef;
        }

        /// <summary>
        /// Giải phóng asset reference khỏi hệ thống, giảm refCount và có thể trigger unload.
        /// </summary>
        /// <param name="assetRef">Asset đã được load trước đó</param>
        public void ReleaseAssetRef(IAssetReference assetRef)
        {
            if (assetToBundle.TryGetValue(assetRef, out var bundleName))
            {
                AssetBundleUsageManager.Instance.UnregisterAssetReference(assetRef);
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

            foreach (var bundleName in AssetBundleUsageManager.Instance.GetNeedToUnloadBundle())
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
    }
}
