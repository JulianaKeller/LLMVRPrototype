using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class CategoriesSection : DatabaseToolSubsection<AssetCategoryDefinition>
    {

        private TextField categoryNameField;
        private VisualElement suggestionsContainer;
        private Label feedbackLabel;

        public CategoriesSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {

        }

        protected override string DefinitionTypeName => "Category";

        protected override string GetDisplayName(AssetCategoryDefinition category)
        {
            return category.DisplayName;
        }

        protected override string GetIdentifier(AssetCategoryDefinition category)
        {
            return category.CategoryId;
        }

        protected override IEnumerable<AssetCategoryDefinition> GetDefinitions()
        {
            return context.DatabaseConfig.CategoryRegistry.Categories;
        }

        protected override IEnumerable<string> GetSearchTerms(AssetCategoryDefinition category)
        {
            foreach (string term in base.GetSearchTerms(category))
                yield return term;

            if (category.Synonyms == null)
                yield break;

            foreach (string synonym in category.Synonyms)
                yield return synonym;
        }



        protected override string GetSectionTitle()
        {
            return "Categories";
        }

        protected override string GetSectionDescription()
        {
            return "Create, find, and edit asset categories.";
        }

        protected override void OpenForCreate(
            string displayName,
            Action<AssetCategoryDefinition> onClosed)
        {
            CategoryEditorWindow.OpenForCreate(
                context.DatabaseConfig.CategoryRegistry,
                displayName,
                onClosed);
        }

        protected override void OpenForEdit(
            AssetCategoryDefinition category,
            Action<AssetCategoryDefinition> onClosed)
        {
            CategoryEditorWindow.OpenForEdit(
                category,
                context.DatabaseConfig.CategoryRegistry,
                onClosed);
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
