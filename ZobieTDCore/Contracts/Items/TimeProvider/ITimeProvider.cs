using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.TimeProvider
{
    public interface ITimeProvider
    {
        public float TimeNow { get; }
    }
}
