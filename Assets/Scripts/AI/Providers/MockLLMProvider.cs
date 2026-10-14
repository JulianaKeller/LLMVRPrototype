using UnityEngine;
using System.Threading.Tasks;

namespace LLMVRPrototype
{
    public class MockLLMProvider : ILLMProvider
    {
        public Task<LLMResponse> SendAsync(LLMRequest request)
        {
            string responseText = "Mock response received for: " + request.UserPrompt;

            return Task.FromResult(LLMResponse.Successful(responseText, request.RequestId));
        }
        public string GetProviderName()
        {
            return "Mock Provider";
        }
        public ValidationResult ValidateConfiguration()
        {
            return ValidationResult.Success();
        }
    }
}
