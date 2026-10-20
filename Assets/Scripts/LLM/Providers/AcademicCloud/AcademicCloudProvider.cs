using Project.LLM.Providers.AcademicCloud;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static LLMVRPrototype.AcademicCloudDtos;

namespace LLMVRPrototype
{
    public sealed class AcademicCloudProvider : ILLMProvider
    {
        private readonly AcademicCloudConfig config;
        private readonly string apiKey;
        private readonly ILLMTransport transport;

        public AcademicCloudProvider(AcademicCloudConfig config, string apiKey, ILLMTransport transport = null)
        {
            this.config = config;
            this.apiKey = apiKey;

            this.transport = transport ?? new UnityWebRequestTransport();
            // default transport makes nomal usage in Unity simple, optionally a fake transport can be provided for unit testing
        }

        public string GetProviderName()
        {
            return "Academic Cloud";
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            if(config == null)
            {
                result.AddError("Academic Cloud configuration is missing.");
                return result;
            }
            if (string.IsNullOrWhiteSpace(this.apiKey))
            {
                result.AddError("Academic Cloud API key is missing.");
            }
            if (string.IsNullOrWhiteSpace(config.Model))
            {
                result.AddError("Academic Cloud model name is missing.");
            }
            if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out Uri baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            {
                result.AddError("Academic Cloud base URL must be a valid HTTPS URL.");
            }
            if (config.TimeoutSeconds <= 0)
            {
                result.AddError("Timeout must be greater than zero.");
            }
            return result;
        }

        public async Task<LLMResponse> SendAsync(LLMRequest request)
        {
            var stopwatch = Stopwatch.StartNew();

            string requestId = request?.RequestId ?? string.Empty;

            if (request == null)
            {
                return LLMResponse.Failed("LLM request is null.", string.Empty, 0, 0);
            }

            /* Validation not necessary as this is done in Orchestrator before calling SendAsync
            ValidationResult requestValidation = request.Validate();

            if (!requestValidation.IsValid)
            {
                return LLMResponse.Failed(
                    requestValidation.GetSummary(),
                    request.RequestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }*/

            ValidationResult configValidation = ValidateConfiguration();

            if (!configValidation.IsValid)
            {
                return LLMResponse.Failed(
                    configValidation.GetSummary(),
                    request.RequestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }

            try
            {
                // 1. Convert the application request into the Academic Cloud-specific request DTO

                AcademicCloudChatRequest payload = BuildPayload(request); // payload = the actual data sent between client and server

                string json = JsonUtility.ToJson(payload);

                // 2. Build a provider-independent HTTP request

                string endpoint = config.BaseUrl.TrimEnd('/') + "/chat/completions";

                var headers = new Dictionary<string, string>
                {
                    {
                        "Authorization",
                        "Bearer " + apiKey
                    },
                    {
                        "Content-Type",
                        "application/json"
                    },
                    {
                        "Accept",
                        "application/json"
                    }
                };

                var httpRequest = new HttpRequestData(
                    url: endpoint,
                    method: "POST",
                    body: json,
                    headers: headers,
                    timeoutSeconds: config.TimeoutSeconds);

                // 3. Execute the HTTP request through the transport

                HttpResponseData httpResponse = await transport.SendAsync(httpRequest);

                stopwatch.Stop();

                // 4. Handle transport failures

                if (httpResponse == null)
                {
                    return LLMResponse.Failed(
                        "HTTP transport returned no response.",
                        requestId,
                        0,
                        stopwatch.ElapsedMilliseconds);
                }

                if (!httpResponse.TransportSucceeded)
                {
                    string errorMessage = !string.IsNullOrWhiteSpace(httpResponse.Error)
                            ? httpResponse.Error
                            : "The HTTP request failed.";

                    LLMLogger.LogWarning($"Request {requestId} failed at the transport layer.");

                    return LLMResponse.Failed(
                        errorMessage,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                // 5. Handle unsuccessful HTTP status codes

                if (!httpResponse.IsHttpSuccess)
                {
                    string errorMessage = ExtractErrorMessage(httpResponse.Body, httpResponse.Error);

                    LLMLogger.LogWarning($"Academic Cloud returned HTTP {httpResponse.StatusCode} for request {requestId}.");

                    return LLMResponse.Failed(
                        errorMessage,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                // 6. Parse the successful API response

                AcademicCloudChatResponse parsedResponse;

                try
                {
                    parsedResponse = JsonUtility.FromJson<AcademicCloudChatResponse>(httpResponse.Body);
                }
                catch (Exception exception)
                {
                    LLMLogger.LogError($"Could not parse Academic Cloud response for request {requestId}: {exception.Message}");

                    return LLMResponse.Failed(
                        "Could not parse Academic Cloud response: " +
                        exception.Message,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                if (parsedResponse == null)
                {
                    return LLMResponse.Failed(
                        "Academic Cloud returned an empty or invalid response.",
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                if (parsedResponse.choices == null ||
                    parsedResponse.choices.Count == 0 ||
                    parsedResponse.choices[0] == null ||
                    parsedResponse.choices[0].message == null)
                {
                    return LLMResponse.Failed(
                        "Academic Cloud response contained no message.",
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                string generatedText = parsedResponse.choices[0].message.content;

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    return LLMResponse.Failed(
                        "Academic Cloud returned an empty message.",
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                // 7. Return a provider-independent response

                LLMLogger.Log($"Request {requestId} succeeded in {stopwatch.ElapsedMilliseconds} ms.");

                return LLMResponse.Successful(
                    generatedText,
                    requestId,
                    (int)httpResponse.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                // Do not log the API key, authorization headers, or complete request payload
                LLMLogger.LogError($"Request {requestId} failed: {exception.Message}");

                return LLMResponse.Failed(
                    "Request failed: " + exception.Message,
                    requestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        private AcademicCloudChatRequest BuildPayload(LLMRequest request)
        {
            var messages = new List<AcademicCloudMessage>();

            // Use LLMRequest.GetMessages() to preserve the existing system/history/user message ordering
            foreach (ChatMessage message in request.GetMessages())
            {
                if (message == null)
                {
                    continue;
                }

                messages.Add(new AcademicCloudMessage(message.Role, message.Content));
            }

            return new AcademicCloudChatRequest
            {
                model = config.Model,
                messages = messages,
                temperature = request.Temperature,
                max_tokens = request.MaxOutputTokens
            };
        }

        private static string ExtractErrorMessage(string responseBody, string fallbackError)
        {
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    var errorResponse = JsonUtility.FromJson<AcademicCloudChatResponse>(responseBody);

                    if (errorResponse?.error != null && !string.IsNullOrWhiteSpace(errorResponse.error.message))
                    {
                        return errorResponse.error.message;
                    }
                }
                catch (Exception)
                {
                    // Fall through to the generic HTTP error
                }
                return responseBody;
            }

            return string.IsNullOrWhiteSpace(fallbackError) ? "Unknown HTTP error." : fallbackError;
        }
    }
}
