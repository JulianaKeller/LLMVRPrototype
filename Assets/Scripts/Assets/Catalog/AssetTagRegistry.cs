using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "AssetTagRegistry", menuName = "LLM VR/Database/Tag Registry")]
    public class AssetTagRegistry : ScriptableObject
    {
        [SerializeField]
        private List<AssetTagDefinition> tags = new();

        private Dictionary<string, AssetTagDefinition> tagsById;

        public IReadOnlyList<AssetTagDefinition> Tags => tags;

        public void RebuildIndex()
        {
            tagsById = new Dictionary<string, AssetTagDefinition>(
                StringComparer.OrdinalIgnoreCase);

            foreach (AssetTagDefinition tag in tags)
            {
                if (tag == null || string.IsNullOrWhiteSpace(tag.TagId))
                    continue;

                string id = tag.TagId.Trim();

                if (!tagsById.TryAdd(id, tag))
                {
                    Debug.LogWarning($"Duplicate tag ID '{id}'.", this);
                }
            }
        }

        public bool TryGetById(
            string tagId,
            out AssetTagDefinition definition)
        {
            if (tagsById == null)
                RebuildIndex();

            return tagsById.TryGetValue(
                tagId?.Trim() ?? string.Empty,
                out definition);
        }

        public bool Contains(string tagId)
        {
            return TryGetById(tagId, out _);
        }

        public void AddTag(AssetTagDefinition tag)
        {
            tags.Add(tag);
            RebuildIndex();
        }
    }
}
