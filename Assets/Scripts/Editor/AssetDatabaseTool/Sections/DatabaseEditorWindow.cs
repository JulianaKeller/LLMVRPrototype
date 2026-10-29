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

        protected void BuildMetadataGenerationUI(VisualElement container)
        {
            metadataController = new MetadataGenerationController();

            metadataController.BuildUI(
                container,
                statusLabel,
                GetMetadataInput,
                ApplyGeneratedMetadata);
        }

        protected abstract MetadataGenerationInput GetMetadataInput();

        private void ApplyGeneratedMetadata(GeneratedMetadata metadata)
        {
            if (metadata == null)
                return;

            GeneratedTaxonomyMetadata taxonMetadata = metadata as GeneratedTaxonomyMetadata;

            if(taxonMetadata != null)
            {
                SerializedObject serializedObject = MetadataSerializedObject;

                if (serializedObject == null)
                    return;

                serializedObject.Update();

                SerializedProperty description = serializedObject.FindProperty("description");

                SerializedProperty synonyms = serializedObject.FindProperty("synonyms");

                if (description != null && string.IsNullOrWhiteSpace(description.stringValue))
                {
                    description.stringValue = metadata.Description;
                }

                if (synonyms != null &&
                    synonyms.isArray &&
                    synonyms.arraySize == 0)
                {
                    synonyms.arraySize = taxonMetadata.Synonyms.Count;

                    for (int i = 0; i < taxonMetadata.Synonyms.Count; i++)
                    {
                        synonyms.GetArrayElementAtIndex(i).stringValue = taxonMetadata.Synonyms[i];
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                //Nothing is applied
            }
        }

        #endregion

        protected virtual void OnDisable()
        {
            metadataController = null;
        }
    }
}
