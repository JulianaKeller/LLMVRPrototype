using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "AssetCategory", menuName = "LLM VR/Database/Category Definition")]
    public class AssetCategoryDefinition : ScriptableObject
    {
        [SerializeField] private string categoryId; // e.g. environment.skyboxes
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private AssetCategoryDefinition parentCategory;

        public string CategoryId => categoryId;
        public string DisplayName => displayName;
        public string Description => description;
        public AssetCategoryDefinition ParentCategory => parentCategory;
    }
}
