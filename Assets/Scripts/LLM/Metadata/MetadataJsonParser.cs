using Newtonsoft.Json;
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

                    var settings = new JsonLoadSettings
                    {
                        DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                    };

                    JToken token = JToken.Load(reader, settings);

                    // Reject content after the JSON object.
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

                    JsonSerializer serializer = JsonSerializer.Create(
                        new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        });

                    metadata = token.ToObject<TMetadata>(serializer);

                    if (metadata == null)
                    {
                        errorMessage = "The response could not be converted to metadata.";
                        return false;
                    }

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

        private static bool TryValidateProperties(
            JObject jsonObject,
            Type metadataType,
            out string errorMessage)
        {
            errorMessage = null;

            HashSet<string> allowedPropertyNames = GetAllowedPropertyNames(metadataType);

            foreach (JProperty property in jsonObject.Properties())
            {
                if (!allowedPropertyNames.Contains(property.Name))
                {
                    errorMessage = $"Unexpected property '{property.Name}' for metadata type '{metadataType.Name}'.";
                    return false;
                }
            }

            return true;
        }

        private static HashSet<string> GetAllowedPropertyNames(
            Type metadataType)
        {
            var propertyNames = new HashSet<string>(
                StringComparer.Ordinal);

            for (Type currentType = metadataType;
                 currentType != null &&
                 typeof(GeneratedMetadata).IsAssignableFrom(currentType);
                 currentType = currentType.BaseType)
            {
                foreach (PropertyInfo property in currentType.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    if (property.GetIndexParameters().Length == 0)
                    {
                        propertyNames.Add(property.Name);
                    }
                }

                foreach (FieldInfo field in currentType.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly))
                {
                    propertyNames.Add(field.Name);
                }
            }

            return propertyNames;
        }
    }
}