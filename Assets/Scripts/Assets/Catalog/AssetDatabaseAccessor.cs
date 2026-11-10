using UnityEngine;

namespace LLMVRPrototype.Assets
{
    public static class AssetDatabaseAccessor
    {
        private const string ConfigResourcePath = "Database/MainAssetDatabaseConfig";

        private static AssetDatabaseConfig config;

        public static AssetDatabaseConfig Config
        {
            get
            {
                if (config == null)
                    config = Resources.Load<AssetDatabaseConfig>(ConfigResourcePath);

                if (config == null)
                    throw new System.InvalidOperationException($"Could not load AssetDatabaseConfig from Resources/{ConfigResourcePath}.");

                return config;
            }
        }

        public static AssetCatalog AssetCatalog => Config.AssetCatalog;
        public static AssetCategoryRegistry CategoryRegistry => Config.CategoryRegistry;
        public static AssetTagRegistry TagRegistry => Config.TagRegistry;
        public static OperationCatalog OperationCatalog => Config.OperationCatalog;
    }
}