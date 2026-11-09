using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public sealed class CategoryEditorWindow : TaxonomyMetadataEditorWindow<AssetCategoryDefinition>
    {
        private const string CategoryFolder = DatabaseFolder + "/Categories";

        private AssetCategoryRegistry registry;
        private AssetCategoryDefinition selectedParentCategory;

        protected override string DefinitionTypeName => "Category";
        protected override string IdPropertyName => "id";
        protected override string DisplayNamePropertyName => "displayName";
        protected override string DescriptionPropertyName => "description";
        protected override string DefaultAssetFolder => CategoryFolder;
        protected override string ParentPropertyName => "parentCategory";

        protected override AssetCategoryDefinition SelectedParent
        {
            get => selectedParentCategory;
            set => selectedParentCategory = value;
        }

        public static void OpenForCreate(
            AssetCategoryRegistry registry,
            string displayName,
            Action<AssetCategoryDefinition> onSaved = null)
        {
            var window = CreateInstance<CategoryEditorWindow>();
            window.registry = registry;
            window.selectedParentCategory = null;

            window.InitializeCreate(displayName, onSaved);
            window.ShowEditor("Create Category");
        }

        public static void OpenForEdit(
            AssetCategoryDefinition category,
            AssetCategoryRegistry registry,
            Action<AssetCategoryDefinition> onSaved = null)
        {
            if (category == null)
                return;

            var window = CreateInstance<CategoryEditorWindow>();
            window.registry = registry;
            window.selectedParentCategory = category.ParentCategory;

            window.InitializeEdit(category, onSaved);
            window.ShowEditor("Edit Category");
        }

        protected override string GetDefinitionId(AssetCategoryDefinition item)
        {
            return item.CategoryId;
        }

        protected override string GetDefinitionDisplayName(AssetCategoryDefinition item)
        {
            return item.DisplayName;
        }

        protected override IEnumerable<AssetCategoryDefinition> GetAllDefinitions()
        {
            return registry != null && registry.Categories != null
                ? registry.Categories
                : new List<AssetCategoryDefinition>();
        }

        protected override AssetCategoryDefinition GetParent(AssetCategoryDefinition item)
        {
            return item.ParentCategory;
        }

        protected override string GetHierarchyItemName(AssetCategoryDefinition item)
        {
            return item.DisplayName;
        }

        protected override bool HasId(string id)
        {
            if (registry?.Categories == null)
                return false;

            foreach (AssetCategoryDefinition item in registry.Categories)
            {
                if (item != null &&
                    string.Equals(
                        item.CategoryId,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        protected override bool HasDuplicateId(AssetCategoryDefinition candidate)
        {
            if (registry?.Categories == null)
                return false;

            foreach (AssetCategoryDefinition item in registry.Categories)
            {
                if (item == null || item == candidate)
                    continue;

                if (string.Equals(
                    item.CategoryId,
                    candidate.CategoryId,
                    StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        protected override bool RegisterDefinition(AssetCategoryDefinition item)
        {
            return registry != null && registry.RegisterCategory(item);
        }

        protected override void UnregisterDefinition(AssetCategoryDefinition item)
        {
            registry?.UnregisterCategory(item);
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

        protected override void OnDefinitionSaved(AssetCategoryDefinition item)
        {
        }

        protected override MetadataGenerationInput GetMetadataInput()
        {
            return new CategoryMetadataGenerationInput(
                definition.DisplayName,
                definition.Description,
                selectedParentCategory?.DisplayName,
                selectedParentCategory?.Description,
                3,
                8);
        }
    }
}