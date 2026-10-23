using Project.LLM.Core;
using Project.LLM.Providers.AcademicCloud;
using Project.LLM.Providers.Gemini;
using UnityEngine;
using static UnityEngine.Rendering.STP;

namespace LLMVRPrototype
{

    public class LLMTest : MonoBehaviour
    {
        [SerializeField]
        private AcademicCloudConfig academicCloudConfig;
        [SerializeField]
        private GeminiConfig geminiConfig;

        private LLMOrchestrator llmOrchestrator;
        private ConversationManager conversationManager;

        void Start()
        {
            string apiKeyAcademicCloud = LocalLLMSecrets.LoadAcademicCloudApiKey();
            string apiKeyGemini = LocalLLMSecrets.LoadGeminiApiKey();

            if (string.IsNullOrWhiteSpace(apiKeyAcademicCloud))
            {
                Debug.LogError("Academic Cloud API key is unavailable.");
                return;
            }
            if (string.IsNullOrWhiteSpace(apiKeyGemini))
            {
                Debug.LogError("Gemini API key is unavailable.");
                return;
            }

            var providerAcademicCloud = new AcademicCloudProvider(academicCloudConfig, apiKeyAcademicCloud);
            var providerGemini = new GeminiProvider(geminiConfig, apiKeyGemini);
            conversationManager = new ConversationManager();


            llmOrchestrator = new LLMOrchestrator(providerGemini, conversationManager);

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
                Debug.LogError($"LLM request failed: {response.ErrorMessage}");
            }
        }
    }
}
