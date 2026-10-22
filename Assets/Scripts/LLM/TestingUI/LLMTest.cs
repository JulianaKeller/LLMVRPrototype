using Project.LLM.Core;
using Project.LLM.Providers.AcademicCloud;
using UnityEngine;
using static UnityEngine.Rendering.STP;

namespace LLMVRPrototype
{
    public class LLMTest : MonoBehaviour
    {
        [SerializeField]
        private AcademicCloudConfig academicCloudConfig;

        private LLMOrchestrator llmOrchestrator;
        private ConversationManager conversationManager;

        void Start()
        {
            string apiKey = LocalLLMSecrets.LoadAcademicCloudApiKey();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Debug.LogError("Academic Cloud API key is unavailable.");
                return;
            }

            var provider = new AcademicCloudProvider(academicCloudConfig, apiKey);
            conversationManager = new ConversationManager();
            llmOrchestrator = new LLMOrchestrator(provider, conversationManager);

            _ = TestConnectionAsync();
        }

        void Update()
        {
        
        }

        private async System.Threading.Tasks.Task TestConnectionAsync()
        {
            var request = new LLMRequest("Reply with exactly 'Connection successful'.", "You are a concise assistant.");

            LLMResponse response = await llmOrchestrator.SendAsync(request);

            if (response.Success)
            {
                Debug.Log("LLM response: " + response.Text);
            }
            else
            {
                Debug.LogError(
                    $"LLM request failed: {response.ErrorMessage}");
            }
        }
    }
}
