using System;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    public static class MetadataValidators
    {
        // Tags
        public const int MinSynonyms = 1;
        public const int MaxSynonyms = 8;
        public const int MaxDescriptionLength = 300;
        public const int MaxSynonymLength = 60;

        public static bool TryValidateTagMetadata(
            TagMetadata metadata,
            string tagDisplayName,
            out string errorMessage)
        {
            errorMessage = null;

            if (metadata == null)
            {
                errorMessage = "The metadata object is missing.";
                return false;
            }

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

            if (metadata.Synonyms == null)
            {
                errorMessage = "The synonyms array is missing.";
                return false;
            }

            string normalizedTagName = (tagDisplayName ?? string.Empty).Trim();

            var uniqueSynonyms = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                if (string.Equals(
                    synonym,
                    normalizedTagName,
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
    }
}