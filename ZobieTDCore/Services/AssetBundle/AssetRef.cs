using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;

namespace ZobieTDCore.Services.AssetBundle
{
    public class AssetRef<T> : IDisposable where T : class
    {
        public T? Ref { get; private set; }
        public AssetRef(T @ref)
        {
            Ref = @ref;
        }

        public override bool Equals(object? obj)
        {
            if (obj is AssetRef<T> cast)
            {
                return Ref?.Equals(cast.Ref) ?? base.Equals(cast);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Ref?.GetHashCode() ?? base.GetHashCode();
        }

        public void Dispose()
        {
            Ref = null;
        }
    }
}
