using UnityEngine;
using System.Threading.Tasks;

namespace LLMVRPrototype
{
    public interface ILLMProvider
    {
        Task<LLMResponse> SendAsync(LLMRequest request); // for sending requests
        string GetProviderName(); // for identifying the provider
        ValidationResult ValidateConfiguration();

        // Responsibilities:
        // Validate configuration
        // Convert LLMRequest into JSON request
        // Build HttpRequestData
        // Send HttpRequestData through ILLMTransport
        // Convert parsed HttpResponseData.Body into LLMResponse
    }
}
