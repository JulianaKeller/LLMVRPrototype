using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public abstract class DatabaseEditorWindow : EditorWindow
    {
        protected Label statusLabel;
        protected abstract SerializedObject MetadataSerializedObject { get; }

        private MetadataGenerationController metadataController;

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

        private void ApplyGeneratedMetadata(GeneratedMetadata metadata)
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

        protected virtual void OnGeneratedMetadataApplied(
            GeneratedMetadata metadata)
        {
        }

        #endregion

        protected virtual void OnDisable()
        {
            metadataController = null;
        }
    }
}
