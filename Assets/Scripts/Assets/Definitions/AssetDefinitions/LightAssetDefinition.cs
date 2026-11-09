using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "LightAsset", menuName = "LLM VR/Assets/Light Definition")]
    public class LightAssetDefinition : AssetDefinition
    {
        [Header("Light")]
        [SerializeField] private GameObject prefab;

        public GameObject Prefab => prefab;
    }
}