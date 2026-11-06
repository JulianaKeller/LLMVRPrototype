using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public class TagsSection : DatabaseToolSubsection<AssetTagDefinition>
    {
        private TextField tagNameField;
        private VisualElement suggestionsContainer;
        private Label feedbackLabel;

        public TagsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
            
        }

        protected override string DefinitionTypeName => "Tag";

        protected override string GetDisplayName(AssetTagDefinition tag)
        {
            return tag.DisplayName;
        }

        protected override string GetIdentifier(AssetTagDefinition tag)
        {
            return tag.TagId;
        }

        protected override IEnumerable<AssetTagDefinition> GetDefinitions()
        {
            return context.DatabaseConfig.TagRegistry.Tags;
        }

        protected override IEnumerable<string> GetSearchTerms(AssetTagDefinition tag)
        {
            foreach (string term in base.GetSearchTerms(tag))
                yield return term;

            if (tag.Synonyms == null)
                yield break;

            foreach (string synonym in tag.Synonyms)
                yield return synonym;
        }

        protected override string GetSectionTitle()
        {
            return "Tags";
        }

        protected override string GetSectionDescription()
        {
            return "Create, find, and edit asset tags.";
        }

        protected override void OpenForCreate(
            string displayName,
            Action<AssetTagDefinition> onClosed)
        {
            TagEditorWindow.OpenForCreate(
                context.DatabaseConfig.TagRegistry,
                displayName,
                onClosed);
        }

        protected override void OpenForEdit(
            AssetTagDefinition tag,
            Action<AssetTagDefinition> onClosed)
        {
            TagEditorWindow.OpenForEdit(
                tag,
                context.DatabaseConfig.TagRegistry,
                onClosed);
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
