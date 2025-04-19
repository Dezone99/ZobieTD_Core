using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    /// <summary>
    /// Đại diện cho một asset đã được load từ bundle.
    /// Có thể là 1 sprite hoặc nhóm sprite (animation).
    /// </summary>
    public interface IAssetReference
    {
        /// <summary>
        /// Object thực tế bên Unity hoặc backend (sprite, prefab, ...).
        /// </summary>
        object? Ref { get; }

        /// <summary>
        /// Tên logic hoặc tên file của asset.
        /// </summary>
        string Name { get; }
    }
}
