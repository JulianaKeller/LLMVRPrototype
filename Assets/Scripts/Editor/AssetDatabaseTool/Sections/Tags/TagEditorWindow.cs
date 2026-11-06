using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    // This popup edits the ScriptableObject through Unity's SerializedObject
    public sealed class TagEditorWindow : TaxonomyMetadataEditorWindow<AssetTagDefinition>
    {
        private const string TagFolder = "Assets/EnvironmentLibrary/Definitions/Tags";

        private AssetTagRegistry registry;
        private AssetTagDefinition selectedParentTag;

        protected override string DefinitionTypeName => "Tag";
        protected override string IdPropertyName => "id";
        protected override string DisplayNamePropertyName => "displayName";
        protected override string DescriptionPropertyName => "description";
        protected override string DefaultAssetFolder => TagFolder;
        protected override string ParentPropertyName => "parentTag";

        protected override AssetTagDefinition SelectedParent
        {
            get => selectedParentTag;
            set => selectedParentTag = value;
        }

        public static void OpenForCreate(
            AssetTagRegistry registry,
            string displayName,
            Action<AssetTagDefinition> onSaved = null)
        {
            var window = CreateInstance<TagEditorWindow>();
            window.registry = registry;
            window.selectedParentTag = null;

            window.InitializeCreate(displayName, onSaved);
            window.ShowEditor("Create Tag");
        }

        public static void OpenForEdit(
            AssetTagDefinition tag,
            AssetTagRegistry registry,
            Action<AssetTagDefinition> onSaved = null)
        {
            if (tag == null)
                return;

            var window = CreateInstance<TagEditorWindow>();
            window.registry = registry;
            window.selectedParentTag = tag.ParentTag;

            window.InitializeEdit(tag, onSaved);
            window.ShowEditor("Edit Tag");
        }

        protected override string GetDefinitionId(AssetTagDefinition item)
        {
            return item.TagId;
        }

        protected override string GetDefinitionDisplayName(AssetTagDefinition item)
        {
            return item.DisplayName;
        }

        protected override IEnumerable<AssetTagDefinition> GetAllDefinitions()
        {
            return registry != null && registry.Tags != null
                ? registry.Tags
                : new List<AssetTagDefinition>();
        }

        protected override AssetTagDefinition GetParent(AssetTagDefinition item)
        {
            return item.ParentTag;
        }

        protected override string GetHierarchyItemName(AssetTagDefinition item)
        {
            return item.DisplayName;
        }

        protected override bool HasId(string id)
        {
            if (registry?.Tags == null)
                return false;

            foreach (AssetTagDefinition item in registry.Tags)
            {
                if (item != null &&
                    string.Equals(
                        item.TagId,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        protected override bool HasDuplicateId(AssetTagDefinition candidate)
        {
            if (registry?.Tags == null)
                return false;

            foreach (AssetTagDefinition item in registry.Tags)
            {
                if (item == null || item == candidate)
                    continue;

                if (string.Equals(
                    item.TagId,
                    candidate.TagId,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        protected override bool RegisterDefinition(AssetTagDefinition item)
        {
            return registry != null && registry.RegisterTag(item);
        }

        protected override void UnregisterDefinition(AssetTagDefinition item)
        {
            registry?.UnregisterTag(item);
        }

        protected override void RebuildRegistryIndex()
        {
            registry?.RebuildIndex();
        }

        protected override void MarkRegistryDirty()
        {
            if (registry != null)
                UnityEditor.EditorUtility.SetDirty(registry);
        }

        protected override void BuildSpecificFields(VisualElement container)
        {
            AddProperty(container, "synonyms", "Synonyms");
            base.BuildSpecificFields(container);
        }

        protected override void OnDefinitionSaved(AssetTagDefinition item)
        {
        }

        protected override MetadataGenerationInput GetMetadataInput()
        {
            return new TagMetadataGenerationInput(
                definition.DisplayName,
                definition.Description,
                selectedParentTag?.DisplayName,
                selectedParentTag?.Description,
                3,
                8);
        }

    }
}
