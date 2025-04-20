using System;
using System.Collections.Generic;
using System.Text;
using ZobieTDCore.Contracts.Items;

namespace ZobieTDCore.Contracts
{
    internal class ContractManager
    {
        public static ContractManager Instance { get; } = new ContractManager();

        public IUnityEngineContract? UnityEngineContract { get; private set; }

        private ContractManager() { }

        public void SetUnityEngineContract(IUnityEngineContract unityEngineContract)
        {
            UnityEngineContract = unityEngineContract;
        }
    }
}
