using System;
using System.Collections.Generic;
using System.Text;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;

namespace ZobieTDCore
{
    public class EngineKeeper
    {
        public static void Init(IUnityEngineContract unityEngineContract)
        {
            ContractManager.Instance.SetUnityEngineContract(unityEngineContract);
        }
    }
}
