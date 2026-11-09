using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "ParticleSystemAsset", menuName = "LLM VR/Assets/Particle System Definition")]
    public class ParticleSystemAssetDefinition : AssetDefinition
    {
        [Header("Particle System")]
        [SerializeField] private GameObject prefab;

        public GameObject Prefab => prefab;
    }
}