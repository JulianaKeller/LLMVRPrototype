using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEngine;

namespace LLMVRPrototype
{
    public abstract class Registry<T> : ScriptableObject where T : RegistryEntryDefinition
    {
        [SerializeField] protected List<T> entries = new();
        public IReadOnlyList<T> Entries => entries;

        protected Dictionary<string, RegistryEntryDefinition> entriesById;

        public void RebuildIndex()
        {
            entriesById = new Dictionary<string, RegistryEntryDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (RegistryEntryDefinition entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
                    continue;

                string id = entry.Id.Trim();

                if (!entriesById.TryAdd(id, entry))
                {
                    Debug.LogWarning($"Duplicate entry ID '{id}'.", this);
                }
            }
        }

        protected bool TryGetById(
            string getId,
            out RegistryEntryDefinition definition)
        {
            if (entriesById == null)
                RebuildIndex();

            if (entriesById.TryGetValue(
                getId?.Trim() ?? string.Empty,
                out RegistryEntryDefinition entryDef))
            {
                definition = entryDef;
                return definition != null;
            }

            definition = null;
            return false;
        }

        public bool Contains(string tagId)
        {
            return TryGetById(tagId, out _);
        }

        private void AddEntry(T entryToAdd)
        {
            entries.Add(entryToAdd);
            RebuildIndex();
        }

        protected bool RegisterEntry(T entryToRegister)
        {
            if (entryToRegister == null || string.IsNullOrWhiteSpace(entryToRegister.Id))
                return false;

            if (entries == null)
                entries = new List<T>();

            foreach (RegistryEntryDefinition existing in entries)
            {
                if (existing == entryToRegister)
                    return true;

                if (existing != null &&
                    string.Equals(
                        existing.Id,
                        entryToRegister.Id,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            AddEntry(entryToRegister);
            return true;
        }

        protected bool UnregisterEntry(T entryToUnregister)
        {
            if (entryToUnregister == null || entries == null)
                return false;

            bool removed = entries.Remove(entryToUnregister);

            if (removed)
                RebuildIndex();

            return removed;
        }
    }
}
