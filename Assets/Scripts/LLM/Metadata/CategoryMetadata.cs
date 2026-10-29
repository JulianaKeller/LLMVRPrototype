using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [Serializable]
    public sealed class CategoryMetadata
    {
        public string Description { get; set; }

        public List<string> Synonyms { get; set; } = new List<string>();
    }
}
