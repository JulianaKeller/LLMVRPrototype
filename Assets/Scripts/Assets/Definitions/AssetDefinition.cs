using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    public abstract class AssetDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string assetId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;

        [Header("Classification")]
        [SerializeField] private string categoryId;
        [SerializeField] private string parentCategoryId;
        [SerializeField] private List<string> tagIds = new();
        [SerializeField] private List<string> styleTagIds = new();
        // [SerializeField] private List<string> compatibleRoomIds = new();

        [Header("Appearance")]
        [SerializeField] private List<Color> dominantColors = new();
        [SerializeField] private Sprite thumbnail;

        [Header("Operations")]
        [SerializeField] private List<string> supportedOperationIds = new();

        [Header("Status")]
        [SerializeField] private bool isEnabled = true;

        public string AssetId => assetId;
        public string DisplayName => displayName;
        public string Description => description;
        public string CategoryId => categoryId;
        public string ParentCategoryId => parentCategoryId;
        public IReadOnlyList<string> TagIds => tagIds;
        public IReadOnlyList<string> StyleTagIds => styleTagIds;
        // public IReadOnlyList<string> CompatibleRoomIds => compatibleRoomIds;
        public IReadOnlyList<Color> DominantColors => dominantColors;
        public Sprite Thumbnail => thumbnail;
        public IReadOnlyList<string> SupportedOperationIds => supportedOperationIds;
        public bool IsEnabled => isEnabled;
    }
}
