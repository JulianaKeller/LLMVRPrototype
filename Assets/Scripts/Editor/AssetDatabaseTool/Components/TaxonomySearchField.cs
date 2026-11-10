using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    // searching and displaying suggestions
    public sealed class TaxonomySearchField<T> where T : class
    {
        private readonly List<T> definitions;
        private readonly Func<T, string> getDisplayName;
        private readonly Func<T, string> getId;
        private readonly Func<T, IEnumerable<string>> getSynonyms;
        private readonly Action<T> onSelected;
        private readonly Action<string> onSubmit;
        private readonly Func<T, bool> canSelect;

        private readonly int maxResults;

        private readonly VisualElement root;
        private readonly TextField searchField;
        private readonly VisualElement suggestionsContainer;

        public VisualElement Root => root;

        public TaxonomySearchField(
            string label,
            IEnumerable<T> definitions,
            Func<T, string> getDisplayName,
            Func<T, string> getId,
            Func<T, IEnumerable<string>> getSynonyms,
            Action<T> onSelected,
            Func<T, bool> canSelect = null,
            int maxResults = 10,
            Action<string> onSubmit = null)
        {
            this.definitions = definitions != null
                ? new List<T>(definitions)
                : new List<T>();

            this.getDisplayName = getDisplayName
                ?? throw new ArgumentNullException(nameof(getDisplayName));

            this.getId = getId;
            this.getSynonyms = getSynonyms;
            this.onSelected = onSelected;
            this.onSubmit = onSubmit;
            this.canSelect = canSelect;
            this.maxResults = Math.Max(1, maxResults);

            root = new VisualElement();

            var heading = new Label(label);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 4;
            root.Add(heading);

            searchField = new TextField();
            searchField.tooltip = $"Search {label.ToLowerInvariant()} by name or synonym.";
            root.Add(searchField);

            suggestionsContainer = new VisualElement();
            suggestionsContainer.style.marginTop = 2;
            root.Add(suggestionsContainer);

            searchField.RegisterValueChangedCallback(evt => RefreshSuggestions(evt.newValue));
            searchField.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void Refresh()
        {
            RefreshSuggestions(searchField.value);
        }

        public void Clear()
        {
            searchField.SetValueWithoutNotify(string.Empty);
            suggestionsContainer.Clear();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                return;

            evt.StopPropagation();
            onSubmit?.Invoke(searchField.value);
        }

        private void RefreshSuggestions(string query)
        {
            suggestionsContainer.Clear();

            if (string.IsNullOrWhiteSpace(query))
                return;

            var matches = TaxonomySearchUtility.Search(
                definitions,
                query,
                getDisplayName,
                getId,
                getSynonyms,
                maxResults);

            int displayedCount = 0;

            foreach (var result in matches)
            {
                T definition = result.Definition;

                if (canSelect != null && !canSelect(definition))
                    continue;

                var button = new Button(() =>
                {
                    onSelected?.Invoke(definition);
                    Clear();
                })
                {
                    text = $"{getDisplayName(definition)}  ({getId?.Invoke(definition)})"
                };

                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                suggestionsContainer.Add(button);
                displayedCount++;
            }

            if (displayedCount == 0)
            {
                var noResults = new Label("No matching definitions.");
                noResults.style.opacity = 0.6f;
                suggestionsContainer.Add(noResults);
            }
        }
    }
}