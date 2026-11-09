using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "AssetCategory", menuName = "LLM VR/Database/Category Definition")]
    public class AssetCategoryDefinition : RegistryEntryDefinition
    {
        [SerializeField] private AssetCategoryDefinition parentCategory;

        public string CategoryId => id;

        public AssetCategoryDefinition ParentCategory => parentCategory;
    }
}
