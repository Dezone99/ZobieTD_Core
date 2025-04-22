#define UNITY_EDITOR

using Newtonsoft.Json.Linq;
using ZobieTDCore.Services.AssetBundle;
namespace ZobieTDCoreNTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var e1 = new Data() { Id = 1, Name = "1" };
            var e2 = new Data() { Id = 2, Name = "2" };
            var e3 = new Data() { Id = 3, Name = "3" };

            var ar1 = new AssetRef(e1);
            var ar1_2 = new AssetRef(e1);

            var dic = new Dictionary<AssetRef, int>();
            dic[ar1] = 1;
            var isContain = dic.ContainsKey(ar1);
            var isContain2 = dic.ContainsKey(ar1_2);
            var isContain3 = dic.ContainsKey(ar1_2);
        }

        private class Data
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

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
}