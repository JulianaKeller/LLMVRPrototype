using UnityEngine;

namespace LLMVRPrototype.Assets
{
    [CreateAssetMenu(fileName = "AudioAsset", menuName = "LLM VR/Assets/Audio Definition")]
    public class AudioAssetDefinition : AssetDefinition
    {
        [SerializeField] private AudioClip audioClip;

        public AudioClip AudioClip => audioClip;
    }
}
