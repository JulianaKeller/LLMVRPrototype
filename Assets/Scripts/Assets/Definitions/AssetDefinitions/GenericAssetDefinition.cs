using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "GenericAsset", menuName = "LLM VR/Assets/Generic Definition")]
    public class GenericAssetDefinition : AssetDefinition
    {
        [Header("Source")]
        [SerializeField] private Object sourceAsset;

        public Object SourceAsset => sourceAsset;
    }
}