using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "MaterialAsset", menuName = "LLM VR/Assets/Material Definition")]
    public class MaterialAssetDefinition : AssetDefinition
    {
        [SerializeField] private Material material;

        [SerializeField] private List<Color> dominantColors = new();

        public Material Material => material;

        public IReadOnlyList<Color> DominantColors => dominantColors;
    }
}
