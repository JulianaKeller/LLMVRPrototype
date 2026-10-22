using LLMVRPrototype.Assets.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets.Editor
{
    public class AssetDefinitionValidator
    {
        public static List<string> Validate(AssetCatalog catalog, AssetCategoryRegistry categoryRegistry, AssetTagRegistry tagRegistry)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (catalog == null)
            {
                errors.Add("Asset catalog reference is null.");
                return errors;
            }

            if (categoryRegistry == null)
                errors.Add("Category registry reference is null.");

            if (tagRegistry == null)
                errors.Add("Tag registry reference is null.");

            var definitions = catalog.Assets;
            var ids = new Dictionary<string, AssetDefinition>(
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < definitions.Count; i++)
            {
                AssetDefinition asset = definitions[i];

                if (asset == null)
                {
                    errors.Add($"Entry {i}: Asset definition is null.");
                    continue;
                }

                string context = $"Asset '{asset.name}'";

                if (string.IsNullOrWhiteSpace(asset.AssetId))
                {
                    errors.Add($"{context}: Missing asset ID.");
                }
                else
                {
                    string id = asset.AssetId.Trim();

                    if (ids.TryGetValue(id, out AssetDefinition existing))
                    {
                        errors.Add(
                            $"{context}: Duplicate asset ID '{id}'. " +
                            $"Also used by '{existing.name}'.");
                    }
                    else
                    {
                        ids.Add(id, asset);
                    }

                    if (asset.AssetId != id)
                    {
                        warnings.Add(
                            $"{context}: Asset ID contains leading or trailing whitespace.");
                    }
                }

                if (string.IsNullOrWhiteSpace(asset.DisplayName))
                    errors.Add($"{context}: Missing display name.");

                if (string.IsNullOrWhiteSpace(asset.CategoryId))
                    errors.Add($"{context}: Missing category ID.");

                ValidateCategory(asset, context, categoryRegistry, errors);
                ValidateTags(asset, context, tagRegistry, errors, warnings);
                ValidateResourceReference(asset, context, errors);
            }

            foreach (string warning in warnings)
                Debug.LogWarning(warning, catalog);

            return errors;
        }

        private static void ValidateCategory(
            AssetDefinition asset,
            string context,
            AssetCategoryRegistry registry,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(asset.CategoryId))
            {
                errors.Add($"{context}: Missing category ID.");
                return;
            }

            if (registry != null && !registry.Contains(asset.CategoryId))
            {
                errors.Add($"{context}: Unknown category ID '{asset.CategoryId}'.");
            }
        }

        private static void ValidateTags(
            AssetDefinition asset,
            string context,
            AssetTagRegistry registry,
            List<string> errors,
            List<string> warnings)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string tagId in asset.TagIds)
            {
                if (string.IsNullOrWhiteSpace(tagId))
                {
                    errors.Add($"{context}: Contains an empty tag ID.");
                    continue;
                }

                string id = tagId.Trim();

                if (!seen.Add(id))
                {
                    errors.Add($"{context}: Duplicate tag ID '{id}'.");
                    continue;
                }

                if (tagId != id)
                {
                    warnings.Add($"{context}: Tag ID '{tagId}' has surrounding whitespace.");
                }

                if (registry != null && !registry.Contains(id))
                {
                    errors.Add($"{context}: Unknown tag ID '{id}'.");
                }
            }
        }

        // TODO add more cases if new AssetDefinitions are added
        private static void ValidateResourceReference(
            AssetDefinition asset,
            string context,
            List<string> errors)
        {
            switch (asset)
            {
                case ObjectAssetDefinition prefabDefinition:
                    if (prefabDefinition.Prefab == null)
                        errors.Add($"{context}: Missing prefab reference.");
                    break;

                case MaterialAssetDefinition materialDefinition:
                    if (materialDefinition.Material == null)
                        errors.Add($"{context}: Missing material reference.");
                    break;

                case TextureAssetDefinition textureDefinition:
                    if (textureDefinition.Texture == null)
                        errors.Add($"{context}: Missing texture reference.");
                    break;

                case AudioAssetDefinition audioDefinition:
                    if (audioDefinition.AudioClip == null)
                        errors.Add($"{context}: Missing audio clip reference.");
                    break;

                default:
                    errors.Add(
                        $"{context}: No resource validation rule exists for " +
                        $"definition type '{asset.GetType().Name}'.");
                    break;
            }
        }
    }
}
