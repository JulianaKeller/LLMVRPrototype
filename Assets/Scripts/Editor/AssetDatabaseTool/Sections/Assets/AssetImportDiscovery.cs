using LLMVRPrototype;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LLMVRPrototype
{
    public class AssetImportDiscovery
    {
        private static readonly HashSet<string> SupportedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // 3D models
                ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".blend",
                ".ma", ".mb", ".abc", ".lwo", ".lws", ".c4d",

                // Unity assets
                ".prefab", ".unity", ".asset", ".mat",
                ".controller", ".overridecontroller", ".anim", ".mask",
                ".physicmaterial", ".rendertexture", ".terrainlayer",
                ".guiskin", ".flare", ".lighting", ".giparams",
                ".cubemap", ".shadervariants", ".compute", ".shader",
                ".vfx", ".spriteatlas", ".mixer", ".playable",
                ".signal", ".preset",

                // Textures and images
                ".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff",
                ".psd", ".exr", ".hdr", ".bmp", ".gif", ".svg",

                // Audio
                ".wav", ".mp3", ".ogg", ".aiff", ".aif",
                ".mod", ".it", ".s3m", ".xm",

                // Video
                ".mp4", ".mov", ".avi", ".asf", ".mpg", ".mpeg",

                // Fonts
                ".ttf", ".otf", ".fnt"
            };

        public static bool TryDiscover(
            string absoluteFolderPath,
            out List<AssetImportCandidate> candidates,
            out int skippedFileCount,
            out string errorMessage)
        {
            candidates = new List<AssetImportCandidate>();
            skippedFileCount = 0;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(absoluteFolderPath))
            {
                errorMessage = "No folder was selected.";
                return false;
            }

            string selectedFolder;
            string assetsRoot;

            try
            {
                selectedFolder = Path.GetFullPath(absoluteFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                assetsRoot = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception exception)
            {
                errorMessage = $"Invalid folder path: {exception.Message}";
                return false;
            }

            // Only discover files inside this Unity project's Assets folder
            if (!selectedFolder.Equals(assetsRoot, StringComparison.OrdinalIgnoreCase) && !selectedFolder.StartsWith(
                    assetsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Select a folder inside this project's Assets directory.";
                return false;
            }

            if (!Directory.Exists(selectedFolder))
            {
                errorMessage = "The selected folder does not exist.";
                return false;
            }

            try
            {
                string[] files = Directory.GetFiles(
                    selectedFolder,
                    "*",
                    SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    if (string.Equals(Path.GetExtension(file), ".meta",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string extension = Path.GetExtension(file);

                    if (!SupportedExtensions.Contains(extension))
                    {
                        skippedFileCount++;
                        continue;
                    }

                    string assetPath = ToAssetPath(file);

                    // Unity has to recognize the file as an imported asset
                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                    if (mainAsset == null)
                    {
                        skippedFileCount++;
                        continue;
                    }

                    Type definitionType = AssetDefinitionTypeResolver.Resolve(mainAsset);

                    candidates.Add(new AssetImportCandidate(
                        assetPath,
                        Path.GetFileNameWithoutExtension(file),
                        mainAsset.GetType().Name,
                        definitionType));
                }

                candidates = candidates
                    .OrderBy(candidate => candidate.DisplayName,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(candidate => candidate.AssetPath,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return true;
            }
            catch (Exception exception)
            {
                candidates.Clear();
                errorMessage = $"Asset discovery failed: {exception.Message}";
                return false;
            }
        }

        private static string ToAssetPath(string absoluteFilePath)
        {
            string normalizedPath = Path.GetFullPath(absoluteFilePath).Replace('\\', '/');

            string normalizedAssetsPath = Path.GetFullPath(Application.dataPath)
                .Replace('\\', '/')
                .TrimEnd('/');

            return "Assets" + normalizedPath.Substring(normalizedAssetsPath.Length);
        }
    }
}
