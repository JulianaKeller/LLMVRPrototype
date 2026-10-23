using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "AssetTagRegistry", menuName = "LLM VR/Database/Tag Registry")]
    public class AssetTagRegistry : Registry<AssetTagDefinition>
    {
        public IReadOnlyList<AssetTagDefinition> Tags => (IReadOnlyList<AssetTagDefinition>)entries;

        public bool TryGetById(string tagId, out AssetTagDefinition definition)
        {
            definition = null;
            if (base.TryGetById(tagId, out RegistryEntryDefinition baseDefinition) && baseDefinition is AssetTagDefinition tagDef)
            {
                definition = tagDef;
                return true;
            }
            return false;
        }

        public bool RegisterTag(AssetTagDefinition tag)
        {
            return base.RegisterEntry(tag);
        }

        public bool UnregisterTag(AssetTagDefinition tag)
        {
            return base.UnregisterEntry(tag);
        }
    }
}
