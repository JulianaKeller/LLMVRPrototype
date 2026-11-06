using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
// use Newtonsoft JSON rather than Unity's JsonUtility, which can silently ignore unknown fields. This parser requires exactly two properties, with the expected names and types, and rejects Markdown fences, additional fields, malformed JSON, and duplicate property names.

namespace LLMVRPrototype
{
    public static class MetadataJsonParser
    {
        public static bool TryParseGeneratedMetadata<TMetadata>(
            string json,
            out TMetadata metadata,
            out string errorMessage)
            where TMetadata : GeneratedMetadata
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
                using (var reader = new JsonTextReader(new StringReader(json)))
                {
                    reader.DateParseHandling = DateParseHandling.None;
                    reader.FloatParseHandling = FloatParseHandling.Double;

                    var loadSettings = new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error,
                        CommentHandling = CommentHandling.Ignore,
                        LineInfoHandling = LineInfoHandling.Load
                    };

                    JToken token = JToken.Load(reader, loadSettings);

                    // Reject any content after the JSON object.
                    while (reader.Read())
                    {
                        if (reader.TokenType != JsonToken.Comment)
                        {
                            errorMessage = "Unexpected content after the JSON object.";
                            return false;
                        }
                    }

                    if (token.Type != JTokenType.Object)
                    {
                        errorMessage = "The response must be a JSON object.";
                        return false;
                    }

                    var serializerSettings = new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Error,
                        NullValueHandling = NullValueHandling.Include,
                        DefaultValueHandling = DefaultValueHandling.Include,
                        Converters =
                        {
                            new StringEnumConverter()
                        }
                    };

                    JsonSerializer serializer = JsonSerializer.Create(serializerSettings);

                    metadata = token.ToObject<TMetadata>(serializer);

                    if (metadata == null)
                    {
                        errorMessage = "The response could not be converted to metadata.";
                        return false;
                    }

                    return true;
                }
            }
            catch (JsonReaderException exception)
            {
                errorMessage = $"Invalid JSON: {exception.Message}";
                metadata = null;
                return false;
            }
            catch (JsonSerializationException exception)
            {
                errorMessage = $"Invalid metadata structure: {exception.Message}";
                metadata = null;
                return false;
            }
            catch (JsonException exception)
            {
                errorMessage = $"The response contains invalid JSON: {exception.Message}";
                metadata = null;
                return false;
            }
            catch (Exception)
            {
                errorMessage = "The response could not be parsed.";
                metadata = null;
                return false;
            }
        }
    }
}