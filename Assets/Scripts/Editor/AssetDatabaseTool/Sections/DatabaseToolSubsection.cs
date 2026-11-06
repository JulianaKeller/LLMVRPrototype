using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace LLMVRPrototype
{
    public abstract class DatabaseToolSubsection<TDefinition> : DatabaseToolSection
        where TDefinition : class
    {
        private TextField nameField;
        private VisualElement suggestionsContainer;
        private Label feedbackLabel;

        protected DatabaseToolSubsection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
        }

        protected abstract string DefinitionTypeName { get; }
        protected abstract string GetDisplayName(TDefinition definition);
        protected abstract string GetIdentifier(TDefinition definition);
        protected abstract IEnumerable<TDefinition> GetDefinitions();

        protected abstract void OpenForCreate(string displayName, Action<TDefinition> onClosed);

        protected abstract void OpenForEdit(TDefinition definition, Action<TDefinition> onClosed);

        // allows each section to provide additional searchable values, such as synonym
        protected virtual IEnumerable<string> GetSearchTerms(TDefinition definition)
        {
            yield return GetDisplayName(definition);
            yield return GetIdentifier(definition);
        }

        protected virtual bool IsValidDefinition(TDefinition definition)
        {
            return definition != null &&
                   !string.IsNullOrWhiteSpace(GetDisplayName(definition)) &&
                   !string.IsNullOrWhiteSpace(GetIdentifier(definition));
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            AddHeading($"Create or Find a {DefinitionTypeName}");

            nameField = new TextField($"{DefinitionTypeName} Display Name")
            {
                value = string.Empty
            };

            nameField.tooltip = $"Enter a {DefinitionTypeName.ToLowerInvariant()} name, then press Enter to create it.";

            nameField.style.marginBottom = 4;
            root.Add(nameField);

            suggestionsContainer = new VisualElement();
            suggestionsContainer.style.marginBottom = 8;
            root.Add(suggestionsContainer);

            feedbackLabel = new Label();
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.marginBottom = 8;
            root.Add(feedbackLabel);

            nameField.RegisterValueChangedCallback(evt => RefreshSuggestions(evt.newValue));

            nameField.RegisterCallback<KeyDownEvent>(OnInputKeyDown);

            AddHeading($"Registered {DefinitionTypeName}s");
            BuildRegisteredList();

            nameField.Focus();
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            evt.StopPropagation();

            string displayName = nameField.value.Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                feedbackLabel.text =
                    $"Enter a {DefinitionTypeName.ToLowerInvariant()} display name first.";
                return;
            }

            TDefinition exactMatch = FindExactMatch(displayName);

            if (exactMatch != null)
            {
                OpenForEdit(exactMatch, _ => RefreshSection());
                return;
            }

            OpenForCreate(displayName, _ => RefreshSection());
        }

        private TDefinition FindExactMatch(string query)
        {
            return GetDefinitions().FirstOrDefault(definition =>
                IsValidDefinition(definition) &&
                (string.Equals(
                     GetDisplayName(definition),
                     query,
                     StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                     GetIdentifier(definition),
                     query,
                     StringComparison.OrdinalIgnoreCase)));
        }

        private void RefreshSuggestions(string query)
        {
            suggestionsContainer.Clear();

            if (string.IsNullOrWhiteSpace(query))
                return;

            List<TDefinition> matches = FindSimilarDefinitions(query.Trim());

            if (matches.Count == 0)
                return;

            var label = new Label($"Existing or similar {DefinitionTypeName.ToLowerInvariant()}s");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 4;
            suggestionsContainer.Add(label);

            foreach (TDefinition definition in matches)
            {
                TDefinition capturedDefinition = definition;

                var button = new Button(() =>
                {
                    OpenForEdit(capturedDefinition, _ => RefreshSection());
                })
                {
                    text = $"{GetDisplayName(definition)}  ({GetIdentifier(definition)})"
                };

                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.marginTop = 2;
                button.style.marginBottom = 2;

                suggestionsContainer.Add(button);
            }
        }

        private List<TDefinition> FindSimilarDefinitions(string query)
        {
            var results = new List<TDefinition>();

            string normalizedQuery = query.Trim().ToLowerInvariant();

            foreach (TDefinition definition in GetDefinitions())
            {
                if (!IsValidDefinition(definition))
                    continue;

                bool partialMatch = false;
                bool closeSpelling = false;

                foreach (string searchTerm in GetSearchTerms(definition))
                {
                    if (string.IsNullOrWhiteSpace(searchTerm))
                        continue;

                    string normalizedTerm = searchTerm.Trim().ToLowerInvariant();

                    if (normalizedTerm.Contains(normalizedQuery))
                        partialMatch = true;

                    if (EditDistance(normalizedTerm, normalizedQuery) <=
                        Math.Max(2, normalizedQuery.Length / 3))
                    {
                        closeSpelling = true;
                    }
                }

                if (partialMatch || closeSpelling)
                    results.Add(definition);
            }

            results.Sort((a, b) =>
                string.Compare(
                    GetDisplayName(a),
                    GetDisplayName(b),
                    StringComparison.OrdinalIgnoreCase));

            return results;
        }

        private void BuildRegisteredList()
        {
            var list = new ScrollView();
            list.style.maxHeight = 280;
            list.style.marginTop = 4;

            List<TDefinition> definitions = GetDefinitions().Where(IsValidDefinition)
                .OrderBy(
                    GetDisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (definitions.Count == 0)
            {
                list.Add(new Label(
                    $"No {DefinitionTypeName.ToLowerInvariant()}s have been registered yet."));
            }
            else
            {
                foreach (TDefinition definition in definitions)
                {
                    TDefinition capturedDefinition = definition;

                    var button = new Button(() =>
                    {
                        OpenForEdit(capturedDefinition, _ => RefreshSection());
                    })
                    {
                        text = $"{GetDisplayName(definition)}  ({GetIdentifier(definition)})"
                    };

                    button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    list.Add(button);
                }
            }

            root.Add(list);
        }

        private void RefreshSection()
        {
            Build();
        }

        // TODO optimize or replace with a more efficient fuzzy-search implementation
        private static int EditDistance(string a, string b)
        {
            int[,] distances = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                distances[i, 0] = i;

            for (int j = 0; j <= b.Length; j++)
                distances[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;

                    distances[i, j] = Math.Min(
                        Math.Min(
                            distances[i - 1, j] + 1,
                            distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }

            return distances[a.Length, b.Length];
        }
    }
}
