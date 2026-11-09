using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LLMVRPrototype.EditorTools
{
    public sealed class AssetImportSession : IDisposable
    {
        private readonly List<AssetImportItem> items = new();
        private readonly List<Sprite> generatedSprites = new();

        private bool isDisposed;

        public IReadOnlyList<AssetImportItem> Items => items;

        public bool IsDisposed => isDisposed;

        public int RemainingCount => items.Count(item => !item.IsSaved && !item.IsDiscarded);
        public int SavedCount => items.Count(item => item.IsSaved);
        public int DiscardedCount => items.Count(item => item.IsDiscarded);


        public AssetImportSession(IEnumerable<AssetImportCandidate> candidates)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            foreach (AssetImportCandidate candidate in candidates)
            {
                if (candidate == null)
                    throw new ArgumentException("The candidate collection contains a null entry.", nameof(candidates));

                items.Add(new AssetImportItem(candidate));
            }
        }

        public AssetImportItem AddCandidate(AssetImportCandidate candidate)
        {
            ThrowIfDisposed();

            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            AssetImportItem item = new AssetImportItem(candidate);
            items.Add(item);

            return item;
        }

        public bool RemoveItem(AssetImportItem item)
        {
            ThrowIfDisposed();

            if (item == null || !items.Remove(item))
                return false;

            item.DestroyTemporaryObjects();
            return true;
        }

        public void ClearItems()
        {
            ThrowIfDisposed();
            items.Clear();
        }

        public void AddGeneratedSprite(Sprite sprite)
        {
            ThrowIfDisposed();

            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            generatedSprites.Add(sprite);
        }

        public void RemoveGeneratedSprite(Sprite sprite)
        {
            ThrowIfDisposed();

            if (sprite == null || !generatedSprites.Remove(sprite))
                return;

            UnityEngine.Object.DestroyImmediate(sprite);
        }

        public void ClearTemporaryObjects()
        {
            ThrowIfDisposed();

            foreach (AssetImportItem item in items)
                item.DestroyTemporaryObjects();

            foreach (Sprite sprite in generatedSprites)
            {
                if (sprite != null)
                    UnityEngine.Object.DestroyImmediate(sprite);
            }

            generatedSprites.Clear();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            foreach (AssetImportItem item in items)
                item.DestroyTemporaryObjects();

            foreach (Sprite sprite in generatedSprites)
            {
                if (sprite != null)
                    UnityEngine.Object.DestroyImmediate(sprite);
            }

            generatedSprites.Clear();
            items.Clear();

            isDisposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(AssetImportSession));
        }
    }
}