using UnityEngine;
using System.Threading.Tasks;

namespace LLMVRPrototype
{
    public interface ILLMProvider
    {
        Task<LLMResponse> SendAsync(LLMRequest request); // for sending requests
        string GetProviderName(); // for identifying the provider
        ValidationResult ValidateConfiguration();
    }
}
