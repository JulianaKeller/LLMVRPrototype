using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "AssetTag", menuName = "LLM VR/Database/Tag Definition")]
    public class AssetTagDefinition : RegistryEntryDefinition
    {
        
        [Header("Hierarchy")]
        [SerializeField] private AssetTagDefinition parentTag;

        public string TagId => id;
        public AssetTagDefinition ParentTag => parentTag;
    }
}
