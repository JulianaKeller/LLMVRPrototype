using UnityEngine;

namespace Project.LLM.Providers.AcademicCloud
{
    [CreateAssetMenu(
        fileName = "AcademicCloudConfig",
        menuName = "LLM/Providers/Academic Cloud Configuration")]
    public sealed class AcademicCloudConfig : ScriptableObject
    {
        [Header("API Endpoint")]
        [SerializeField]
        private string baseUrl = "https://chat-ai.academiccloud.de/v1";

        [Header("Model")]
        [SerializeField]
        private string model = "meta-llama-3.1-8b-instruct";

        [Header("Networking")]
        [SerializeField, Min(1)]
        private int timeoutSeconds = 90;

        public string BaseUrl => baseUrl;

        public string Model => model;

        public int TimeoutSeconds => timeoutSeconds;

        private void OnValidate()
        {
            timeoutSeconds = Mathf.Max(1, timeoutSeconds);

            baseUrl = baseUrl?.Trim().TrimEnd('/');
            model = model?.Trim();
        }
    }
}