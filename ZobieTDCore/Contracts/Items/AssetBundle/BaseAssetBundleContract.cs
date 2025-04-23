using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    public abstract class BaseAssetBundleContract : IAssetBundleContract
    {
        private bool isUnloaded = false;
        private bool isSoftUnloaded = false;


        public abstract string BundleName { get; }

        public abstract bool Contain(object asset);

        public abstract T[] LoadAllSubAssets<T>() where T : class;

        public abstract T LoadSingleSubAsset<T>(string name) where T : class;

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

        protected abstract void UnloadInternal(bool unloadAllAsset);
    }
}
