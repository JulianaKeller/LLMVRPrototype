
using LLMVRPrototype.Assets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [Serializable]
    public class GeneratedMetadata
    {
        [JsonProperty(MetadataJsonFields.Description)]
        public string Description;
        
    }

    [Serializable]
    public class GeneratedTaxonomyMetadata : GeneratedMetadata
    {
        [JsonProperty(MetadataJsonFields.Synonyms)]
        public List<string> Synonyms = new List<string>();
    }

    [Serializable]
    public sealed class GeneratedTagMetadata : GeneratedTaxonomyMetadata
    {

    }

    [Serializable]
    public sealed class GeneratedCategoryMetadata : GeneratedTaxonomyMetadata
    {

    }

    [Serializable]
    public sealed class GeneratedAssetMetadata : GeneratedMetadata
    {
        [JsonProperty(MetadataJsonFields.Tags)]
        public List<string> Tags = new List<string>();
    }

    [Serializable]
    public sealed class GeneratedOperationMetadata : GeneratedMetadata
    {
        [JsonProperty(MetadataJsonFields.TargetType)]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationTargetType TargetType;

        [JsonProperty(MetadataJsonFields.RequiresTarget)]
        public bool RequiresTarget;

        [JsonProperty("Parameters")]
        public List<GeneratedOperationParameterMetadata> Parameters = new List<GeneratedOperationParameterMetadata>();
    }

    [Serializable]
    public sealed class GeneratedOperationParameterMetadata
    {
        [JsonProperty("ParameterId")]
        public string ParameterId;

        [JsonProperty("DisplayName")]
        public string DisplayName;

        [JsonProperty("Description")]
        public string Description;

        [JsonProperty("ParameterType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OperationParameterType ParameterType;

        [JsonProperty("IsRequired")]
        public bool IsRequired = true;

        [JsonProperty("HasMinimum")]
        public bool HasMinimum;

        [JsonProperty("Minimum")]
        public JToken Minimum;

        [JsonProperty("HasMaximum")]
        public bool HasMaximum;

        [JsonProperty("Maximum")]
        public JToken Maximum;

        [JsonProperty("AllowedValues")]
        public List<JToken> AllowedValues = new List<JToken>();

        [JsonProperty("HasDefaultValue")]
        public bool HasDefaultValue;

        [JsonProperty("DefaultValue")]
        public JToken DefaultValue;
    }
}
