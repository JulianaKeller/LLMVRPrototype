using LLMVRPrototype.EditorTools;
using System;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public abstract class MetadataEditorWindow<TDefinition> : EditorWindow where TDefinition : ScriptableObject
    {
        protected TDefinition definition;
        protected SerializedObject serializedDefinition;
        protected bool isNewDefinition;
        protected string assetFolder;

        protected Label statusLabel;
        private MetadataGenerationController metadataController;

        protected const string DatabaseFolder = DatabasePaths.DefinitionsFolder;
        protected const string AssetFolder = DatabasePaths.AssetsFolder;

        protected abstract string DefinitionTypeName { get; }
        protected abstract string IdPropertyName { get; }
        protected abstract string DisplayNamePropertyName { get; }
        protected abstract string DescriptionPropertyName { get; }
        protected abstract string DefaultAssetFolder { get; }

        protected abstract string GetDefinitionId(TDefinition item);
        protected abstract string GetDefinitionDisplayName(TDefinition item);

        protected abstract bool HasDuplicateId(TDefinition candidate);
        protected abstract bool RegisterDefinition(TDefinition item);
        protected abstract void UnregisterDefinition(TDefinition item);
        protected abstract void RebuildRegistryIndex();
        protected abstract void MarkRegistryDirty();

        protected abstract void BuildSpecificFields(VisualElement container);

        protected abstract void OnDefinitionSaved(TDefinition item);

        protected Action<TDefinition> onSaved;

        protected SerializedObject MetadataSerializedObject => serializedDefinition;

        protected void InitializeCreate(string displayName, Action<TDefinition> callback)
        {
            definition = CreateInstance<TDefinition>();
            definition.name = displayName;

            isNewDefinition = true;
            assetFolder = DefaultAssetFolder;
            onSaved = callback;

            serializedDefinition = new SerializedObject(definition);

            SerializedProperty nameProperty = serializedDefinition.FindProperty(DisplayNamePropertyName);

            if (nameProperty != null)
                nameProperty.stringValue = displayName;

            SerializedProperty idProperty = serializedDefinition.FindProperty(IdPropertyName);

            if (idProperty != null)
                idProperty.stringValue = GenerateUniqueId(displayName);

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        }

        protected void InitializeEdit(TDefinition existingDefinition, Action<TDefinition> callback)
        {
            definition = existingDefinition;
            isNewDefinition = false;
            assetFolder = DefaultAssetFolder;
            onSaved = callback;

            serializedDefinition = new SerializedObject(definition);
        }

        protected void ShowEditor(string title, bool autostartMetadataGeneration = true)
        {
            titleContent = new GUIContent(title);
            ShowUtility();

            minSize = new Vector2(420, 360);
            maxSize = new Vector2(700, 800);

            RebuildUI(autostartMetadataGeneration);
        }

        protected virtual void RebuildUI(bool autostartMetadataGeneration = true)
        {
            rootVisualElement.Clear();

            if (definition == null)
            {
                rootVisualElement.Add(new Label(
                    $"No {DefinitionTypeName.ToLowerInvariant()} selected."));
                return;
            }

            serializedDefinition = new SerializedObject(definition);

            var container = new ScrollView();
            container.style.paddingLeft = 12;
            container.style.paddingRight = 12;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;

            rootVisualElement.Add(container);

            var heading = new Label(
                $"{(isNewDefinition ? "Create" : "Edit")} {DefinitionTypeName}");

            heading.style.fontSize = 16;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 10;

            container.Add(heading);

            AddProperty(container, IdPropertyName, $"{DefinitionTypeName} ID");
            AddProperty(container, DisplayNamePropertyName, "Display Name");
            AddProperty(container, DescriptionPropertyName, "Description");

            BuildSpecificFields(container);

            statusLabel = new Label("Press Enter to save. Press Escape to cancel.");

            statusLabel.style.marginTop = 10;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            container.Add(statusLabel);

            var saveButton = new Button(SaveAndClose)
            {
                text = $"Save {DefinitionTypeName}"
            };

            saveButton.style.marginTop = 8;
            container.Add(saveButton);

            var cancelButton = new Button(CancelAndClose)
            {
                text = "Cancel"
            };

            cancelButton.style.marginTop = 4;
            container.Add(cancelButton);

            if (!isNewDefinition)
            {
                var deleteButton = new Button(DeleteDefinition)
                {
                    text = $"Delete {DefinitionTypeName}"
                };

                deleteButton.style.marginTop = 12;
                deleteButton.style.color = new Color(0.8f, 0.2f, 0.2f);
                container.Add(deleteButton);
            }

            if (isNewDefinition)
                BuildMetadataGenerationUI(container, autostartMetadataGeneration);

            rootVisualElement.RegisterCallback<KeyDownEvent>(
                OnKeyDown,
                TrickleDown.TrickleDown);
        }

        protected void AddProperty(VisualElement parent, string propertyName, string label)
        {
            SerializedProperty property =
                serializedDefinition.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogWarning(
                    $"{DefinitionTypeName} property '{propertyName}' was not found.");
                return;
            }

            var field = new PropertyField(property, label);
            field.Bind(serializedDefinition);
            field.style.marginBottom = 6;

            parent.Add(field);
        }


        #region LLM API Generation call

        protected void BuildMetadataGenerationUI(VisualElement container, bool autostartMetadataGeneration = true)
        {
            metadataController = new MetadataGenerationController();

            metadataController.BuildUI(
                container,
                statusLabel,
                GetMetadataInput,
                ApplyGeneratedMetadata,
                autostartMetadataGeneration);
        }

        protected abstract MetadataGenerationInput GetMetadataInput();

        protected virtual void OnGeneratedMetadataApplied(GeneratedMetadata metadata)
        {
        }

        protected void ApplyGeneratedMetadata(GeneratedMetadata metadata)
        {
            if (metadata == null)
                return;

            SerializedObject serializedObject = MetadataSerializedObject;

            if (serializedObject == null)
                return;

            serializedObject.Update();

            bool changed = false;

            SerializedProperty description = serializedObject.FindProperty("description");

            if (description != null &&
                !string.IsNullOrWhiteSpace(metadata.Description) &&
                !string.Equals(description.stringValue, metadata.Description, StringComparison.Ordinal))
            {
                description.stringValue = metadata.Description;
                changed = true;
            }

            if (metadata is GeneratedTaxonomyMetadata taxonomyMetadata)
            {
                SerializedProperty synonyms =
                    serializedObject.FindProperty("synonyms");

                if (synonyms != null &&
                    synonyms.isArray &&
                    synonyms.arraySize == 0)
                {
                    synonyms.arraySize = taxonomyMetadata.Synonyms.Count;

                    for (int i = 0; i < taxonomyMetadata.Synonyms.Count; i++)
                    {
                        synonyms.GetArrayElementAtIndex(i).stringValue =
                            taxonomyMetadata.Synonyms[i];
                    }

                    changed = true;
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedProperties();

            }
            OnGeneratedMetadataApplied(metadata);
        }

        #endregion

        protected virtual bool ValidateDefinition()
        {
            if (string.IsNullOrWhiteSpace(GetDefinitionId(definition)) ||
                string.IsNullOrWhiteSpace(GetDefinitionDisplayName(definition)))
            {
                statusLabel.text =
                    $"{DefinitionTypeName} ID and Display Name are required.";
                return false;
            }

            if (HasDuplicateId(definition))
            {
                statusLabel.text =
                    $"Another {DefinitionTypeName.ToLowerInvariant()} already uses this ID.";
                return false;
            }

            return true;
        }

        protected virtual void SaveAndClose()
        {
            if (definition == null)
                return;

            serializedDefinition.ApplyModifiedProperties();

            serializedDefinition.Update();

            if (!ValidateDefinition())
                return;

            if (isNewDefinition)
            {
                if (!EnsureFolderExists(assetFolder))
                {
                    statusLabel.text =
                        $"Could not create asset folder: {assetFolder}";
                    return;
                }

                string fileName = SanitizeFileName(
                    GetDefinitionDisplayName(definition));

                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{assetFolder}/{fileName}.asset");

                AssetDatabase.CreateAsset(definition, assetPath);

                if (!RegisterDefinition(definition))
                {
                    AssetDatabase.DeleteAsset(assetPath);

                    statusLabel.text =
                        $"Could not register this {DefinitionTypeName.ToLowerInvariant()}.";
                    return;
                }
            }
            else
            {
                if (!RegisterDefinition(definition))
                {
                    statusLabel.text =
                        $"A different {DefinitionTypeName.ToLowerInvariant()} with this ID already exists.";
                    return;
                }
            }

            EditorUtility.SetDirty(definition);

            RebuildRegistryIndex();
            MarkRegistryDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            OnDefinitionSaved(definition);
            onSaved?.Invoke(definition);

            Close();
        }

        protected virtual void CancelAndClose()
        {
            if (isNewDefinition && definition != null)
                DestroyImmediate(definition);

            definition = null;
            Close();
        }

        protected virtual void DeleteDefinition()
        {
            if (definition == null || isNewDefinition)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                $"Delete {DefinitionTypeName}",
                $"Delete '{GetDefinitionDisplayName(definition)}'? This cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            string assetPath = AssetDatabase.GetAssetPath(definition);

            if (string.IsNullOrEmpty(assetPath) ||
                !AssetDatabase.DeleteAsset(assetPath))
            {
                statusLabel.text =
                    $"Failed to delete the {DefinitionTypeName.ToLowerInvariant()} asset.";
                return;
            }

            UnregisterDefinition(definition);
            MarkRegistryDirty();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            onSaved?.Invoke(null);

            definition = null;
            Close();
        }

        protected virtual void OnKeyDown(KeyDownEvent evt)
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

        protected string GenerateUniqueId(string displayName)
        {
            string slug = MakeSlug(displayName);

            if (string.IsNullOrEmpty(slug))
                slug = DefinitionTypeName.ToLowerInvariant();

            string id;

            do
            {
                id = $"{slug}-{Guid.NewGuid():N}";
            }
            while (RegistryContainsId(id));

            return id;
        }

        private bool RegistryContainsId(string id)
        {
            // HasDuplicateId expects a definition, so ID lookup is delegated
            // to the concrete registry implementation.
            return HasId(id);
        }

        protected abstract bool HasId(string id);

        protected static string MakeSlug(string value)
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

        private static string SanitizeFileName(string fileName)
        {
            foreach (char invalidCharacter in
                     System.IO.Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '-');
            }

            return fileName.Trim();
        }

        protected static bool EnsureFolderExists(string folderPath)
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

        protected virtual void OnDestroy()
        {
            metadataController = null;

            if (isNewDefinition &&
                definition != null &&
                !EditorUtility.IsPersistent(definition))
            {
                DestroyImmediate(definition);
            }
        }

        protected virtual void OnDisable()
        {
            metadataController = null;
        }
    }
}
