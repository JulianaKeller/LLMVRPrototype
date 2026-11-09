using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "ObjectAsset", menuName = "LLM VR/Assets/Object Definition")]
    public class ObjectAssetDefinition : AssetDefinition {
        [Header("Prefab")]
        [SerializeField] private GameObject prefab;

        [Header("Physical Properties")]
        [SerializeField] private Vector3 physicalSize = Vector3.one;

        public GameObject Prefab => prefab;
        public Vector3 PhysicalSize => physicalSize;
    }
}
