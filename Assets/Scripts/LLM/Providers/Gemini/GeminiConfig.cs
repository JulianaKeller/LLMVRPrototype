using UnityEngine;

namespace Project.LLM.Providers.Gemini
{
    [CreateAssetMenu(
        fileName = "GeminiConfig",
        menuName = "LLM/Providers/Gemini Configuration")]
    public sealed class GeminiConfig : ScriptableObject
    {
        [Header("API Endpoint")]
        [SerializeField]
        private string baseUrl = "https://generativelanguage.googleapis.com/v1beta";

        [Header("Model")]
        [SerializeField]
        private string model = "gemini-2.5-flash";

        [Header("Networking")]
        [SerializeField, Min(1)]
        private int timeoutSeconds = 60;

        public string BaseUrl => baseUrl;
        public string Model => model;
        public int TimeoutSeconds => timeoutSeconds;

        private void OnValidate()
        {
            baseUrl = baseUrl?.Trim().TrimEnd('/');
            model = model?.Trim();
            timeoutSeconds = Mathf.Max(1, timeoutSeconds);
        }
    }
}