using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LLMVRPrototype
{
    /// <summary>
    /// Moves source assets, creates folders and saves definitions.
    /// </summary>
    public sealed class AssetImportPersistence
    {
        private const string LibraryFolder = DatabasePaths.AssetsFolder;
        private const string DefinitionsFolder = DatabasePaths.DefinitionsFolder;


        public AssetImportPersistence()
        {
            
        }

        public bool SaveItem(AssetImportSession session, AssetImportItem item, out string errorMessage)
        {
            errorMessage = null;

            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (session.IsDisposed)
            {
                errorMessage = "The import session has already been disposed.";
                return false;
            }

            if (item.IsSaved || item.IsDiscarded)
            {
                errorMessage = "This import item has already been completed.";
                return false;
            }

            if (item.Definition == null)
            {
                errorMessage = "The import item does not have a definition.";
                return false;
            }

            string sourcePath = item.Candidate.AssetPath;

            if (string.IsNullOrWhiteSpace(sourcePath) || !sourcePath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                errorMessage = $"Invalid source asset path: {sourcePath}";
                return false;
            }

            if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            {
                errorMessage = $"Source asset could not be found: {sourcePath}";
                return false;
            }

            string originalSourcePath = sourcePath;
            string definitionPath = null;
            bool definitionCreated = false;

            try
            {
                AssetDefinition definition = item.Definition;

                // Resolve and create the destination folders.
                string sourceDestinationFolder = GetSourceFolder(definition);

                string definitionDestinationFolder = CombineAssetPath(DefinitionsFolder, GetDefinitionFolderName(definition));

                EnsureFolderExists(sourceDestinationFolder);
                EnsureFolderExists(definitionDestinationFolder);

                // Generate unique destination paths.
                string sourceDestinationPath = AssetDatabase.GenerateUniqueAssetPath(CombineAssetPath(sourceDestinationFolder, Path.GetFileName(sourcePath)));

                string definitionFileName = SanitizeFileName(string.IsNullOrWhiteSpace(definition.DisplayName)
                        ? definition.AssetId
                        : definition.DisplayName);

                definitionPath = AssetDatabase.GenerateUniqueAssetPath(CombineAssetPath(definitionDestinationFolder, definitionFileName + ".asset"));

                // Save the definition first. Its references to the source asset remain valid because moving an asset preserves its GUID.
                AssetDatabase.CreateAsset(definition, definitionPath);
                definitionCreated = true;

                // Save generated thumbnail resources as subassets.
                SaveThumbnailSubAssets(definition, session);

                // Move the original source asset into the library.
                if (!string.Equals(sourcePath, sourceDestinationPath, StringComparison.Ordinal))
                {
                    string moveError = AssetDatabase.MoveAsset(sourcePath, sourceDestinationPath);

                    if (!string.IsNullOrEmpty(moveError))
                        throw new InvalidOperationException($"Failed to move source asset: {moveError}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Mark the item saved only after all persistence operations succeed.
                item.MarkSaved();

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                // Remove the partially created definition.
                if (definitionCreated && !string.IsNullOrEmpty(definitionPath))
                    AssetDatabase.DeleteAsset(definitionPath);

                // Restore the source asset if it was moved.
                if (!string.Equals(sourcePath, originalSourcePath, StringComparison.Ordinal) &&
                    AssetDatabase.LoadMainAssetAtPath(originalSourcePath) == null)
                {
                    string rollbackError = AssetDatabase.MoveAsset(
                        sourcePath,
                        originalSourcePath);

                    if (!string.IsNullOrEmpty(rollbackError))
                        Debug.LogError($"Failed to restore source asset: {rollbackError}");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                errorMessage = exception.Message;
                return false;
            }
        }

        private static void SaveThumbnailSubAssets(AssetDefinition definition, AssetImportSession session)
        {
            SerializedObject serializedDefinition = new SerializedObject(definition);
            SerializedProperty thumbnailProperty = serializedDefinition.FindProperty("thumbnail");

            if (thumbnailProperty == null || thumbnailProperty.objectReferenceValue is not Sprite sprite)
                return;

            Texture2D texture = sprite.texture;

            if (texture == null)
                return;

            // Store the texture before the sprite because the sprite references it
            AssetDatabase.AddObjectToAsset(texture, definition);
            AssetDatabase.AddObjectToAsset(sprite, definition);

            // Transfer ownership from the temporary import session to the asset
            session.TransferGeneratedThumbnail(sprite, texture);
        }

        private static string GetDefinitionFolderName(AssetDefinition definition)
        {
            string typeName = definition.GetType().Name;

            const string suffix = "Definition";

            if (typeName.EndsWith(suffix, StringComparison.Ordinal))
                typeName = typeName.Substring(0, typeName.Length - suffix.Length);

            return typeName + "Definitions";
        }

        private static string CombineAssetPath(string parent, string child)
        {
            if (string.IsNullOrWhiteSpace(child))
                return parent;

            string normalizedChild = child.Replace('\\', '/').Trim('/');

            if (normalizedChild == ".." ||
                normalizedChild.StartsWith("../", StringComparison.Ordinal) ||
                normalizedChild.Contains("/../"))
            {
                throw new ArgumentException("Destination paths cannot escape the library folder.");
            }

            return parent.TrimEnd('/') + "/" + normalizedChild;
        }

        private string GetSourceFolder(AssetDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.CategoryId))
                return LibraryFolder + "/Uncategorized";

            if (!AssetDatabaseAccessor.CategoryRegistry.TryGetCategoryPath(definition.CategoryId, out IReadOnlyList<string> categoryPath))
                throw new InvalidOperationException($"Category '{definition.CategoryId}' could not be resolved.");

            string folderPath = LibraryFolder;

            foreach (string categoryName in categoryPath)
            {
                if (string.IsNullOrWhiteSpace(categoryName))
                    throw new InvalidOperationException("A category has an empty display name.");

                folderPath = CombineAssetPath(folderPath, SanitizeFileName(categoryName));
            }

            return folderPath;
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "UnnamedAsset";

            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            foreach (char character in invalidCharacters)
                fileName = fileName.Replace(character, '_');

            return fileName.Trim();
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                throw new InvalidOperationException($"Invalid folder path: {folderPath}");

            EnsureFolderExists(parent);

            if (!AssetDatabase.IsValidFolder(parent))
                throw new InvalidOperationException($"Parent folder does not exist: {parent}");

            string guid = AssetDatabase.CreateFolder(parent, folderName);

            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Failed to create folder: {folderPath}");
        }
    }
}
