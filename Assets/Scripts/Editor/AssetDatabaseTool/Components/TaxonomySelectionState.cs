using System;
using System.Collections.Generic;
using System.Linq;

namespace LLMVRPrototype.EditorTools
{
    public sealed class TaxonomySelectionState<T> where T : class
    {
        private readonly Func<T, string> getId;
        private readonly HashSet<string> selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> SelectedIds => selectedIds;

        public event Action SelectionChanged;

        public TaxonomySelectionState(Func<T, string> getId)
        {
            this.getId = getId ?? throw new ArgumentNullException(nameof(getId));
        }

        public bool IsSelected(T definition)
        {
            if (definition == null)
                return false;

            string id = getId(definition);

            return !string.IsNullOrWhiteSpace(id) &&
                   selectedIds.Contains(id);
        }

        public void SetSingle(T definition)
        {
            selectedIds.Clear();

            if (definition != null)
            {
                string id = getId(definition);

                if (!string.IsNullOrWhiteSpace(id))
                    selectedIds.Add(id);
            }

            SelectionChanged?.Invoke();
        }

        public void Toggle(T definition)
        {
            if (definition == null)
                return;

            string id = getId(definition);

            if (string.IsNullOrWhiteSpace(id))
                return;

            if (!selectedIds.Add(id))
                selectedIds.Remove(id);

            SelectionChanged?.Invoke();
        }

        public void SetSelectedIds(IEnumerable<string> ids)
        {
            selectedIds.Clear();

            if (ids != null)
            {
                foreach (string id in ids)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        selectedIds.Add(id);
                }
            }

            SelectionChanged?.Invoke();
        }

        public void Clear()
        {
            selectedIds.Clear();
            SelectionChanged?.Invoke();
        }

        public List<string> ToList()
        {
            return selectedIds.ToList();
        }
    }
}