using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "AssetCatalog", menuName = "LLM VR/Database/Asset Catalog")]
    public class AssetCatalog : ScriptableObject
    {
        [SerializeField]
        private List<AssetDefinition> assets = new();

        private Dictionary<string, AssetDefinition> assetsById;

        public IReadOnlyList<AssetDefinition> Assets => assets;

        public void RebuildIndex()
        {
            assetsById = new Dictionary<string, AssetDefinition>();

            foreach (AssetDefinition asset in assets)
            {
                if (asset == null || string.IsNullOrWhiteSpace(asset.AssetId))
                    continue;

                if (assetsById.ContainsKey(asset.AssetId))
                {
                    Debug.LogWarning(
                        $"Duplicate asset ID '{asset.AssetId}' in catalog.",
                        this);

                    continue;
                }

                assetsById.Add(asset.AssetId, asset);
            }
        }

        public bool TryGetById(string assetId, out AssetDefinition definition)
        {
            if (assetsById == null)
                RebuildIndex();

            return assetsById.TryGetValue(assetId, out definition);
        }

        public List<AssetDefinition> GetEnabledAssets()
        {
            return assets.FindAll(asset => asset != null && asset.IsEnabled);
        }

        public void AddAsset(AssetDefinition asset)
        {
            assets.Add(asset);
            RebuildIndex();
        }
    }
}
