using UnityEngine;

namespace LLMVRPrototype.Assets.Assets
{
    [CreateAssetMenu(fileName = "TextureAsset", menuName = "LLM VR/Assets/Texture Definition")]
    public class TextureAssetDefinition : AssetDefinition
    {
        [SerializeField] private Texture texture;

        public Texture Texture => texture;

    }
}
