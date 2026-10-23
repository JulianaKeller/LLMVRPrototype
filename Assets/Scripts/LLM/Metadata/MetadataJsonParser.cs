using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// use Newtonsoft JSON rather than Unity's JsonUtility, which can silently ignore unknown fields. This parser requires exactly two properties, with the expected names and types, and rejects Markdown fences, additional fields, malformed JSON, and duplicate property names.

namespace LLMVRPrototype
{
    public static class MetadataJsonParser
    {
        public static bool TryParseTagMetadata(
            string json,
            out TagMetadata metadata,
            out string errorMessage)
        {
            metadata = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                errorMessage = "The response is empty.";
                return false;
            }

            try
            {
                using (var reader = new JsonTextReader(new System.IO.StringReader(json)))
                {
                    reader.DateParseHandling = DateParseHandling.None;

                    var settings = new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                    };

                    JToken token = JToken.Load(reader, settings);

                    // Reject any non-whitespace content after the JSON object.
                    if (reader.Read())
                    {
                        errorMessage = "Unexpected content after the JSON object.";
                        return false;
                    }

                    if (token.Type != JTokenType.Object)
                    {
                        errorMessage = "The response must be a JSON object.";
                        return false;
                    }

                    var obj = (JObject)token;

                    // Only the declared schema properties are permitted.
                    if (obj.Count != 2 ||
                        obj.Property("description") == null ||
                        obj.Property("synonyms") == null)
                    {
                        errorMessage = "The JSON must contain exactly 'description' and 'synonyms'.";
                        return false;
                    }

                    JToken descriptionToken = obj["description"];
                    JToken synonymsToken = obj["synonyms"];

                    if (descriptionToken.Type != JTokenType.String)
                    {
                        errorMessage = "'description' must be a string.";
                        return false;
                    }

                    if (synonymsToken.Type != JTokenType.Array)
                    {
                        errorMessage = "'synonyms' must be an array of strings.";
                        return false;
                    }

                    var synonyms = new System.Collections.Generic.List<string>();

                    foreach (JToken item in (JArray)synonymsToken)
                    {
                        if (item.Type != JTokenType.String)
                        {
                            errorMessage = "Every synonym must be a string.";
                            return false;
                        }

                        synonyms.Add(item.Value<string>());
                    }

                    metadata = new TagMetadata
                    {
                        Description = descriptionToken.Value<string>(),
                        Synonyms = synonyms
                    };

                    return true;
                }
            }
            catch (JsonException)
            {
                errorMessage = "The response contains invalid JSON or duplicate properties.";
                return false;
            }
            catch (Exception)
            {
                errorMessage = "The response could not be parsed.";
                return false;
            }
        }
    }
}