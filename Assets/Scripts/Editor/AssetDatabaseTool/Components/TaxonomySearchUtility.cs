using System;
using System.Collections.Generic;
using System.Linq;

namespace LLMVRPrototype.EditorTools
{
    // fuzzy matching, substring matching, synonym matching, and ranking.
    public static class TaxonomySearchUtility
    {
        public enum MatchType
        {
            ExactName = 0,
            ExactSynonym = 1,
            NameStartsWith = 2,
            SynonymStartsWith = 3,
            NameContains = 4,
            SynonymContains = 5,
            FuzzyName = 6,
            FuzzySynonym = 7
        }

        public sealed class SearchResult<T>
        {
            public T Definition { get; }
            public MatchType MatchType { get; }
            public int EditDistance { get; }

            public SearchResult(T definition, MatchType matchType, int editDistance)
            {
                Definition = definition;
                MatchType = matchType;
                EditDistance = editDistance;
            }
        }

        public static List<SearchResult<T>> Search<T>(
            IEnumerable<T> definitions,
            string query,
            Func<T, string> getDisplayName,
            Func<T, string> getId,
            Func<T, IEnumerable<string>> getSynonyms,
            int maxResults = 10,
            int minimumFuzzyQueryLength = 3)
        {
            var results = new List<SearchResult<T>>();

            if (definitions == null ||
                string.IsNullOrWhiteSpace(query) ||
                getDisplayName == null)
            {
                return results;
            }

            string normalizedQuery = Normalize(query);

            if (normalizedQuery.Length == 0)
                return results;

            var bestMatches = new Dictionary<T, SearchResult<T>>();

            foreach (T definition in definitions)
            {
                if (definition == null)
                    continue;

                string displayName = Normalize(getDisplayName(definition));
                string id = Normalize(getId?.Invoke(definition));

                MatchType? bestMatch = null;
                int bestDistance = int.MaxValue;

                EvaluateTerm(
                    displayName,
                    normalizedQuery,
                    false,
                    ref bestMatch,
                    ref bestDistance,
                    minimumFuzzyQueryLength);

                EvaluateTerm(
                    id,
                    normalizedQuery,
                    false,
                    ref bestMatch,
                    ref bestDistance,
                    minimumFuzzyQueryLength);

                IEnumerable<string> synonyms = getSynonyms?.Invoke(definition);

                if (synonyms != null)
                {
                    foreach (string synonym in synonyms)
                    {
                        EvaluateTerm(
                            Normalize(synonym),
                            normalizedQuery,
                            true,
                            ref bestMatch,
                            ref bestDistance,
                            minimumFuzzyQueryLength);
                    }
                }

                if (bestMatch == null)
                    continue;

                bestMatches[definition] = new SearchResult<T>(
                    definition,
                    bestMatch.Value,
                    bestDistance);
            }

            results.AddRange(bestMatches.Values);

            return results
                .OrderBy(result => result.MatchType)
                .ThenBy(result => result.EditDistance)
                .ThenBy(
                    result => Normalize(getDisplayName(result.Definition)),
                    StringComparer.Ordinal)
                .Take(Math.Max(1, maxResults))
                .ToList();
        }

        private static void EvaluateTerm(
            string term,
            string query,
            bool isSynonym,
            ref MatchType? bestMatch,
            ref int bestDistance,
            int minimumFuzzyQueryLength)
        {
            if (string.IsNullOrWhiteSpace(term))
                return;

            MatchType? match = null;
            int distance = int.MaxValue;

            if (string.Equals(term, query, StringComparison.Ordinal))
            {
                match = isSynonym
                    ? MatchType.ExactSynonym
                    : MatchType.ExactName;
            }
            else if (term.StartsWith(query, StringComparison.Ordinal))
            {
                match = isSynonym
                    ? MatchType.SynonymStartsWith
                    : MatchType.NameStartsWith;
            }
            else if (term.Contains(query))
            {
                match = isSynonym
                    ? MatchType.SynonymContains
                    : MatchType.NameContains;
            }
            else if (query.Length >= minimumFuzzyQueryLength)
            {
                int threshold = Math.Max(2, query.Length / 3);

                distance = GetEditDistance(term, query, threshold);

                if (distance <= threshold)
                {
                    match = isSynonym
                        ? MatchType.FuzzySynonym
                        : MatchType.FuzzyName;
                }
            }

            if (match == null)
                return;

            if (bestMatch == null ||
                match.Value < bestMatch.Value ||
                (match.Value == bestMatch.Value && distance < bestDistance))
            {
                bestMatch = match;
                bestDistance = distance;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

        private static int GetEditDistance(
            string source,
            string target,
            int maximumDistance)
        {
            if (Math.Abs(source.Length - target.Length) > maximumDistance)
                return maximumDistance + 1;

            int[,] distances = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; i++)
                distances[i, 0] = i;

            for (int j = 0; j <= target.Length; j++)
                distances[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                int rowMinimum = int.MaxValue;

                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                    int value = Math.Min(
                        Math.Min(
                            distances[i - 1, j] + 1,
                            distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);

                    distances[i, j] = value;
                    rowMinimum = Math.Min(rowMinimum, value);
                }

                if (rowMinimum > maximumDistance)
                    return maximumDistance + 1;
            }

            return distances[source.Length, target.Length];
        }
    }
}
