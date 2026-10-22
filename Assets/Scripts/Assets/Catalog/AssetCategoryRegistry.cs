using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "AssetCategoryRegistry", menuName = "LLM VR/Database/Category Registry")]
    public class AssetCategoryRegistry : ScriptableObject
    {
        [SerializeField]
        private List<AssetCategoryDefinition> categories = new();

        private Dictionary<string, AssetCategoryDefinition> categoriesById;

        public IReadOnlyList<AssetCategoryDefinition> Categories => categories;

        public void RebuildIndex()
        {
            categoriesById = new Dictionary<string, AssetCategoryDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (AssetCategoryDefinition category in categories)
            {
                if (category == null || string.IsNullOrWhiteSpace(category.CategoryId))
                {
                    continue;
                }

                string id = category.CategoryId.Trim();

                if (!categoriesById.TryAdd(id, category))
                {
                    Debug.LogWarning($"Duplicate category ID '{id}'.", this);
                }
            }
        }

        public bool TryGetById(string categoryId, out AssetCategoryDefinition definition)
        {
            if (categoriesById == null)
                RebuildIndex();

            return categoriesById.TryGetValue(categoryId?.Trim() ?? string.Empty, out definition);
        }

        public bool Contains(string categoryId)
        {
            return TryGetById(categoryId, out _);
        }

        public void AddCategory(AssetCategoryDefinition category)
        {
            categories.Add(category);
            RebuildIndex();
        }
    }
}
