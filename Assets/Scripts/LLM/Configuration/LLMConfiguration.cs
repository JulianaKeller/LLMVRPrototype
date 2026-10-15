using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "LLMConfiguration", menuName = "LLM/LLMConfiguration")]
    public class LLMConfiguration : ScriptableObject
    {
        [Header("Request Defaults")]
        [SerializeField, Range(0f, 2f)]
        private float defaultTemperature = 0.7f;

        [SerializeField, Min(1)]
        private int defaultMaxOutputTokens = 1024;

        [Header("Request Limits")]
        [Tooltip("Maximum number of conversation messages to retain.")]
        [SerializeField, Min(0)]
        private int maxConversationHistoryMessages = 20;

        public float DefaultTemperature => defaultTemperature;

        public int DefaultMaxOutputTokens => defaultMaxOutputTokens;

        public int MaxConversationHistoryMessages => maxConversationHistoryMessages;

        private void OnValidate()
        {
            defaultTemperature = Mathf.Clamp(
                defaultTemperature,
                0f,
                2f);

            defaultMaxOutputTokens = Mathf.Max(
                1,
                defaultMaxOutputTokens);

            maxConversationHistoryMessages = Mathf.Max(
                0,
                maxConversationHistoryMessages);
        }
    }
}
