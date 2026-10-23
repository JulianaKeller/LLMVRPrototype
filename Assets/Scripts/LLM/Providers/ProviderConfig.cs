using UnityEngine;

namespace LLMVRPrototype.LLM.Providers
{
    public abstract class ProviderConfig : ScriptableObject
    {
        [Header("Authentication")]
        public abstract string ApiKeySecretName { get; }
    }
}
