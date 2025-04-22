using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Services.AssetBundle
{
    public class AssetRef
    {
        public object? Ref { get; }
        public AssetRef(object @ref)
        {
            Ref = @ref;
        }

        public override bool Equals(object? obj)
        {
            if (obj is AssetRef cast)
            {
                return Ref?.Equals(cast.Ref) ?? base.Equals(cast);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Ref?.GetHashCode() ?? base.GetHashCode();
        }
    }
}
