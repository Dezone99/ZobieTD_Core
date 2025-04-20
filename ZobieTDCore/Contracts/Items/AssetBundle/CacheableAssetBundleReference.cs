using System;
using System.Collections.Generic;
using System.Text;

namespace ZobieTDCore.Contracts.Items.AssetBundle
{
    public abstract class CacheableAssetBundleReference : IAssetBundleReference
    {
        private readonly Dictionary<string, IAssetReference> singleSpriteCache = new Dictionary<string, IAssetReference>();
        private IAssetReference? assetAllSpriteCache = null;

        public abstract string BundleName { get; }
        public abstract bool Contain(IAssetReference asset);

        public IAssetReference LoadAllSubSpriteAssets()
        {
            if (assetAllSpriteCache == null)
            {
                assetAllSpriteCache = LoadAllSubAssetInternal();
            }
            return assetAllSpriteCache;
        }
        public IAssetReference LoadSingleSubSpriteAsset(string name)
        {
            if (singleSpriteCache.ContainsKey(name))
            {
                return singleSpriteCache[name];
            }
            var sprite = LoadSpriteInternal(name);
            if (sprite == null)
            {
                throw new InvalidOperationException("Should never happen, asset name not in bundle");
            }
            singleSpriteCache[name] = sprite;
            return sprite;
        }

        public void Unload(bool unloadAllAsset)
        {
            if (unloadAllAsset)
            {
                singleSpriteCache.Clear();
                assetAllSpriteCache = null;
            }
            UnloadInternal(unloadAllAsset);
        }

        protected abstract IAssetReference LoadAllSubAssetInternal();
        protected abstract IAssetReference LoadSpriteInternal(string name);
        protected abstract void UnloadInternal(bool unloadAllAsset);

    }
}
