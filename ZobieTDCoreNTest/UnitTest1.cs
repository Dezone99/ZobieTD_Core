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

            var ar1 = new AssetRef<Data>(e1);
            var ar1_2 = new AssetRef<Data>(e1);
            ar1.Dispose();
            var t = ar1 == null;
            e3 = new Data() { Id = 3, Name = "3" };
        }

        private class Data
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }


    }
}