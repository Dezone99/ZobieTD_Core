using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    public abstract class BaseAssetBundleContract : IAssetBundleContract
    {
        private bool isUnloaded = false;
        private bool isSoftUnloaded = false;
        protected string bundlePath;
        public BaseAssetBundleContract(string bundlePath)
        {
            this.bundlePath = bundlePath;
        }

        public bool IsUnloaded()
        {
            return isUnloaded;
        }

        public void Unload(bool unloadAllAsset)
        {
            if (isUnloaded)
            {
                return;
            }
            isUnloaded = true;
            isSoftUnloaded = !unloadAllAsset;

            UnloadInternal(unloadAllAsset);
        }
        public abstract string BundleName { get; }
        public abstract bool Contain(object asset);
        public abstract void ReloadBundle();
        protected abstract void UnloadInternal(bool unloadAllAsset);
        public abstract object LoadSingleSubAsset(string name);
        public abstract object[] LoadAllSubAssets();
    }
}
