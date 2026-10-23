using LLMVRPrototype.Assets;
using LLMVRPrototype.LLM.Providers;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public class CategoryEditorWindow : DatabaseEditorWindow
    {
        private const string DefaultTagFolder = "Assets/EnvironmentLibrary/Definitions/Categories";

        private AssetCategoryDefinition category;
        private AssetCategoryRegistry registry;
        private SerializedObject serializedCategory;
        private bool isNewCategory;
        private string assetFolder;
        private Action<AssetCategoryDefinition> onSaved;

        private AssetCategoryDefinition selectedParentCategory;
        private Label statusLabel;
        private Label selectedParentLabel;
        private VisualElement parentTreeContainer;

        #region UI Methods

        public static void OpenForCreate(AssetCategoryRegistry registry,
            string displayName,
            Action<AssetCategoryDefinition> onSaved = null)
        {
            var window = CreateInstance<CategoryEditorWindow>();

            window.titleContent = new GUIContent("Create Category");
            window.registry = registry;
            window.category = CreateInstance<AssetCategoryDefinition>();
            window.isNewCategory = true;
            window.assetFolder = DefaultTagFolder;
            window.onSaved = onSaved;
            window.selectedParentCategory = null;

            window.category.name = displayName;

            SerializedObject serialized = new SerializedObject(window.category);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("id").stringValue = GenerateUniqueTagId(displayName, registry);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            window.ShowUtility();
            window.minSize = new Vector2(420, 360);
            window.maxSize = new Vector2(700, 800);
            window.RebuildUI();
        }

        public static void OpenForEdit(
            AssetCategoryDefinition existingCategory,
            AssetCategoryRegistry registry,
            Action<AssetCategoryDefinition> onSaved = null)
        {
            if (existingCategory == null)
                return;

            var window = CreateInstance<CategoryEditorWindow>();

            window.titleContent = new GUIContent("Edit Category");
            window.registry = registry;
            window.category = existingCategory;
            window.isNewCategory = false;
            window.assetFolder = DefaultTagFolder;
            window.onSaved = onSaved;
            window.selectedParentCategory = existingCategory.ParentCategory;

            window.ShowUtility();
            window.minSize = new Vector2(420, 360);
            window.maxSize = new Vector2(700, 800);
            window.RebuildUI();
        }

        private void RebuildUI()
        {
            rootVisualElement.Clear();

            if (category == null)
            {
                rootVisualElement.Add(new Label("No category selected."));
                return;
            }

            serializedCategory = new SerializedObject(category);

            var container = new ScrollView();
            container.style.paddingLeft = 12;
            container.style.paddingRight = 12;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;
            rootVisualElement.Add(container);

            var heading = new Label(isNewCategory ? "Create Category" : "Edit Category");
            heading.style.fontSize = 16;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 10;
            container.Add(heading);

            AddProperty(container, "id", "Category ID");
            AddProperty(container, "displayName", "Display Name");
            AddProperty(container, "description", "Description");
            AddProperty(container, "synonyms", "Synonyms");

            BuildParentSelector(container);

            statusLabel = new Label("Press Enter to save. Press Escape to cancel.");
            statusLabel.style.marginTop = 10;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            container.Add(statusLabel);

            var saveButton = new Button(SaveAndClose)
            {
                text = "Save Category"
            };
            saveButton.style.marginTop = 8;
            container.Add(saveButton);

            var cancelButton = new Button(CancelAndClose)
            {
                text = "Cancel"
            };
            cancelButton.style.marginTop = 4;
            container.Add(cancelButton);

            if (!isNewCategory)
            {
                var deleteButton = new Button(DeleteCategory)
                {
                    text = "Delete Category"
                };

                deleteButton.style.marginTop = 12;
                deleteButton.style.color = new Color(0.8f, 0.2f, 0.2f);
                container.Add(deleteButton);
            }

            if (isNewCategory)
            {
                // TODO
                //BuildMetadataGenerationUI(container);
            }

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private void AddProperty(
            VisualElement parent,
            string propertyName,
            string label)
        {
            SerializedProperty property = serializedCategory.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogWarning($"Category property '{propertyName}' was not found.");
                return;
            }

            var field = new PropertyField(property, label);
            field.Bind(serializedCategory);
            field.style.marginBottom = 6;
            parent.Add(field);
        }

        #endregion

        #region Parent Selector

        private void BuildParentSelector(VisualElement container)
        {
            var section = new Foldout
            {
                text = "Parent Category",
                value = false
            };
            section.style.marginTop = 10;
            container.Add(section);

            selectedParentLabel = new Label();
            selectedParentLabel.style.whiteSpace = WhiteSpace.Normal;
            selectedParentLabel.style.marginBottom = 6;
            section.Add(selectedParentLabel);

            var noneButton = new Button(() => SelectParent(null))
            {
                text = "None (root category)"
            };
            noneButton.style.marginBottom = 6;
            section.Add(noneButton);

            var scrollView = new ScrollView();
            scrollView.style.height = 220;
            scrollView.style.minHeight = 100;
            scrollView.style.borderTopWidth = 1;
            scrollView.style.borderBottomWidth = 1;
            scrollView.style.borderLeftWidth = 1;
            scrollView.style.borderRightWidth = 1;
            scrollView.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            scrollView.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            scrollView.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            scrollView.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            section.Add(scrollView);

            parentTreeContainer = scrollView;

            RefreshSelectedParentLabel();
            RefreshParentTree();
        }

        private void RefreshSelectedParentLabel()
        {
            selectedParentLabel.text = selectedParentCategory == null
                ? "Current parent: None (root category)"
                : $"Current parent: {selectedParentCategory.DisplayName}";
        }

        private void RefreshParentTree()
        {
            if (parentTreeContainer == null)
                return;

            parentTreeContainer.Clear();

            if (registry == null || registry.Categories == null)
            {
                parentTreeContainer.Add(new Label("No category registry is available."));
                return;
            }

            List<AssetCategoryDefinition> allCategories = new List<AssetCategoryDefinition>(registry.Categories);

            allCategories.RemoveAll(item => item == null);

            if (allCategories.Count == 0)
            {
                parentTreeContainer.Add(new Label("No registered categories to choose from."));
                return;
            }

            var visited = new HashSet<AssetCategoryDefinition>();

            // First show root tags and tags whose parent is missing.
            foreach (AssetCategoryDefinition item in allCategories)
            {
                bool isRoot = item.ParentCategory == null ||
                              !allCategories.Contains(item.ParentCategory);

                if (!isRoot || visited.Contains(item))
                    continue;

                VisualElement node = BuildCategoryNode(item, allCategories, visited);
                if (node != null)
                    parentTreeContainer.Add(node);
            }

            // Also display categories in pre-existing cyclic or malformed trees.
            foreach (AssetCategoryDefinition item in allCategories)
            {
                if (visited.Contains(item))
                    continue;

                var invalidGroup = new Foldout
                {
                    text = "Other categories (check hierarchy)",
                    value = true
                };

                VisualElement node = BuildCategoryNode(item, allCategories, visited);

                if (node != null)
                    invalidGroup.Add(node);

                parentTreeContainer.Add(invalidGroup);
            }
        }

        private VisualElement BuildCategoryNode(
            AssetCategoryDefinition node,
            List<AssetCategoryDefinition> allCategories,
            HashSet<AssetCategoryDefinition> visited)
        {
            if (node == null || !visited.Add(node))
                return null;

            var foldout = new Foldout
            {
                text = node.DisplayName,
                value = false
            };

            bool canSelect = CanSelectAsParent(node);

            var selectButton = new Button(() => SelectParent(node))
            {
                text = ReferenceEquals(node, selectedParentCategory)
                    ? "Current parent"
                    : "Use as parent"
            };

            selectButton.SetEnabled(canSelect);

            if (!canSelect)
            {
                selectButton.tooltip = "A category cannot be its own parent or be parented to one of its descendants.";
            }

            selectButton.style.marginBottom = 4;
            foldout.Add(selectButton);

            foreach (AssetCategoryDefinition child in allCategories)
            {
                if (child == null || child.ParentCategory != node ||
                    visited.Contains(child))
                {
                    continue;
                }

                VisualElement childNode = BuildCategoryNode(child, allCategories, visited);

                if (childNode != null)
                    foldout.Add(childNode);
            }

            return foldout;
        }

        private bool CanSelectAsParent(AssetCategoryDefinition candidate)
        {
            if (candidate == null || candidate == category)
                return false;

            // If candidate is below this tag in the hierarchy, it cannot
            // become this tag's parent.
            var visited = new HashSet<AssetCategoryDefinition>();
            AssetCategoryDefinition current = candidate;

            while (current != null && visited.Add(current))
            {
                if (current == category)
                    return false;

                current = current.ParentCategory;
            }

            return true;
        }

        private void SelectParent(AssetCategoryDefinition parent)
        {
            if (parent != null && !CanSelectAsParent(parent))
                return;

            selectedParentCategory = parent;
            RefreshSelectedParentLabel();
            RefreshParentTree();
        }

        #endregion

        #region Interaction

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return ||
                evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.StopPropagation();
                SaveAndClose();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopPropagation();
                CancelAndClose();
            }
        }

        #endregion

        #region Saving, Closing, Deleting

        private void SaveAndClose()
        {
            if (category == null)
                return;

            serializedCategory.Update();

            SerializedProperty parentProperty = serializedCategory.FindProperty("parentCategory");

            if (parentProperty == null)
            {
                statusLabel.text = "The parentCategory field is missing from AssetCategoryDefinition.";
                return;
            }

            parentProperty.objectReferenceValue = selectedParentCategory;
            serializedCategory.ApplyModifiedProperties();

            if (string.IsNullOrWhiteSpace(category.CategoryId) || string.IsNullOrWhiteSpace(category.DisplayName))
            {
                statusLabel.text = "Category ID and Display Name are required.";
                return;
            }

            if (registry == null)
            {
                statusLabel.text = "No category registry is assigned in the database configuration.";
                return;
            }

            if (selectedParentCategory != null && !CanSelectAsParent(selectedParentCategory))
            {
                statusLabel.text = "That parent would create a hierarchy cycle. Choose another parent.";
                return;
            }

            if (HasDuplicateId(category))
            {
                statusLabel.text = "Another category already uses this ID. Change it to a unique ID.";
                return;
            }

            if (isNewCategory)
            {
                if (!EnsureFolderExists(assetFolder))
                {
                    statusLabel.text = $"Could not create asset folder: {assetFolder}";
                    return;
                }

                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{assetFolder}/{SanitizeFileName(category.DisplayName)}.asset");

                AssetDatabase.CreateAsset(category, assetPath);

                if (!registry.RegisterCategory(category))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    statusLabel.text = "A category with this ID already exists in the registry.";
                    return;
                }
            }
            else
            {
                EditorUtility.SetDirty(category);

                // Also registers an existing asset if it was not registered.
                if (!registry.RegisterCategory(category))
                {
                    statusLabel.text = "A different category with this ID already exists.";
                    return;
                }
            }

            EditorUtility.SetDirty(category);
            registry.RebuildIndex();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onSaved?.Invoke(category);
            Close();
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidCharacter in System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '-');
            }

            return fileName.Trim();
        }

        private static bool EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return true;

            string[] parts = folderPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                return false;

            string currentPath = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = $"{currentPath}/{parts[i]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);

                if (!AssetDatabase.IsValidFolder(nextPath))
                    return false;

                currentPath = nextPath;
            }

            return true;
        }

        private void CancelAndClose()
        {
            if (isNewCategory && category != null)
                DestroyImmediate(category);

            Close();
        }

        private void DeleteCategory()
        {
            if (category == null || isNewCategory || registry == null)
                return;

            // Do not leave child tags pointing at a deleted parent.
            foreach (AssetCategoryDefinition existing in registry.Categories)
            {
                if (existing != null && existing.ParentCategory == category)
                {
                    EditorUtility.DisplayDialog(
                        "Cannot Delete Category",
                        $"'{existing.DisplayName}' uses this category as its parent. " +
                        "Reassign or delete its child categories first.",
                        "OK");
                    return;
                }
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Category",
                $"Delete '{category.DisplayName}'? This cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            string assetPath = AssetDatabase.GetAssetPath(category);

            if (string.IsNullOrEmpty(assetPath) ||
                !AssetDatabase.DeleteAsset(assetPath))
            {
                statusLabel.text = "Failed to delete the category asset. Check the Console.";
                return;
            }

            registry.UnregisterCategory(category);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onSaved?.Invoke(null);
            category = null;
            Close();
        }

        #endregion

        #region ID

        private bool HasDuplicateId(AssetCategoryDefinition candidate)
        {
            foreach (AssetCategoryDefinition existing in registry.Categories)
            {
                if (existing == null || existing == candidate)
                    continue;

                if (string.Equals(
                    existing.CategoryId,
                    candidate.CategoryId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GenerateUniqueTagId(
            string displayName,
            AssetCategoryRegistry registry)
        {
            string slug = MakeSlug(displayName);

            if (string.IsNullOrEmpty(slug))
                slug = "category";

            string id;

            do
            {
                string suffix = Guid.NewGuid().ToString("N");
                id = $"{slug}-{suffix}";
            }
            while (RegistryContainsId(registry, id));

            return id;
        }

        private static bool RegistryContainsId(
            AssetCategoryRegistry registry,
            string id)
        {
            if (registry == null || registry.Categories == null)
                return false;

            foreach (AssetCategoryDefinition item in registry.Categories)
            {
                if (item != null &&
                    string.Equals(
                        item.CategoryId,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string MakeSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var result = new StringBuilder();
            bool separatorPending = false;

            foreach (char character in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (separatorPending && result.Length > 0)
                        result.Append('-');

                    result.Append(character);
                    separatorPending = false;
                }
                else
                {
                    separatorPending = true;
                }
            }

            return result.ToString().Trim('-');
        }

        #endregion

        

        private void OnDestroy()
        {
            if (isNewCategory && category != null &&
                !EditorUtility.IsPersistent(category))
            {
                DestroyImmediate(category);
            }
        }
    }
}