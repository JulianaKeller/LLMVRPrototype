using System;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    [Serializable]
    public sealed class TagMetadata
    {
        public string Description { get; set; }

        public List<string> Synonyms { get; set; } = new List<string>();
    }
}