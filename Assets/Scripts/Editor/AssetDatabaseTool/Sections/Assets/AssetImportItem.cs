using System;
using UnityEngine;

namespace LLMVRPrototype.EditorTools
{
    public sealed class AssetImportItem
    {
        public AssetImportCandidate Candidate { get; }
        public AssetDefinition Definition { get; private set; }

        public bool IsSaved { get; private set; }
        public bool IsDiscarded { get; private set; }

        public AssetImportItem(AssetImportCandidate candidate)
        {
            Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        }

        public void SetDefinition(AssetDefinition definition)
        {
            if (IsSaved || IsDiscarded)
                throw new InvalidOperationException("Cannot modify a completed import item.");

            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (Definition != null && Definition != definition)
                throw new InvalidOperationException("An import item already has a definition.");

            Definition = definition;
        }

        public void MarkSaved()
        {
            if (IsDiscarded)
                throw new InvalidOperationException("A discarded import item cannot be saved.");

            if (Definition == null)
                throw new InvalidOperationException("Cannot save an item without a definition.");

            IsSaved = true;
        }

        public void MarkDiscarded()
        {
            if (IsSaved)
                throw new InvalidOperationException("A saved import item cannot be discarded.");

            IsDiscarded = true;
        }

        public void DestroyTemporaryObjects()
        {
            if (Definition != null)
            {
                UnityEngine.Object.DestroyImmediate(Definition);
                Definition = null;
            }
        }
    }
}