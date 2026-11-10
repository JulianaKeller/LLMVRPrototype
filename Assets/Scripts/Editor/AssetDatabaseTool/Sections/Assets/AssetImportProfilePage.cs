using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    // Displays and edits one asset's fields
    public sealed class AssetImportProfilePage : IDisposable
    {
        private readonly AssetImportItem item;
        private readonly AssetDefinition definition;
        private readonly SerializedObject serializedObject;

        private Image thumbnailPreview; 
        private ObjectAssetThumbnailPreview objectThumbnailPreview;

        private readonly VisualElement root;
        private readonly VisualElement propertyContainer;

        private Label selectedCategoryLabel;

        private Foldout selectedTagsFoldout;
        private VisualElement selectedTagsContainer;

        private Foldout selectedOperationsFoldout;
        private VisualElement selectedOperationsContainer;

        private readonly TaxonomySelectionState<AssetCategoryDefinition> categorySelection;
        private readonly TaxonomySelectionState<AssetTagDefinition> tagSelection;
        private readonly TaxonomySelectionState<OperationDefinition> operationSelection;

        private TaxonomySearchField<AssetCategoryDefinition> categorySearch;
        private TaxonomyHierarchyView<AssetCategoryDefinition> categoryHierarchy;

        private TaxonomySearchField<AssetTagDefinition> tagSearch;
        private TaxonomyHierarchyView<AssetTagDefinition> tagHierarchy;

        private TaxonomySearchField<OperationDefinition> operationSearch;

        public AssetImportItem Item => item;
        public AssetDefinition Definition => definition;
        public SerializedObject SerializedObject => serializedObject;
        public VisualElement Root => root;

        private bool isDisposed;

        private Vector3Field importRotationField;

        public AssetImportProfilePage(VisualElement parent, AssetImportItem item, Action<Sprite, Texture2D, Sprite, Texture2D> replaceGeneratedThumbnail = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.item = item ?? throw new ArgumentNullException(nameof(item));

            definition = item.Definition;

            if (definition == null)
                throw new InvalidOperationException($"Import item '{item.Candidate.DisplayName}' does not have a temporary definition.");

            serializedObject = new SerializedObject(definition);

            categorySelection = new TaxonomySelectionState<AssetCategoryDefinition>(category => category.CategoryId);
            tagSelection = new TaxonomySelectionState<AssetTagDefinition>(tag => tag.TagId);
            operationSelection = new TaxonomySelectionState<OperationDefinition>(operation => operation.OperationId);

            root = new VisualElement();
            root.style.flexGrow = 1;

            propertyContainer = new VisualElement();
            propertyContainer.style.flexGrow = 1;

            root.Add(propertyContainer);
            parent.Add(root);

            Build(replaceGeneratedThumbnail);
        }

        #region UI

        private void Build(Action<Sprite, Texture2D, Sprite, Texture2D> replaceGeneratedThumbnail = null)
        {
            root.Clear();
            root.Add(propertyContainer);

            BuildHeader();
            BuildThumbnailPreview(replaceGeneratedThumbnail);
            BuildProperties();

            serializedObject.Update();
            root.Bind(serializedObject);
        }

        private void BuildHeader()
        {
            VisualElement header = new VisualElement();
            header.style.marginBottom = 12;

            Label title = new Label(item.Candidate.PreliminaryName);
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4;

            Label sourcePath = new Label(item.Candidate.AssetPath);
            sourcePath.style.fontSize = 11;
            sourcePath.style.color = new Color(0.65f, 0.65f, 0.65f);
            sourcePath.style.whiteSpace = WhiteSpace.Normal;

            header.Add(title);
            header.Add(sourcePath);

            root.Add(header);
        }

        #region Thumbnail Preview

        private void BuildThumbnailPreview(Action<Sprite, Texture2D, Sprite, Texture2D> replaceGeneratedThumbnail = null)
        {
            if (definition is ObjectAssetDefinition objectDefinition)
            {
                VisualElement previewContainer = new VisualElement();
                previewContainer.style.flexDirection = FlexDirection.Row;
                previewContainer.style.alignItems = Align.Center;
                previewContainer.style.marginBottom = 12;

                VisualElement directionalControls = new VisualElement();
                directionalControls.style.justifyContent = Justify.Center;
                directionalControls.style.alignItems = Align.Center;
                directionalControls.style.marginRight = 8;

                Button topButton = CreateRotationButton("Up", new Vector3(-90f, 0f, 0f));
                Button leftButton = CreateRotationButton("Left", new Vector3(0f, -90f, 0f));
                Button rightButton = CreateRotationButton("Right", new Vector3(0f, 90f, 0f));
                Button bottomButton = CreateRotationButton("Down", new Vector3(90f, 0f, 0f));

                VisualElement middleRow = new VisualElement();
                middleRow.style.flexDirection = FlexDirection.Row;

                middleRow.Add(leftButton);
                middleRow.Add(rightButton);

                directionalControls.Add(topButton);
                directionalControls.Add(middleRow);
                directionalControls.Add(bottomButton);

                thumbnailPreview = new Image();
                thumbnailPreview.style.width = 160;
                thumbnailPreview.style.height = 120;
                thumbnailPreview.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                thumbnailPreview.scaleMode = ScaleMode.ScaleToFit;

                previewContainer.Add(directionalControls);
                previewContainer.Add(thumbnailPreview);

                root.Add(previewContainer);

                objectThumbnailPreview = new ObjectAssetThumbnailPreview(
                    objectDefinition,
                    serializedObject,
                    thumbnailPreview,
                    replaceGeneratedThumbnail);
            }
            else
            {
                thumbnailPreview = new Image();
                thumbnailPreview.style.width = 160;
                thumbnailPreview.style.height = 120;
                thumbnailPreview.style.marginBottom = 12;
                thumbnailPreview.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                thumbnailPreview.scaleMode = ScaleMode.ScaleToFit;

                root.Add(thumbnailPreview);

                SerializedProperty thumbnailProperty = serializedObject.FindProperty("thumbnail");

                if (thumbnailProperty != null &&
                    thumbnailProperty.objectReferenceValue is Sprite sprite)
                {
                    thumbnailPreview.image = sprite.texture;
                }
            }
        }

        private Button CreateRotationButton(string text, Vector3 rotationDelta)
        {
            Button button = new Button(() => RotateObject(rotationDelta))
            {
                text = text
            };

            button.style.width = 50;
            button.style.height = 30;
            button.style.marginBottom = 2;
            button.style.marginTop = 2;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;

            return button;
        }

        private void RotateObject(Vector3 rotationDelta)
        {
            SerializedProperty rotationProperty = serializedObject.FindProperty("importRotation");

            if (rotationProperty == null)
                return;

            Vector3 newRotation = rotationProperty.vector3Value + rotationDelta;

            newRotation.x = Mathf.Repeat(newRotation.x, 360f);
            newRotation.y = Mathf.Repeat(newRotation.y, 360f);
            newRotation.z = Mathf.Repeat(newRotation.z, 360f);

            SetImportRotation(newRotation);
        }

        #endregion

        private void BuildProperties()
        {
            propertyContainer.Clear();

            SerializedProperty iterator = serializedObject.GetIterator();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                // The Script reference is an internal Unity field and should not be editable
                if (iterator.propertyPath == "m_Script")
                    continue;

                if (iterator.propertyPath == "categoryId")
                {
                    BuildCategorySelector(iterator.Copy());
                    continue;
                }
                if (iterator.propertyPath == "tagIds")
                {
                    BuildTagSelector(iterator.Copy());
                    continue;
                }
                if (iterator.propertyPath == "supportedOperationIds")
                {
                    BuildOperationSelector(iterator.Copy());
                    continue;
                }

                // Object rotation gets its own control because changing it must regenerate the thumbnail.
                if (iterator.propertyPath == "importRotation" && definition is ObjectAssetDefinition)
                {
                    BuildImportRotationField(iterator.Copy());
                    continue;
                }

                SerializedProperty property = iterator.Copy();

                PropertyField propertyField = new PropertyField(property);
                propertyField.Bind(serializedObject);
                propertyField.style.marginBottom = 16;

                propertyContainer.Add(propertyField);
            }
        }

        #region Category Selector

        private void BuildCategorySelector(SerializedProperty property)
        {
            categorySelection.SetSelectedIds(
                string.IsNullOrWhiteSpace(property.stringValue)
                    ? Array.Empty<string>()
                    : new[] { property.stringValue });

            var container = new Foldout { text = "Category", value = true };

            selectedCategoryLabel = new Label();
            selectedCategoryLabel.style.marginBottom = 8;
            selectedCategoryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            container.Add(selectedCategoryLabel);

            categorySearch = new TaxonomySearchField<AssetCategoryDefinition>(
                "Search Categories",
                AssetDatabaseAccessor.CategoryRegistry.Categories,
                category => category.DisplayName,
                category => category.CategoryId,
                category => category.Synonyms,
                SelectCategory);

            categoryHierarchy = new TaxonomyHierarchyView<AssetCategoryDefinition>(
                AssetDatabaseAccessor.CategoryRegistry.Categories,
                category => category.DisplayName,
                category => category.ParentCategory,
                category => category.CategoryId,
                SelectCategory,
                categorySelection.IsSelected,
                whatIsBeingSelected: "Category");

            container.Add(categorySearch.Root);
            container.Add(categoryHierarchy.Root);

            propertyContainer.Add(container);
        }

        private void SelectCategory(AssetCategoryDefinition category)
        {
            categorySelection.SetSingle(category);

            SerializedProperty property = serializedObject.FindProperty("categoryId");
            if (property != null)
            {
                property.stringValue = category != null ? category.CategoryId : string.Empty;
                serializedObject.ApplyModifiedProperties();
            }

            RefreshSelectedCategory();
            categoryHierarchy?.Refresh();
        }

        private void RefreshSelectedCategory()
        {
            string selectedCategoryId = categorySelection.SelectedIds.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(selectedCategoryId))
            {
                selectedCategoryLabel.text = "No category selected";
                return;
            }

            AssetCategoryDefinition category = AssetDatabaseAccessor.CategoryRegistry.Categories
                .FirstOrDefault(item => item != null &&
                    string.Equals(item.CategoryId, selectedCategoryId, StringComparison.OrdinalIgnoreCase));

            selectedCategoryLabel.text = category != null
                ? $"Selected category: {category.DisplayName}"
                : $"Selected category: {selectedCategoryId} (unresolved)";
        }

        #endregion

        #region Tag Selector

        private void BuildTagSelector(SerializedProperty property)
        {
            tagSelection.SetSelectedIds(ReadStringArray(property));

            var container = new Foldout { text = "Tags", value = true };
            container.style.marginBottom = 16;

            selectedTagsFoldout = new Foldout
            {
                text = "Selected Tags",
                value = true
            };

            selectedTagsContainer = new VisualElement();
            selectedTagsFoldout.Add(selectedTagsContainer);
            container.Add(selectedTagsFoldout);

            tagSearch = new TaxonomySearchField<AssetTagDefinition>(
                "Search Tags",
                AssetDatabaseAccessor.TagRegistry.Tags,
                tag => tag.DisplayName,
                tag => tag.TagId,
                tag => tag.Synonyms,
                ToggleTag);

            tagHierarchy = new TaxonomyHierarchyView<AssetTagDefinition>(
                AssetDatabaseAccessor.TagRegistry.Tags,
                tag => tag.DisplayName,
                tag => tag.ParentTag,
                tag => tag.TagId,
                ToggleTag,
                tagSelection.IsSelected,
                whatIsBeingSelected: "Tag");

            container.Add(tagSearch.Root);
            container.Add(tagHierarchy.Root);

            propertyContainer.Add(container);

            RefreshSelectedTags();
        }

        private void ToggleTag(AssetTagDefinition tag)
        {
            tagSelection.Toggle(tag);

            WriteStringArray("tagIds", tagSelection.ToList());

            RefreshSelectedTags();
            tagHierarchy?.Refresh();
        }

        private void RefreshSelectedTags()
        {
            if (selectedTagsContainer == null)
                return;

            selectedTagsContainer.Clear();

            foreach (string id in tagSelection.SelectedIds.OrderBy(id => id))
            {
                AssetTagDefinition tag =
                    AssetDatabaseAccessor.TagRegistry.Tags
                        .FirstOrDefault(item => item.TagId == id);

                string displayName = tag != null
                    ? tag.DisplayName
                    : $"{id} (unresolved)";

                var label = new Label("• " + displayName);
                label.style.marginBottom = 3;

                selectedTagsContainer.Add(label);
            }

            if (tagSelection.SelectedIds.Count == 0)
                selectedTagsContainer.Add(new Label("No tags selected."));
        }

        #endregion

        #region Operation Selector

        private void BuildOperationSelector(SerializedProperty property)
        {
            operationSelection.SetSelectedIds(ReadStringArray(property));

            var container = new Foldout
            {
                text = "Supported Operations",
                value = true
            };

            container.style.marginBottom = 16;

            selectedOperationsFoldout = new Foldout
            {
                text = "Selected Operations",
                value = true
            };

            selectedOperationsContainer = new VisualElement();
            selectedOperationsFoldout.Add(selectedOperationsContainer);
            container.Add(selectedOperationsFoldout);

            operationSearch = new TaxonomySearchField<OperationDefinition>(
                "Search Operations",
                AssetDatabaseAccessor.OperationCatalog.Operations,
                operation => operation.DisplayName,
                operation => operation.OperationId,
                null,
                ToggleOperation);

            container.Add(operationSearch.Root);
            propertyContainer.Add(container);

            RefreshSelectedOperations();
        }

        private void ToggleOperation(OperationDefinition operation)
        {
            operationSelection.Toggle(operation);

            WriteStringArray("supportedOperationIds", operationSelection.ToList());

            RefreshSelectedOperations();
        }

        private void RefreshSelectedOperations()
        {
            if (selectedOperationsContainer == null)
                return;

            selectedOperationsContainer.Clear();

            foreach (string id in operationSelection.SelectedIds.OrderBy(id => id))
            {
                OperationDefinition operation =
                    AssetDatabaseAccessor.OperationCatalog.Operations
                        .FirstOrDefault(item => item.OperationId == id);

                string displayName = operation != null
                    ? operation.DisplayName
                    : $"{id} (unresolved)";

                var label = new Label("• " + displayName);
                label.style.marginBottom = 3;

                selectedOperationsContainer.Add(label);
            }

            if (operationSelection.SelectedIds.Count == 0)
                selectedOperationsContainer.Add(new Label("No operations selected."));
        }

        #endregion

        #region Serialization Helpers

        private static List<string> ReadStringArray(SerializedProperty property)
        {
            var values = new List<string>();

            if (property == null || !property.isArray)
                return values;

            for (int i = 0; i < property.arraySize; i++)
            {
                string value = property.GetArrayElementAtIndex(i).stringValue;

                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }

            return values;
        }

        private void WriteStringArray(string propertyPath, IReadOnlyList<string> values)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null || !property.isArray)
                return;

            property.arraySize = values.Count;

            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i];

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Rotation Field

        private void BuildImportRotationField(SerializedProperty rotationProperty)
        {
            var rotationContainer = new VisualElement();
            rotationContainer.style.marginBottom = 16;

            importRotationField = new Vector3Field("Import Rotation");
            importRotationField.value = rotationProperty.vector3Value;

            importRotationField.RegisterValueChangedCallback(evt =>
            {
                SetImportRotation(evt.newValue);
            });

            rotationContainer.Add(importRotationField);

            var resetButton = new Button(() => SetImportRotation(Vector3.zero))
            {
                text = "Reset Rotation"
            };

            resetButton.style.marginTop = 4;
            rotationContainer.Add(resetButton);

            propertyContainer.Add(rotationContainer);
        }

        private void SetImportRotation(Vector3 rotation)
        {
            SerializedProperty rotationProperty = serializedObject.FindProperty("importRotation");

            if (rotationProperty == null)
                return;

            rotationProperty.vector3Value = rotation;
            serializedObject.ApplyModifiedProperties();

            // Synchronize the numeric fields without triggering their change callback
            importRotationField?.SetValueWithoutNotify(rotation);

            // Regenerate the thumbnail using the updated rotation
            objectThumbnailPreview?.RegenerateThumbnail();
        }

        #endregion

        #endregion

        public void ApplyChanges()
        {
            serializedObject.ApplyModifiedProperties();
        }

        public void Refresh()
        {
            ApplyChanges();
            serializedObject.Update();
        }

        // When navigating away from the profile page, this applies pending changes
        public void SetVisible(bool visible)
        {
            if (visible)
            {
                Refresh();
                root.style.display = DisplayStyle.Flex;
            }
            else
            {
                ApplyChanges();
                root.style.display = DisplayStyle.None;
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            // Releases PreviewRenderUtility and its temporary preview objects.
            objectThumbnailPreview?.Dispose();
            objectThumbnailPreview = null;

            // Remove this page from its parent visual element.
            root.RemoveFromHierarchy();

            thumbnailPreview = null;
        }
    }
}
