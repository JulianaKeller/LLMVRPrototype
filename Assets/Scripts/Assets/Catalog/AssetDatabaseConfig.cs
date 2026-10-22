using UnityEngine;

namespace LLMVRPrototype.Assets
{
    // entry point for accessing the database configuration
    [CreateAssetMenu(fileName = "AssetDatabaseConfig", menuName = "LLM VR/Database/Database Configuration")]
    public class AssetDatabaseConfig : ScriptableObject
    {
        [Header("Database References")]
        [SerializeField] private AssetCatalog assetCatalog;
        [SerializeField] private AssetCategoryRegistry categoryRegistry;
        [SerializeField] private AssetTagRegistry tagRegistry;

        [Header("Operation System")]
        [SerializeField] private OperationCatalog operationCatalog;

        public OperationCatalog OperationCatalog => operationCatalog;

        public AssetCatalog AssetCatalog => assetCatalog;
        public AssetCategoryRegistry CategoryRegistry => categoryRegistry;
        public AssetTagRegistry TagRegistry => tagRegistry;

        public bool HasRequiredReferences =>
            assetCatalog != null &&
            categoryRegistry != null &&
            tagRegistry != null &&
            operationCatalog != null;
    }
}
