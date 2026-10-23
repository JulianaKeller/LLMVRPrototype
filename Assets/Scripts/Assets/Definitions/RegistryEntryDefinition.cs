using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    public abstract class RegistryEntryDefinition : ScriptableObject
    {
        [SerializeField] protected string id; // e.g. environment.skyboxes
        [SerializeField] protected string displayName;
        [SerializeField] protected string description;

        [SerializeField]
        protected string[] synonyms = System.Array.Empty<string>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;

        public IReadOnlyList<string> Synonyms => synonyms;
    }
}
