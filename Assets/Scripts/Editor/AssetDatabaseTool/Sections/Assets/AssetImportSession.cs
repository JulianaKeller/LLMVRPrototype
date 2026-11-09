using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LLMVRPrototype.EditorTools
{
    public sealed class AssetImportSession : IDisposable
    {
        private readonly List<AssetImportItem> items = new();

        private readonly HashSet<Sprite> generatedSprites = new();
        private readonly HashSet<Texture2D> generatedTextures = new();

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

        public void ReplaceGeneratedThumbnail(
                    Sprite oldSprite,
                    Texture2D oldTexture,
                    Sprite newSprite,
                    Texture2D newTexture)
        {
            if (newSprite != null)
                generatedSprites.Add(newSprite);

            if (newTexture != null)
                generatedTextures.Add(newTexture);

            if (oldSprite != null)
            {
                generatedSprites.Remove(oldSprite);
                UnityEngine.Object.DestroyImmediate(oldSprite);
            }

            if (oldTexture != null)
            {
                generatedTextures.Remove(oldTexture);
                UnityEngine.Object.DestroyImmediate(oldTexture);
            }
        }

        public void AddGeneratedSprite(Sprite sprite)
        {
            ThrowIfDisposed();

            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));

            generatedSprites.Add(sprite);
        }

        public void RegisterGeneratedThumbnail(Sprite sprite, Texture2D texture)
        {
            ThrowIfDisposed();

            if (sprite != null)
                generatedSprites.Add(sprite);

            if (texture != null)
                generatedTextures.Add(texture);
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
                if (sprite == null)
                    continue;

                Texture2D texture = sprite.texture;

                UnityEngine.Object.DestroyImmediate(sprite);

                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }

            generatedSprites.Clear();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            ClearTemporaryObjects();

            foreach (AssetImportItem item in items)
                item.DestroyTemporaryObjects();

            foreach (Sprite sprite in generatedSprites)
            {
                if (sprite != null)
                    UnityEngine.Object.DestroyImmediate(sprite);
            }

            foreach (Texture2D texture in generatedTextures)
            {
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }

            generatedSprites.Clear();
            generatedTextures.Clear();
            items.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(AssetImportSession));
        }
    }
}