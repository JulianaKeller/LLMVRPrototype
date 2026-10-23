using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "AssetCategoryRegistry", menuName = "LLM VR/Database/Category Registry")]
    public class AssetCategoryRegistry : Registry<AssetCategoryDefinition>
    {

        public IReadOnlyList<AssetCategoryDefinition> Categories =>
        entries.Cast<AssetCategoryDefinition>().ToList();

        public bool TryGetById(string categoryId, out AssetCategoryDefinition definition)
        {
            definition = null;
            if (base.TryGetById(categoryId, out RegistryEntryDefinition baseDefinition) && baseDefinition is AssetCategoryDefinition categoryDef)
            {
                definition = categoryDef;
                return true;
            }
            return false;
        }

        public bool RegisterCategory(AssetCategoryDefinition category)
        {
            return base.RegisterEntry(category);
        }

        public bool UnregisterCategory(AssetCategoryDefinition category)
        {
            return base.UnregisterEntry(category);
        }
    }
}
