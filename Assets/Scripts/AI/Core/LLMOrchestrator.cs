using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LLMVRPrototype
{
    // Alias LLMService
    // coordinates request validation, provider communication, error handling, and response delivery.
    public class LLMOrchestrator
    {
        private readonly ILLMProvider provider;

        public LLMOrchestrator(ILLMProvider provider)
        {
            // dependency injection: provider is supplied when the service is created
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            this.provider = provider;
        }

        public async Task<LLMResponse> SendAsync(LLMRequest request)
        {
            if (request == null)
            {
                return LLMResponse.Failed("The request cannot be null.", string.Empty);
            }
            ValidationResult requestValidation = request.Validate();
            if (!requestValidation.IsValid)
            {
                return LLMResponse.Failed(requestValidation.GetErrorSummary(), request.RequestId);
            }
            ValidationResult providerValidation = provider.ValidateConfiguration();
            if (!providerValidation.IsValid)
            {
                return LLMResponse.Failed("Provider configuration is invalid: " + providerValidation.GetErrorSummary(), request.RequestId);
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                LLMResponse response = await provider.SendAsync(request);
                stopwatch.Stop();

                if (response == null)
                {
                    return LLMResponse.Failed("The provider returned a null response.", request.RequestId, elapsedMilliseconds: stopwatch.ElapsedMilliseconds);
                }
                if (response.RequestId != request.RequestId)
                {
                    Debug.LogWarning($"LLM request ID mismatch. Expected {request.RequestId}, " + $"received {response.RequestId}.");
                }
                if (response.Success)
                {
                    return LLMResponse.Successful(response.Text, request.RequestId, stopwatch.ElapsedMilliseconds, response.StatusCode);
                }
                return LLMResponse.Failed(response.ErrorMessage, request.RequestId, response.StatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop(); 
                return LLMResponse.Failed("The LLM request was cancelled.", request.RequestId, elapsedMilliseconds: stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                Debug.LogError($"LLM request failed. Request ID: {request.RequestId}\n" + exception);
                return LLMResponse.Failed("An unexpected error occurred while communicating with the LLM.", request.RequestId, elapsedMilliseconds: stopwatch.ElapsedMilliseconds);
            }
        }

        public string GetProviderName()
        {
            return provider.GetProviderName();
        }
    }
}
