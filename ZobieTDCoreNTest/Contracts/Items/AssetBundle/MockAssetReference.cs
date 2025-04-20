using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ZobieTDCore.Contracts.Items.AssetBundle;

namespace ZobieTDCoreNTest.Contracts.Items.AssetBundle
{
    internal class MockAssetReference : IAssetReference
    {
        public object? Ref { get; }
        public string Name { get; }
        public MockAssetReference(string name) => Name = name;
        public MockAssetReference(string name, object obRef)
        {
            Name = name;
            Ref = obRef;
        }

        public override bool Equals(object? obj) => obj is MockAssetReference other && Name == other.Name;
        public override int GetHashCode() => Name.GetHashCode();
    }
}
