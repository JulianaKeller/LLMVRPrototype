using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class CategoriesSection : DatabaseToolSection
    {

        private TextField categoryNameField;
        private VisualElement suggestionsContainer;
        private Label feedbackLabel;

        public CategoriesSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {

        }

        protected override string GetSectionTitle()
        {
            return "Categories";
        }

        protected override string GetSectionDescription()
        {
            return "Create, find, and edit asset categories.";
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            AddHeading("Create or Find a Category");

            categoryNameField = new TextField("Category Display Name")
            {
                value = string.Empty
            };
            categoryNameField.tooltip = "Enter a category name, then press Enter to create it.";
            categoryNameField.style.marginBottom = 4;
            root.Add(categoryNameField);

            suggestionsContainer = new VisualElement();
            suggestionsContainer.style.marginBottom = 8;
            root.Add(suggestionsContainer);

            feedbackLabel = new Label();
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.marginBottom = 8;
            root.Add(feedbackLabel);

            categoryNameField.RegisterValueChangedCallback(evt => RefreshSuggestions(evt.newValue));

            categoryNameField.RegisterCallback<KeyDownEvent>(OnInputKeyDown);

            AddHeading("Registered Categories");
            BuildRegisteredCategoryList();

            categoryNameField.Focus();
        }

        private AssetCategoryRegistry GetRegistry()
        {
            return context.DatabaseConfig.CategoryRegistry;
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            evt.StopPropagation();

            string displayName = categoryNameField.value.Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                feedbackLabel.text = "Enter a category display name first.";
                return;
            }

            AssetCategoryDefinition exactMatch = FindExactMatch(displayName);

            if (exactMatch != null)
            {
                CategoryEditorWindow.OpenForEdit(
                    exactMatch,
                    GetRegistry(),
                    _ => RefreshSection());

                return;
            }

            CategoryEditorWindow.OpenForCreate(
                GetRegistry(),
                displayName,
                _ => RefreshSection());
        }

        private void RefreshSuggestions(string query)
        {
            suggestionsContainer.Clear();

            if (string.IsNullOrWhiteSpace(query))
                return;

            List<AssetCategoryDefinition> matches = FindSimilarCategories(query.Trim());

            if (matches.Count == 0)
                return;

            var label = new Label("Existing or similar categories");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 4;
            suggestionsContainer.Add(label);

            foreach (AssetCategoryDefinition match in matches)
            {
                AssetCategoryDefinition capturedMatch = match;

                var button = new Button(() =>
                {
                    CategoryEditorWindow.OpenForEdit(
                        capturedMatch,
                        GetRegistry(),
                        _ => RefreshSection());
                })
                {
                    text = $"{match.DisplayName}  ({match.CategoryId})"
                };

                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.marginTop = 2;
                button.style.marginBottom = 2;
                suggestionsContainer.Add(button);
            }
        }

        private AssetCategoryDefinition FindExactMatch(string query)
        {
            foreach (AssetCategoryDefinition category in GetRegistry().Categories)
            {
                if (category != null &&
                    (string.Equals(
                         category.DisplayName,
                         query,
                         StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                         category.CategoryId,
                         query,
                         StringComparison.OrdinalIgnoreCase)))
                {
                    return category;
                }
            }

            return null;
        }

        private List<AssetCategoryDefinition> FindSimilarCategories(string query)
        {
            var results = new List<AssetCategoryDefinition>();
            string normalizedQuery = query.Trim();

            foreach (AssetCategoryDefinition category in GetRegistry().Categories)
            {
                if (category == null || !category.IsValidForSearch())
                    continue;

                string name = category.DisplayName.Trim();

                bool synonymMatch = false;

                if (category.Synonyms != null)
                {
                    foreach (string synonym in category.Synonyms)
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

                bool partialMatch = name.IndexOf(
                        normalizedQuery,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    category.CategoryId.IndexOf(
                        normalizedQuery,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    synonymMatch;

                bool closeSpelling = EditDistance(
                        name.ToLowerInvariant(),
                        normalizedQuery.ToLowerInvariant())
                    <= Math.Max(2, normalizedQuery.Length / 3);

                if (partialMatch || closeSpelling || synonymMatch)
                    results.Add(category);
            }

            results.Sort((a, b) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase));

            return results;
        }

        private void BuildRegisteredCategoryList()
        {
            var list = new ScrollView();
            list.style.maxHeight = 280;
            list.style.marginTop = 4;

            List<AssetCategoryDefinition> categories = new List<AssetCategoryDefinition>(GetRegistry().Categories);

            categories.RemoveAll(category => category == null);
            categories.Sort((a, b) => string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase));

            if (categories.Count == 0)
            {
                list.Add(new Label("No tags have been registered yet."));
            }
            else
            {
                foreach (AssetCategoryDefinition category in categories)
                {
                    AssetCategoryDefinition capturedCategory = category;

                    var button = new Button(() =>
                    {
                        CategoryEditorWindow.OpenForEdit(
                            capturedCategory,
                            GetRegistry(),
                            _ => RefreshSection());
                    })
                    {
                        text = $"{category.DisplayName}  ({category.CategoryId})"
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

    internal static class AssetCategoryDefinitionSearchExtensions
    {
        public static bool IsValidForSearch(this AssetCategoryDefinition category)
        {
            return category != null &&
                   !string.IsNullOrWhiteSpace(category.DisplayName) &&
                   !string.IsNullOrWhiteSpace(category.CategoryId);
        }
    }
}
