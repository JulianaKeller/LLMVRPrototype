using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets.Assets
{
    [CreateAssetMenu(fileName = "TextureAsset", menuName = "LLM VR/Assets/Texture Definition")]
    public class TextureAssetDefinition : AssetDefinition
    {
        [SerializeField] private Texture texture;

        [SerializeField] private List<Color> dominantColors = new();

        public Texture Texture => texture;

        public IReadOnlyList<Color> DominantColors => dominantColors;

    }
}
