using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Services.AssetBundle;

namespace ZobieTDCoreNTest.UnityItem
{
    internal class MockUnityAsset
    {
        public object? realAsset { get; private set; }
        public string name { get; }
        public MockUnityAsset(string name)
        {
            this.name = name;
            realAsset = new object();
        }

        public MockUnityAsset(string name, object realAsset)
        {
            this.name = name;
            this.realAsset = realAsset;
        }

        public override bool Equals(object? obj)
        {
            if (obj is MockUnityAsset cast)
            {
                return realAsset?.Equals(cast.realAsset) ?? base.Equals(cast);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return realAsset?.GetHashCode() ?? base.GetHashCode();
        }

        public void Dispose()
        {
            realAsset = null;
        }

        public static bool operator ==(MockUnityAsset? a, MockUnityAsset? b)
        {
            if (ReferenceEquals(a, null))
                return ReferenceEquals(b, null);

            if (ReferenceEquals(b, null))
                return a.realAsset == null;

            if (a.realAsset == null || b.realAsset == null)
                return Object.Equals(a.realAsset, b.realAsset);

            return Object.Equals(a, b);
        }

        public static bool operator !=(MockUnityAsset? a, MockUnityAsset? b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            if (realAsset == null)
                return "null";
            else
                return base.ToString();
        }
    }
}
