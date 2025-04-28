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
        public void TestDisposeRefFromData()
        {
            var @ref = new Ref() { Id = 1 };
            var data = new Data() { Id = 1, Name = "1", Ref = @ref };
            var data2 = new Data() { Id = 1, Name = "1", Ref = @ref };
            @ref = null;
            data.Ref = null;
            Assert.IsTrue(data.Ref == null);
        }

        private class Ref
        {
            public int Id { get; set; }
        }
        private class Data
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Ref? @Ref { get; set; }
        }


    }
}