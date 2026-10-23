using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "AssetTag", menuName = "LLM VR/Database/Tag Definition")]
    public class AssetTagDefinition : ScriptableObject
    {
        [SerializeField] private string tagId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;

        [Header("Hierarchy")]
        [SerializeField] private AssetTagDefinition parentTag;

        [SerializeField]
        private string[] synonyms = System.Array.Empty<string>();

        public string TagId => tagId;
        public string DisplayName => displayName;
        public AssetTagDefinition ParentTag => parentTag;
        public string Description => description;
        public IReadOnlyList<string> Synonyms => synonyms;
    }
}
