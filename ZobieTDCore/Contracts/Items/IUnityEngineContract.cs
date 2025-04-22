using System;
using System.Collections.Generic;
using System.Text;
using ZobieTDCore.Contracts.Items.AssetBundle;
using ZobieTDCore.Contracts.Items.TimeProvider;

namespace ZobieTDCore.Contracts.Items
{

    /// <summary>
    /// Hợp đồng giao tiếp với Unity Engine.
    /// Dùng để tách biệt hoàn toàn logic xử lý core khỏi UnityEngine cụ thể.
    /// </summary>
    public interface IUnityEngineContract
    {
        /// <summary>
        /// Đường dẫn đến thư mục StreamingAssets.
        /// </summary>
        string StreamingAssetPath { get; }

        /// <summary>
        /// Tải một asset bundle từ file path chỉ định.
        /// </summary>
        /// <param name="filePath">Đường dẫn tuyệt đối đến file bundle</param>
        /// <returns>IAssetBundleContract tương ứng</returns>
        IAssetBundleContract LoadAssetBundleFromFile(string filePath);

        /// <summary>
        /// Có đang chạy ở chế độ Development (Editor hoặc build có flag).
        /// </summary>
        bool IsDevelopmentBuild { get; }

        /// <summary>
        /// Đối tượng cung cấp thời gian hiện tại, phục vụ việc timeout, tracking.
        /// </summary>
        ITimeProviderContract TimeProvider { get; }

        string GetUnityObjectName(object obj);
    }
}
