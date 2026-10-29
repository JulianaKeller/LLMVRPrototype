using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    // This popup edits the ScriptableObject through Unity's SerializedObject
    public class TagEditorWindow : DatabaseEditorWindow
    {
        private const string DefaultTagFolder = "Assets/EnvironmentLibrary/Definitions/Tags";

        private AssetTagDefinition tag;
        private SerializedObject serializedTag;
        private AssetTagRegistry registry;
        private bool isNewTag;
        private string assetFolder;
        private Action<AssetTagDefinition> onSaved;

        private AssetTagDefinition selectedParentTag;
        private Label selectedParentLabel;
        private VisualElement parentTreeContainer;

        protected override SerializedObject MetadataSerializedObject => serializedTag;



        #region UI Methods

        public static void OpenForCreate(
            AssetTagRegistry registry,
            string displayName,
            Action<AssetTagDefinition> onSaved = null)
        {
            var window = CreateInstance<TagEditorWindow>();

            window.titleContent = new GUIContent("Create Tag");
            window.registry = registry;
            window.tag = CreateInstance<AssetTagDefinition>();
            window.isNewTag = true;
            window.assetFolder = DefaultTagFolder;
            window.onSaved = onSaved;
            window.selectedParentTag = null;

            window.tag.name = displayName;

            SerializedObject serialized = new SerializedObject(window.tag);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("id").stringValue = GenerateUniqueTagId(displayName, registry);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            window.ShowUtility();
            window.minSize = new Vector2(420, 360);
            window.maxSize = new Vector2(700, 800);
            window.RebuildUI();
        }

        public static void OpenForEdit(
            AssetTagDefinition existingTag,
            AssetTagRegistry registry,
            Action<AssetTagDefinition> onSaved = null)
        {
            if (existingTag == null)
                return;

            var window = CreateInstance<TagEditorWindow>();

            window.titleContent = new GUIContent("Edit Tag");
            window.registry = registry;
            window.tag = existingTag;
            window.isNewTag = false;
            window.assetFolder = DefaultTagFolder;
            window.onSaved = onSaved;
            window.selectedParentTag = existingTag.ParentTag;

            window.ShowUtility();
            window.minSize = new Vector2(420, 360);
            window.maxSize = new Vector2(700, 800);
            window.RebuildUI();
        }

        private void RebuildUI()
        {
            rootVisualElement.Clear();

            if (tag == null)
            {
                rootVisualElement.Add(new Label("No tag selected."));
                return;
            }

            serializedTag = new SerializedObject(tag);

            var container = new ScrollView();
            container.style.paddingLeft = 12;
            container.style.paddingRight = 12;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;
            rootVisualElement.Add(container);

            var heading = new Label(isNewTag ? "Create Tag" : "Edit Tag");
            heading.style.fontSize = 16;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 10;
            container.Add(heading);

            AddProperty(container, "id", "Tag ID");
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
                text = "Save Tag"
            };
            saveButton.style.marginTop = 8;
            container.Add(saveButton);

            var cancelButton = new Button(CancelAndClose)
            {
                text = "Cancel"
            };
            cancelButton.style.marginTop = 4;
            container.Add(cancelButton);

            if (!isNewTag)
            {
                var deleteButton = new Button(DeleteTag)
                {
                    text = "Delete Tag"
                };

                deleteButton.style.marginTop = 12;
                deleteButton.style.color = new Color(0.8f, 0.2f, 0.2f);
                container.Add(deleteButton);
            }

            if (isNewTag)
            {
                BuildMetadataGenerationUI(container);
            }

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private void AddProperty(
            VisualElement parent,
            string propertyName,
            string label)
        {
            SerializedProperty property = serializedTag.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogWarning($"Tag property '{propertyName}' was not found.");
                return;
            }

            var field = new PropertyField(property, label);
            field.Bind(serializedTag);
            field.style.marginBottom = 6;
            parent.Add(field);
        }

#endregion

        #region LLM API call

        protected override MetadataGenerationInput GetMetadataInput()
        {
            return new TagMetadataGenerationInput(tag.DisplayName,
                tag.Description,
                selectedParentTag?.DisplayName,
                selectedParentTag?.Description,
                3, 8
            );
        }

        #endregion

        #region Parent Selector

        private void BuildParentSelector(VisualElement container)
        {
            var section = new Foldout
            {
                text = "Parent Tag",
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
                text = "None (root tag)"
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
            selectedParentLabel.text = selectedParentTag == null
                ? "Current parent: None (root tag)"
                : $"Current parent: {selectedParentTag.DisplayName}";
        }

        private void RefreshParentTree()
        {
            if (parentTreeContainer == null)
                return;

            parentTreeContainer.Clear();

            if (registry == null || registry.Tags == null)
            {
                parentTreeContainer.Add(
                    new Label("No tag registry is available."));
                return;
            }

            List<AssetTagDefinition> allTags =
                new List<AssetTagDefinition>(registry.Tags);

            allTags.RemoveAll(item => item == null);

            if (allTags.Count == 0)
            {
                parentTreeContainer.Add(
                    new Label("No registered tags to choose from."));
                return;
            }

            var visited = new HashSet<AssetTagDefinition>();

            // First show root tags and tags whose parent is missing.
            foreach (AssetTagDefinition item in allTags)
            {
                bool isRoot = item.ParentTag == null ||
                              !allTags.Contains(item.ParentTag);

                if (!isRoot || visited.Contains(item))
                    continue;

                VisualElement node = BuildTagNode(item, allTags, visited);
                if (node != null)
                    parentTreeContainer.Add(node);
            }

            // Also display tags in pre-existing cyclic or malformed trees.
            foreach (AssetTagDefinition item in allTags)
            {
                if (visited.Contains(item))
                    continue;

                var invalidGroup = new Foldout
                {
                    text = "Other tags (check hierarchy)",
                    value = true
                };

                VisualElement node =
                    BuildTagNode(item, allTags, visited);

                if (node != null)
                    invalidGroup.Add(node);

                parentTreeContainer.Add(invalidGroup);
            }
        }

        private VisualElement BuildTagNode(
            AssetTagDefinition node,
            List<AssetTagDefinition> allTags,
            HashSet<AssetTagDefinition> visited)
        {
            if (node == null || !visited.Add(node))
                return null;

            var foldout = new Foldout
            {
                text = node.DisplayName,
                value = false
            };

            bool canSelect = CanSelectAsParent(node);

            var selectButton = new Button(
                () => SelectParent(node))
            {
                text = ReferenceEquals(node, selectedParentTag)
                    ? "Current parent"
                    : "Use as parent"
            };

            selectButton.SetEnabled(canSelect);

            if (!canSelect)
            {
                selectButton.tooltip =
                    "A tag cannot be its own parent or be parented to one of its descendants.";
            }

            selectButton.style.marginBottom = 4;
            foldout.Add(selectButton);

            foreach (AssetTagDefinition child in allTags)
            {
                if (child == null || child.ParentTag != node ||
                    visited.Contains(child))
                {
                    continue;
                }

                VisualElement childNode =
                    BuildTagNode(child, allTags, visited);

                if (childNode != null)
                    foldout.Add(childNode);
            }

            return foldout;
        }

        private bool CanSelectAsParent(AssetTagDefinition candidate)
        {
            if (candidate == null || candidate == tag)
                return false;

            // If candidate is below this tag in the hierarchy, it cannot
            // become this tag's parent.
            var visited = new HashSet<AssetTagDefinition>();
            AssetTagDefinition current = candidate;

            while (current != null && visited.Add(current))
            {
                if (current == tag)
                    return false;

                current = current.ParentTag;
            }

            return true;
        }

        private void SelectParent(AssetTagDefinition parent)
        {
            if (parent != null && !CanSelectAsParent(parent))
                return;

            selectedParentTag = parent;
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
            if (tag == null)
                return;

            serializedTag.Update();

            SerializedProperty parentProperty = serializedTag.FindProperty("parentTag");

            if (parentProperty == null)
            {
                statusLabel.text = "The parentTag field is missing from AssetTagDefinition.";
                return;
            }

            parentProperty.objectReferenceValue = selectedParentTag;
            serializedTag.ApplyModifiedProperties();

            if (string.IsNullOrWhiteSpace(tag.TagId) || string.IsNullOrWhiteSpace(tag.DisplayName))
            {
                statusLabel.text = "Tag ID and Display Name are required.";
                return;
            }

            if (registry == null)
            {
                statusLabel.text = "No tag registry is assigned in the database configuration.";
                return;
            }

            if (selectedParentTag != null && !CanSelectAsParent(selectedParentTag))
            {
                statusLabel.text = "That parent would create a hierarchy cycle. Choose another parent.";
                return;
            }

            if (HasDuplicateId(tag))
            {
                statusLabel.text = "Another tag already uses this ID. Change it to a unique ID.";
                return;
            }

            if (isNewTag)
            {
                if (!EnsureFolderExists(assetFolder))
                {
                    statusLabel.text = $"Could not create asset folder: {assetFolder}";
                    return;
                }

                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{assetFolder}/{SanitizeFileName(tag.DisplayName)}.asset");

                AssetDatabase.CreateAsset(tag, assetPath);

                if (!registry.RegisterTag(tag))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    statusLabel.text = "A tag with this ID already exists in the registry.";
                    return;
                }
            }
            else
            {
                EditorUtility.SetDirty(tag);

                // Also registers an existing asset if it was not registered.
                if (!registry.RegisterTag(tag))
                {
                    statusLabel.text = "A different tag with this ID already exists.";
                    return;
                }
            }

            EditorUtility.SetDirty(tag);
            registry.RebuildIndex();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onSaved?.Invoke(tag);
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
            if (isNewTag && tag != null)
                DestroyImmediate(tag);

            Close();
        }

        private void DeleteTag()
        {
            if (tag == null || isNewTag || registry == null)
                return;

            // Do not leave child tags pointing at a deleted parent.
            foreach (AssetTagDefinition existing in registry.Tags)
            {
                if (existing != null && existing.ParentTag == tag)
                {
                    EditorUtility.DisplayDialog(
                        "Cannot Delete Tag",
                        $"'{existing.DisplayName}' uses this tag as its parent. " +
                        "Reassign or delete its child tags first.",
                        "OK");
                    return;
                }
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Tag",
                $"Delete '{tag.DisplayName}'? This cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            string assetPath = AssetDatabase.GetAssetPath(tag);

            if (string.IsNullOrEmpty(assetPath) ||
                !AssetDatabase.DeleteAsset(assetPath))
            {
                statusLabel.text = "Failed to delete the tag asset. Check the Console.";
                return;
            }

            registry.UnregisterTag(tag);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onSaved?.Invoke(null);
            tag = null;
            Close();
        }

        #endregion

        #region ID

        private bool HasDuplicateId(AssetTagDefinition candidate)
        {
            foreach (AssetTagDefinition existing in registry.Tags)
            {
                if (existing == null || existing == candidate)
                    continue;

                if (string.Equals(
                    existing.TagId,
                    candidate.TagId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GenerateUniqueTagId(
            string displayName,
            AssetTagRegistry registry)
        {
            string slug = MakeSlug(displayName);

            if (string.IsNullOrEmpty(slug))
                slug = "tag";

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
            AssetTagRegistry registry,
            string id)
        {
            if (registry == null || registry.Tags == null)
                return false;

            foreach (AssetTagDefinition item in registry.Tags)
            {
                if (item != null &&
                    string.Equals(
                        item.TagId,
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
            if (isNewTag && tag != null &&
                !EditorUtility.IsPersistent(tag))
            {
                DestroyImmediate(tag);
            }
        }

    }
}
