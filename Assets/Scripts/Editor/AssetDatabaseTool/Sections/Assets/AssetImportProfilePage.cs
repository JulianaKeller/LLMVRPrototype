using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
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

                // Object rotation gets its own control because changing it must regenerate the thumbnail.
                if (iterator.propertyPath == "importRotation" && definition is ObjectAssetDefinition)
                {
                    BuildImportRotationField(iterator.Copy());
                    continue;
                }

                SerializedProperty property = iterator.Copy();

                // PropertyField automatically selects an appropriate editor control based on the property's serialized type
                PropertyField propertyField = new PropertyField(property);
                propertyField.Bind(serializedObject);

                propertyContainer.Add(propertyField);
            }
        }

        private void BuildImportRotationField(SerializedProperty rotationProperty)
        {
            importRotationField = new Vector3Field("Import Rotation");
            importRotationField.value = rotationProperty.vector3Value;

            importRotationField.RegisterValueChangedCallback(evt =>
            {
                SetImportRotation(evt.newValue);
            });

            importRotationField.style.marginBottom = 8;
            propertyContainer.Add(importRotationField);

            Button resetButton = new Button(() =>
            {
                SetImportRotation(Vector3.zero);
            })
            {
                text = "Reset Rotation"
            };

            propertyContainer.Add(resetButton);
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
