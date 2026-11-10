using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public class TagsSection : DatabaseToolSubsection<AssetTagDefinition>
    {
        public TagsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
        }

        protected override string DefinitionTypeName => "Tag";

        protected override string GetDisplayName(AssetTagDefinition tag) => tag.DisplayName;

        protected override string GetIdentifier(AssetTagDefinition tag) => tag.TagId;

        protected override IEnumerable<AssetTagDefinition> GetDefinitions()
        {
            return context.DatabaseConfig.TagRegistry.Tags;
        }

        protected override IEnumerable<string> GetSynonyms(AssetTagDefinition tag)
        {
            return tag.Synonyms;
        }

        protected override string GetSectionTitle() => "Tags";

        protected override string GetSectionDescription() => "Create, find, and edit asset tags.";

        protected override void OpenForCreate(string displayName, Action<AssetTagDefinition> onClosed)
        {
            TagEditorWindow.OpenForCreate(
                context.DatabaseConfig.TagRegistry,
                displayName,
                onClosed);
        }

        protected override void OpenForEdit(AssetTagDefinition tag, Action<AssetTagDefinition> onClosed)
        {
            TagEditorWindow.OpenForEdit(
                tag,
                context.DatabaseConfig.TagRegistry,
                onClosed);
        }
    }
}