using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LLMVRPrototype.Assets;
using LLMVRPrototype.Assets.Assets;

namespace LLMVRPrototype.EditorTools
{
    public sealed class AssetImportPreparationController
    {
        private readonly AssetImportSession session;

        public AssetImportPreparationController(AssetImportSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public bool Prepare(out string errorMessage)
        {
            errorMessage = null;

            if (session.IsDisposed)
            {
                errorMessage = "The asset import session has already been disposed.";
                return false;
            }

            if (session.Items.Count == 0)
            {
                errorMessage = "There are no assets to prepare.";
                return false;
            }

            if (!ValidateCandidates(out errorMessage))
                return false;

            // Remove any previously generated temporary definitions.
            session.ClearTemporaryObjects();

            try
            {
                foreach (AssetImportItem item in session.Items)
                {
                    AssetDefinition definition = CreateDefinition(item.Candidate);

                    if (definition == null)
                        throw new InvalidOperationException($"Failed to create a definition for '{item.Candidate.AssetPath}'."
                        );

                    item.SetDefinition(definition);
                }
            }
            catch (Exception exception)
            {
                session.ClearTemporaryObjects();
                errorMessage = $"Asset preparation failed: {exception.Message}";
                Debug.LogException(exception);
                return false;
            }

            return true;
        }

        private bool ValidateCandidates(out string errorMessage)
        {
            errorMessage = null;

            foreach (AssetImportItem item in session.Items)
            {
                if (item == null)
                {
                    errorMessage = "The import session contains a null candidate.";
                    return false;
                }

                if (item.Candidate.DefinitionType == null ||
                    item.Candidate.DefinitionType.IsAbstract ||
                    !typeof(AssetDefinition).IsAssignableFrom(item.Candidate.DefinitionType))
                {
                    errorMessage = $"'{item.Candidate.DisplayName}' has an invalid definition type.";
                    return false;
                }

                UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(item.Candidate.AssetPath);

                if (sourceAsset == null)
                {
                    errorMessage = $"Could not load source asset: {item.Candidate.AssetPath}";
                    return false;
                }

                if (!IsCompatible(item.Candidate.DefinitionType, sourceAsset))
                {
                    errorMessage =
                        $"'{item.Candidate.DisplayName}' cannot use definition type " +
                        $"'{AssetDefinitionTypeResolver.GetDisplayName(item.Candidate.DefinitionType)}'. " +
                        $"The selected type is incompatible with source type '{sourceAsset.GetType().Name}'.";

                    return false;
                }
            }

            return true;
        }

        private AssetDefinition CreateDefinition(AssetImportCandidate candidate)
        {
            UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(candidate.AssetPath);

            if (sourceAsset == null)
                throw new InvalidOperationException($"Could not load '{candidate.AssetPath}'.");

            AssetDefinition definition = ScriptableObject.CreateInstance(candidate.DefinitionType) as AssetDefinition;

            if (definition == null)
                throw new InvalidOperationException($"Could not instantiate {candidate.DefinitionType.Name}.");

            definition.name = candidate.DisplayName;

            SerializedObject serializedDefinition = new SerializedObject(definition);

            SetStringProperty(serializedDefinition, "assetId", Guid.NewGuid().ToString("N"));

            SetStringProperty(serializedDefinition, "displayName", candidate.PreliminaryName.Trim());
            SetStringProperty(serializedDefinition, "description", candidate.PreliminaryDescription ?? string.Empty);

            AssignSourceReference(serializedDefinition, candidate.DefinitionType, sourceAsset);
            AssignThumbnail(definition, candidate);

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        private void AssignSourceReference(
            SerializedObject serializedDefinition,
            Type definitionType,
            UnityEngine.Object sourceAsset)
        {
            if (definitionType == typeof(ObjectAssetDefinition))
                SetObjectProperty(serializedDefinition, "prefab", sourceAsset);
            else if (definitionType == typeof(ParticleSystemAssetDefinition))
                SetObjectProperty(serializedDefinition, "prefab", sourceAsset);
            else if (definitionType == typeof(LightAssetDefinition))
                SetObjectProperty(serializedDefinition, "prefab", sourceAsset);
            else if (definitionType == typeof(TextureAssetDefinition))
                SetObjectProperty(serializedDefinition, "texture", sourceAsset);
            else if (definitionType == typeof(MaterialAssetDefinition))
                SetObjectProperty(serializedDefinition, "material", sourceAsset);
            else if (definitionType == typeof(AudioAssetDefinition))
                SetObjectProperty(serializedDefinition, "audioClip", sourceAsset);
            else if (definitionType == typeof(GenericAssetDefinition))
                SetObjectProperty(serializedDefinition, "sourceAsset", sourceAsset);
            else
                throw new InvalidOperationException($"No source assignment rule exists for {definitionType.Name}.");
        }

        private void AssignThumbnail(AssetDefinition definition, AssetImportCandidate candidate)
        {
            UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(candidate.AssetPath);
            if (sourceAsset == null)
                return;

            Texture2D previewTexture = candidate.PreviewTexture;

            if (previewTexture == null)
                previewTexture = AssetPreview.GetAssetPreview(sourceAsset);

            if (previewTexture == null)
                previewTexture = AssetPreview.GetMiniThumbnail(sourceAsset) as Texture2D;

            if (previewTexture == null)
                return;

            Sprite sprite = Sprite.Create(
                previewTexture,
                new Rect(0, 0, previewTexture.width, previewTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            session.AddGeneratedSprite(sprite);

            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty thumbnailProperty = serializedDefinition.FindProperty("thumbnail");

            if (thumbnailProperty == null)
            {
                session.RemoveGeneratedSprite(sprite);
                throw new InvalidOperationException(
                    $"The definition type '{definition.GetType().Name}' does not contain a serialized 'thumbnail' field."
                );
            }

            thumbnailProperty.objectReferenceValue = sprite;
            serializedDefinition.ApplyModifiedProperties();
        }

        private bool IsCompatible(Type definitionType, UnityEngine.Object sourceAsset)
        {
            if (definitionType == typeof(ObjectAssetDefinition) ||
                definitionType == typeof(ParticleSystemAssetDefinition) ||
                definitionType == typeof(LightAssetDefinition))
            {
                if (sourceAsset is not GameObject gameObject)
                    return false;

                if (definitionType == typeof(ParticleSystemAssetDefinition))
                    return gameObject.GetComponentInChildren<ParticleSystem>(true) != null;

                if (definitionType == typeof(LightAssetDefinition))
                    return gameObject.GetComponentInChildren<Light>(true) != null;

                return true;
            }

            if (definitionType == typeof(TextureAssetDefinition))
                return sourceAsset is Texture;

            if (definitionType == typeof(MaterialAssetDefinition))
                return sourceAsset is Material;

            if (definitionType == typeof(AudioAssetDefinition))
                return sourceAsset is AudioClip;

            if (definitionType == typeof(GenericAssetDefinition))
                return true;

            return false;
        }

        private void SetStringProperty(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
                throw new InvalidOperationException($"Serialized string property '{propertyName}' was not found on {serializedObject.targetObject.GetType().Name}.");

            property.stringValue = value;
        }

        private void SetObjectProperty(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
                throw new InvalidOperationException($"Serialized object property '{propertyName}' was not found on {serializedObject.targetObject.GetType().Name}.");

            property.objectReferenceValue = value;
        }
    }
}