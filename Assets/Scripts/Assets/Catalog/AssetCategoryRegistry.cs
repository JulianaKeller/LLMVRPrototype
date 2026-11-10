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
            entries?.OfType<AssetCategoryDefinition>().ToList()
            ?? new List<AssetCategoryDefinition>();

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

        public bool TryGetCategoryPath(string categoryId, out IReadOnlyList<string> categoryPath)
        {
            categoryPath = null;

            if (!TryGetById(categoryId, out AssetCategoryDefinition category))
                return false;

            List<string> path = new();
            HashSet<AssetCategoryDefinition> visited = new();

            AssetCategoryDefinition current = category;

            while (current != null)
            {
                // Prevent infinite loops caused by circular parent references
                if (!visited.Add(current))
                    return false;

                path.Add(current.DisplayName);
                current = current.ParentCategory;
            }

            path.Reverse();
            categoryPath = path;
            return true;
        }
    }
}
