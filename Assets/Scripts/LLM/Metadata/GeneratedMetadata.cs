
using LLMVRPrototype.Assets;
using Newtonsoft.Json;
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
    public sealed class GeneratedOperationsMetadata : GeneratedMetadata
    {
        [JsonProperty(MetadataJsonFields.TargetType)]
        private OperationTargetType targetType;
        [JsonProperty(MetadataJsonFields.RequiresTarget)]
        private bool requiresTarget;
    }

    [Serializable]
    public sealed class GeneratedAssetMetadata : GeneratedMetadata
    {
        [JsonProperty(MetadataJsonFields.Tags)]
        public List<string> Tags = new List<string>();
    }
}
