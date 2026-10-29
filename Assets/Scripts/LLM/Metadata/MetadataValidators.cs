using System;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    public static class MetadataValidators
    {
        public const int MinSynonyms = 1;
        public const int MaxSynonyms = 8;
        public const int MaxDescriptionLength = 300;
        public const int MaxSynonymLength = 60;

        public static bool TryValidateGeneratedMetadata<TMetadata>(
            TMetadata metadata,
            out string errorMessage)
            where TMetadata : GeneratedMetadata
        {
            return TryValidateGeneratedMetadata(
                metadata,
                null,
                out errorMessage);
        }

        public static bool TryValidateGeneratedMetadata<TMetadata>(
            TMetadata metadata,
            string entityName,
            out string errorMessage)
            where TMetadata : GeneratedMetadata
        {
            errorMessage = null;

            if (metadata == null)
            {
                errorMessage = "The metadata object is missing.";
                return false;
            }

            if (!TryValidateDescription(
                metadata,
                out errorMessage))
            {
                return false;
            }

            if (metadata is GeneratedTaxonomyMetadata taxonomyMetadata)
            {
                return TryValidateTaxonomyMetadata(
                    taxonomyMetadata,
                    entityName,
                    out errorMessage);
            }

            if (metadata is GeneratedAssetMetadata assetMetadata)
            {
                return TryValidateAssetMetadata(
                    assetMetadata,
                    out errorMessage);
            }

            return true;
        }

        private static bool TryValidateDescription(
            GeneratedMetadata metadata,
            out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(metadata.Description))
            {
                errorMessage = "The description is empty.";
                return false;
            }

            metadata.Description = metadata.Description.Trim();

            if (metadata.Description.Length > MaxDescriptionLength)
            {
                errorMessage = $"The description exceeds {MaxDescriptionLength} characters.";
                return false;
            }

            return true;
        }

        private static bool TryValidateTaxonomyMetadata(
            GeneratedTaxonomyMetadata metadata,
            string entityName,
            out string errorMessage)
        {
            errorMessage = null;

            if (metadata.Synonyms == null)
            {
                errorMessage = "The synonyms array is missing.";
                return false;
            }

            string normalizedEntityName = (entityName ?? string.Empty).Trim();

            var uniqueSynonyms = new List<string>();
            var seen = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string rawSynonym in metadata.Synonyms)
            {
                if (string.IsNullOrWhiteSpace(rawSynonym))
                    continue;

                string synonym = rawSynonym.Trim();

                if (synonym.Length > MaxSynonymLength)
                {
                    errorMessage = $"A synonym exceeds {MaxSynonymLength} characters.";
                    return false;
                }

                if (!string.IsNullOrEmpty(normalizedEntityName) &&
                    string.Equals(
                        synonym,
                        normalizedEntityName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (seen.Add(synonym))
                    uniqueSynonyms.Add(synonym);
            }

            if (uniqueSynonyms.Count < MinSynonyms)
            {
                errorMessage = $"At least {MinSynonyms} distinct synonyms are required.";
                return false;
            }

            if (uniqueSynonyms.Count > MaxSynonyms)
            {
                errorMessage = $"No more than {MaxSynonyms} synonyms are allowed.";
                return false;
            }

            metadata.Synonyms = uniqueSynonyms;
            return true;
        }

        private static bool TryValidateAssetMetadata(
            GeneratedAssetMetadata metadata,
            out string errorMessage)
        {
            errorMessage = null;

            if (metadata.Tags == null)
            {
                errorMessage = "The tags array is missing.";
                return false;
            }

            var uniqueTags = new List<string>();
            var seen = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (string rawTag in metadata.Tags)
            {
                if (string.IsNullOrWhiteSpace(rawTag))
                    continue;

                string tag = rawTag.Trim();

                if (seen.Add(tag))
                    uniqueTags.Add(tag);
            }

            metadata.Tags = uniqueTags;

            // Material, Style, and ObjectType are optional for now.
            return true;
        }
    }
}