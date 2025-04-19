using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    /// <summary>
    /// Đại diện cho một asset bundle đã load thành công.
    /// Cung cấp API để truy xuất các asset bên trong bundle đó.
    /// </summary>
    public interface IAssetBundleReference
    {
        /// <summary>
        /// Giải phóng bundle khỏi bộ nhớ.
        /// </summary>
        /// <param name="unloadAllAsset">True để giải phóng toàn bộ memory liên quan</param>
        void Unload(bool unloadAllAsset);

        /// <summary>
        /// Tên của bundle (dùng để map, tracking).
        /// </summary>
        string BundleName { get; }

        /// <summary>
        /// Load một asset duy nhất từ bundle (thường là sprite).
        /// </summary>
        IAssetReference LoadSingleSubAsset(string name);

        /// <summary>
        /// Load toàn bộ asset trong bundle, dùng cho animation, group.
        /// </summary>
        IAssetReference LoadAllSubAssets();

        /// <summary>
        /// Kiểm tra asset này có thuộc bundle hay không (dùng cho dev/debug).
        /// </summary>
        bool Contain(IAssetReference asset);
    }
}
