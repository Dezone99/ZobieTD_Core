using System;
using System.Collections.Generic;
using System.Text;
using ZobieTDCore.Contracts;
using ZobieTDCore.Contracts.Items;
using ZobieTDCore.Services.Logger;

namespace ZobieTDCore
{
    public class EngineKeeper
    {
        public static void Init(IUnityEngineContract unityEngineContract)
        {
            ContractManager.Instance.SetUnityEngineContract(unityEngineContract);
            TDLogger.Init(unityEngineContract);
        }
    }
}
