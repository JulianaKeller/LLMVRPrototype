using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public class TagsSection : DatabaseToolSection
    {
        private TextField tagNameField;
        private VisualElement suggestionsContainer;
        private Label feedbackLabel;

        public TagsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
            
        }

        protected override string GetSectionTitle()
        {
            return "Tags";
        }

        protected override string GetSectionDescription()
        {
            return "Create, find, and edit asset tags.";
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            AddHeading("Create or Find a Tag");

            tagNameField = new TextField("Tag Display Name")
            {
                value = string.Empty
            };
            tagNameField.tooltip = "Enter a tag name, then press Enter to create it.";
            tagNameField.style.marginBottom = 4;
            root.Add(tagNameField);

            suggestionsContainer = new VisualElement();
            suggestionsContainer.style.marginBottom = 8;
            root.Add(suggestionsContainer);

            feedbackLabel = new Label();
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.marginBottom = 8;
            root.Add(feedbackLabel);

            tagNameField.RegisterValueChangedCallback(evt => RefreshSuggestions(evt.newValue));

            tagNameField.RegisterCallback<KeyDownEvent>(OnInputKeyDown);

            AddHeading("Registered Tags");
            BuildRegisteredTagList();

            tagNameField.Focus();
        }

        private AssetTagRegistry GetRegistry()
        {
            return context.DatabaseConfig.TagRegistry;
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            evt.StopPropagation();

            string displayName = tagNameField.value.Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                feedbackLabel.text = "Enter a tag display name first.";
                return;
            }

            AssetTagDefinition exactMatch = FindExactMatch(displayName);

            if (exactMatch != null)
            {
                TagEditorWindow.OpenForEdit(
                    exactMatch,
                    GetRegistry(),
                    _ => RefreshSection());

                return;
            }

            TagEditorWindow.OpenForCreate(
                GetRegistry(),
                displayName,
                _ => RefreshSection());
        }

        private void RefreshSuggestions(string query)
        {
            suggestionsContainer.Clear();

            if (string.IsNullOrWhiteSpace(query))
                return;

            List<AssetTagDefinition> matches = FindSimilarTags(query.Trim());

            if (matches.Count == 0)
                return;

            var label = new Label("Existing or similar tags");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 4;
            suggestionsContainer.Add(label);

            foreach (AssetTagDefinition match in matches)
            {
                AssetTagDefinition capturedMatch = match;

                var button = new Button(() =>
                {
                    TagEditorWindow.OpenForEdit(
                        capturedMatch,
                        GetRegistry(),
                        _ => RefreshSection());
                })
                {
                    text = $"{match.DisplayName}  ({match.TagId})"
                };

                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.marginTop = 2;
                button.style.marginBottom = 2;
                suggestionsContainer.Add(button);
            }
        }

        private AssetTagDefinition FindExactMatch(string query)
        {
            foreach (AssetTagDefinition tag in GetRegistry().Tags)
            {
                if (tag != null &&
                    (string.Equals(
                         tag.DisplayName,
                         query,
                         StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                         tag.TagId,
                         query,
                         StringComparison.OrdinalIgnoreCase)))
                {
                    return tag;
                }
            }

            return null;
        }

        private List<AssetTagDefinition> FindSimilarTags(string query)
        {
            var results = new List<AssetTagDefinition>();
            string normalizedQuery = query.Trim();

            foreach (AssetTagDefinition tag in GetRegistry().Tags)
            {
                if (tag == null || !tag.IsValidForSearch())
                    continue;

                string name = tag.DisplayName.Trim();

                bool synonymMatch = false;

                if (tag.Synonyms != null)
                {
                    foreach (string synonym in tag.Synonyms)
                    {
                        if (!string.IsNullOrWhiteSpace(synonym) &&
                            synonym.IndexOf(
                                normalizedQuery,
                                StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            synonymMatch = true;
                            break;
                        }
                    }
                }

                bool partialMatch =
                    name.IndexOf(
                        normalizedQuery,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    tag.TagId.IndexOf(
                        normalizedQuery,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    synonymMatch;

                bool closeSpelling =
                    EditDistance(
                        name.ToLowerInvariant(),
                        normalizedQuery.ToLowerInvariant())
                    <= Math.Max(2, normalizedQuery.Length / 3);

                if (partialMatch || closeSpelling || synonymMatch)
                    results.Add(tag);
            }

            results.Sort((a, b) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase));

            return results;
        }

        private void BuildRegisteredTagList()
        {
            var list = new ScrollView();
            list.style.maxHeight = 280;
            list.style.marginTop = 4;

            List<AssetTagDefinition> tags = new List<AssetTagDefinition>(GetRegistry().Tags);

            tags.RemoveAll(tag => tag == null);
            tags.Sort((a, b) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase));

            if (tags.Count == 0)
            {
                list.Add(new Label("No tags have been registered yet."));
            }
            else
            {
                foreach (AssetTagDefinition tag in tags)
                {
                    AssetTagDefinition capturedTag = tag;

                    var button = new Button(() =>
                    {
                        TagEditorWindow.OpenForEdit(
                            capturedTag,
                            GetRegistry(),
                            _ => RefreshSection());
                    })
                    {
                        text = $"{tag.DisplayName}  ({tag.TagId})"
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

    internal static class AssetTagDefinitionSearchExtensions
    {
        public static bool IsValidForSearch(this AssetTagDefinition tag)
        {
            return tag != null &&
                   !string.IsNullOrWhiteSpace(tag.DisplayName) &&
                   !string.IsNullOrWhiteSpace(tag.TagId);
        }
    }
}
