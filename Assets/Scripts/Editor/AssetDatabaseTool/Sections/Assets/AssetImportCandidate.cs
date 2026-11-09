using System;
using UnityEngine;
using System.Globalization;

namespace LLMVRPrototype
{
    public sealed class AssetImportCandidate
    {
        public string AssetPath { get; }
        public string DisplayName { get; }
        public string AssetType { get; }

        public string PreliminaryName { get; set; }
        public string PreliminaryDescription { get; set; }

        public Type DefinitionType { get; set; }
        public Texture2D PreviewTexture { get; set; }

        public AssetImportCandidate(
            string assetPath,
            string displayName,
            string assetType,
            Type definitionType)
        {
            AssetPath = assetPath;
            DisplayName = displayName;
            AssetType = assetType;
            DefinitionType = definitionType;

            PreliminaryName = FormatPreliminaryName(displayName);
            PreliminaryDescription = string.Empty;
        }

        private static string FormatPreliminaryName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string formatted = value.Replace('_', ' ').Trim().ToLowerInvariant();

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted);
        }
    }
}
