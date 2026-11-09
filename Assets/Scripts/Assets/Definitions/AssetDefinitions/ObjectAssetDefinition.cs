using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "ObjectAsset", menuName = "LLM VR/Assets/Object Definition")]
    public class ObjectAssetDefinition : AssetDefinition {
        [Header("Prefab")]
        [SerializeField] private GameObject prefab;

        [Header("Physical Properties")]
        [SerializeField] private Vector3 physicalSize = Vector3.one;

        [Header("Import Transform")]
        [SerializeField] private Vector3 importRotation;

        public Vector3 ImportRotation => importRotation;

        public GameObject Prefab => prefab;
        public Vector3 PhysicalSize => physicalSize;
    }
}
