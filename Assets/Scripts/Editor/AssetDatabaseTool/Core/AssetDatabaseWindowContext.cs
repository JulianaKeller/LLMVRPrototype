using LLMVRPrototype.Assets;
using UnityEngine;

namespace LLMVRPrototype.EditorTools
{
    public class AssetDatabaseWindowContext
    {
        public AssetDatabaseConfig DatabaseConfig { get; }

        public AssetDatabaseWindowContext(AssetDatabaseConfig databaseConfig)
        {
            DatabaseConfig = databaseConfig;
        }

        public bool HasValidConfiguration => DatabaseConfig != null && DatabaseConfig.HasRequiredReferences;
    }
}
