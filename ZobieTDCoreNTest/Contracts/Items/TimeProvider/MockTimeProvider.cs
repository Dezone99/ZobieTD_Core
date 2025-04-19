using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZobieTDCore.Contracts.Items.TimeProvider;

namespace ZobieTDCoreNTest.Contracts.Items.TimeProvider
{
    internal class MockTimeProvider : ITimeProvider
    {
        private float time;
        public float TimeNow => time;
        public void SetTime(float t) => time = t;
        public void Advance(float delta) => time += delta;
    }

}
