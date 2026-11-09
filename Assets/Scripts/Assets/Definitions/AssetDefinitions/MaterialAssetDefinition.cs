using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "MaterialAsset", menuName = "LLM VR/Assets/Material Definition")]
    public class MaterialAssetDefinition : AssetDefinition
    {
        [SerializeField] private Material material;

        public Material Material => material;
    }
}
