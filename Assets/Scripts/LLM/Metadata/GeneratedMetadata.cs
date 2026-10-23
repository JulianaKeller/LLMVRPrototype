
using System;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    [Serializable]
    public sealed class MetadataGenerationInput
    {
        public string EntityType;
        public string Name;
        public string ContextLabel;
        public string ContextDescription;
    }

    [Serializable]
    public sealed class GeneratedMetadata
    {
        public string Description;
        public List<string> Terms = new List<string>();
    }
}
