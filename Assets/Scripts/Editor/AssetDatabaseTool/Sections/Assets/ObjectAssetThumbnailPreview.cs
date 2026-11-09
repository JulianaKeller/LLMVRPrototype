using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    /// <summary>
    /// Creates a thumbnail from an ObjectAssetDefinition prefab,
    /// applying its ImportRotation before rendering.
    /// </summary>
    public sealed class ObjectAssetThumbnailPreview : IDisposable
    {
        private const int ThumbnailResolution = 512;

        private readonly ObjectAssetDefinition definition;
        private readonly SerializedObject serializedObject;
        private readonly Image previewImage;
        private readonly Action<Sprite, Texture2D, Sprite, Texture2D> replaceGeneratedThumbnail;

        private PreviewRenderUtility previewUtility;
        private GameObject previewInstance;

        private Sprite generatedSprite;
        private Texture2D generatedTexture;

        private bool disposed;

        private static readonly HashSet<ObjectAssetThumbnailPreview> ActivePreviews = new();

        static ObjectAssetThumbnailPreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanupAll;
            EditorApplication.quitting += CleanupAll;
        }

        public ObjectAssetThumbnailPreview(
            ObjectAssetDefinition definition,
            SerializedObject serializedObject,
            Image previewImage,
            Action<Sprite, Texture2D, Sprite, Texture2D> registerGeneratedSprite = null)
        {
            this.definition = definition ??
                throw new ArgumentNullException(nameof(definition));

            this.serializedObject = serializedObject ??
                throw new ArgumentNullException(nameof(serializedObject));

            this.previewImage = previewImage ??
                throw new ArgumentNullException(nameof(previewImage));

            this.replaceGeneratedThumbnail = registerGeneratedSprite;

            try
            {
                InitializePreview();

                // Register immediately after creating the rendering utility.
                ActivePreviews.Add(this);

                RegenerateThumbnail();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void InitializePreview()
        {
            previewUtility = new PreviewRenderUtility();

            previewUtility.cameraFieldOfView = 30f;

            previewUtility.camera.clearFlags = CameraClearFlags.Color;
            previewUtility.camera.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            previewUtility.camera.nearClipPlane = 0.01f;
            previewUtility.camera.farClipPlane = 1000f;

            previewUtility.lights[0].intensity = 1.2f;
            previewUtility.lights[0].transform.rotation =
                Quaternion.Euler(30f, 30f, 0f);

            previewUtility.lights[1].intensity = 1f;
        }

        public void RegenerateThumbnail()
        {
            if (disposed)
                return;

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            SerializedProperty prefabProperty =
                serializedObject.FindProperty("prefab");

            if (prefabProperty == null ||
                prefabProperty.objectReferenceValue == null)
                return;

            GameObject prefab =
                prefabProperty.objectReferenceValue as GameObject;

            if (prefab == null)
                return;

            ClearPreviewInstance();

            previewInstance = previewUtility.InstantiatePrefabInScene(prefab);

            if (previewInstance == null)
                return;

            // Apply the user's import correction.
            previewInstance.transform.rotation = Quaternion.Euler(definition.ImportRotation);

            // Determine the object's bounds.
            Bounds bounds = CalculateBounds(previewInstance);

            PositionCamera(bounds);

            RenderThumbnail();

            // Store the generated Sprite in the definition.
            SerializedProperty thumbnailProperty =
                serializedObject.FindProperty("thumbnail");

            if (thumbnailProperty != null)
            {
                thumbnailProperty.objectReferenceValue = generatedSprite;

                serializedObject.ApplyModifiedProperties();
            }

            previewImage.image = generatedTexture;
        }

        private Bounds CalculateBounds(GameObject instance)
        {
            Renderer[] renderers =
                instance.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
                return new Bounds(instance.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        private void PositionCamera(Bounds bounds)
        {
            Camera camera = previewUtility.camera;

            Vector3 center = bounds.center;

            float radius = Mathf.Max(bounds.extents.magnitude, 0.1f);

            float distance =
                radius / Mathf.Sin(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            distance *= 1.25f;

            Vector3 direction =
                new Vector3(1f, 0.7f, -1f).normalized;

            camera.transform.position =
                center + direction * distance;

            camera.transform.LookAt(center);

            camera.orthographic = false;
        }

        private void RenderThumbnail()
        {
            Sprite previousSprite = generatedSprite;
            Texture2D previousTexture = generatedTexture;


            Rect previewRect = new Rect(0, 0, ThumbnailResolution, ThumbnailResolution);

            previewUtility.BeginPreview(previewRect, GUIStyle.none);

            Texture renderedTexture;

            try
            {
                previewUtility.camera.Render();
            }
            finally
            {
                renderedTexture = previewUtility.EndPreview();
            }

            if (renderedTexture == null)
                return;

            RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary(
                ThumbnailResolution,
                ThumbnailResolution,
                24,
                RenderTextureFormat.ARGB32);

            RenderTexture previousActive = RenderTexture.active;

            try
            {
                Graphics.Blit(renderedTexture, temporaryRenderTexture);
                RenderTexture.active = temporaryRenderTexture;

                generatedTexture = new Texture2D(
                    ThumbnailResolution,
                    ThumbnailResolution,
                    TextureFormat.RGBA32,
                    false);

                generatedTexture.ReadPixels(
                    new Rect(0, 0, ThumbnailResolution, ThumbnailResolution),
                    0,
                    0);

                generatedTexture.Apply();

                generatedSprite = Sprite.Create(
                    generatedTexture,
                    new Rect(0, 0, ThumbnailResolution, ThumbnailResolution),
                    new Vector2(0.5f, 0.5f));

                replaceGeneratedThumbnail?.Invoke(
                        previousSprite,
                        previousTexture,
                        generatedSprite,
                        generatedTexture);
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(temporaryRenderTexture);
            }
        }

        private void ClearPreviewInstance()
        {
            if (previewInstance == null)
                return;

            UnityEngine.Object.DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        private static void CleanupAll()
        {
            ObjectAssetThumbnailPreview[] previews = new ObjectAssetThumbnailPreview[ActivePreviews.Count];

            ActivePreviews.CopyTo(previews);

            foreach (ObjectAssetThumbnailPreview preview in previews)
                preview.Dispose();

            ActivePreviews.Clear();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            ActivePreviews.Remove(this);

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }

            previewInstance = null;

            // Generated sprites and textures are owned by the import session
        }
    }
}