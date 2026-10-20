using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LLMVRPrototype
{
    /// <summary>
    /// Alias LLMService
    /// Coordinates request validation, conversation history, provider communication, and response delivery.
    /// </summary>
    public class LLMOrchestrator
    {
        private readonly ILLMProvider provider;
        private readonly LLMRequestValidator validator;
        private readonly ConversationManager conversationManager;
        private readonly RetryPolicy retryPolicy;

        public LLMOrchestrator(ILLMProvider provider, ConversationManager conversationManager, RetryPolicy retryPolicy = null)
        {
            // dependency injection: provider is supplied when the service is created
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            this.provider = provider;

            if (conversationManager == null)
            {
                throw new ArgumentNullException(nameof(conversationManager));
            }
            this.conversationManager = conversationManager;

            if (validator == null)
            {
                validator = new LLMRequestValidator();
            }

            this.retryPolicy = retryPolicy ?? new RetryPolicy();
        }

        public async Task<LLMResponse> SendAsync(LLMRequest request)
        {
            if (request == null)
            {
                return LLMResponse.Failed("The request cannot be null.", string.Empty);
            }
            ValidationResult requestValidation = validator.Validate(request);
            if (!requestValidation.IsValid)
            {
                return LLMResponse.Failed(requestValidation.GetErrorSummary(), request.RequestId);
            }
            ValidationResult providerValidation = provider.ValidateConfiguration();
            if (!providerValidation.IsValid)
            {
                return LLMResponse.Failed("Provider configuration is invalid: " + providerValidation.GetErrorSummary(), request.RequestId);
            }

            request.ConversationHistory = (System.Collections.Generic.List<ChatMessage>)conversationManager.GetHistory();

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                LLMResponse response = null; 
                int totalAttempts = retryPolicy.MaxRetries + 1;

                for (int attempt = 1; attempt <= totalAttempts; attempt++)
                {
                    Debug.Log($"LLM request {request.RequestId}: attempt {attempt}/{totalAttempts}.");

                    response = await provider.SendAsync(request);

                    if (response == null)
                    {
                        response = LLMResponse.Failed("The provider returned a null response.", request.RequestId, 0, 0);
                    }

                    if (response.Success)
                    {
                        break;
                    }

                    bool canRetry = retryPolicy.ShouldRetry(response, attempt);

                    if (!canRetry)
                    {
                        break;
                    }

                    int delayMilliseconds = retryPolicy.GetDelayMilliseconds(attempt);

                    Debug.LogWarning(
                        $"LLM request {request.RequestId} failed on " +
                        $"attempt {attempt}. Retrying in " +
                        $"{delayMilliseconds} ms. " +
                        $"HTTP status: {response.StatusCode}.");

                    await retryPolicy.WaitBeforeRetryAsync(attempt);
                }

                stopwatch.Stop();

                if (response == null)
                {
                    return LLMResponse.Failed(
                        "The provider did not return a response.",
                        request.RequestId,
                        0,
                        stopwatch.ElapsedMilliseconds);
                }

                if (response.RequestId != request.RequestId)
                {
                    Debug.LogWarning(
                        $"LLM request ID mismatch. Expected " +
                        $"{request.RequestId}, received {response.RequestId}.");
                }

                if (!response.Success)
                {
                    // All attempts failed. Do not modify conversation history.
                    return LLMResponse.Failed(
                        response.ErrorMessage,
                        request.RequestId,
                        response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                // 4. Update conversation history only after success.

                conversationManager.AddUserMessage(request.UserPrompt);
                conversationManager.AddAssistantMessage(response.Text);

                return LLMResponse.Successful(
                    response.Text,
                    request.RequestId,
                    response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();

                return LLMResponse.Failed(
                    "The LLM request was cancelled.",
                    request.RequestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                Debug.LogError(
                    $"LLM request failed. Request ID: {request.RequestId}\n" +
                    exception);

                return LLMResponse.Failed(
                    "An unexpected error occurred while communicating with the LLM.",
                    request.RequestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        public string GetProviderName()
        {
            return provider.GetProviderName();
        }
    }
}
