using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.TimeProvider
{
    /// <summary>
    /// Giao diện cung cấp thời gian thực tế.
    /// Dùng để thay thế Time.realtimeSinceStartup và dễ mock trong test.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Thời gian hiện tại tính bằng giây (thường dùng real-time).
        /// </summary>
        float TimeNow { get; }
    }
}
