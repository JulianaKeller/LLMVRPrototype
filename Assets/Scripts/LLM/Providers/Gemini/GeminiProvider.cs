using Project.LLM.Providers.Gemini;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using static LLMVRPrototype.GeminiDtos;

namespace LLMVRPrototype
{
    public sealed class GeminiProvider : ILLMProvider
    {
        private readonly GeminiConfig config;
        private readonly string apiKey;
        private readonly ILLMTransport transport;

        public GeminiProvider(
            GeminiConfig config,
            string apiKey,
            ILLMTransport transport = null)
        {
            this.config = config;
            this.apiKey = apiKey;
            this.transport = transport ?? new UnityWebRequestTransport();
        }

        public string GetProviderName()
        {
            return "Gemini";
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult();

            if (config == null)
            {
                result.AddError("Gemini configuration is missing.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
                result.AddError("Gemini API key is missing.");

            if (string.IsNullOrWhiteSpace(config.Model))
                result.AddError("Gemini model name is missing.");

            if (!Uri.TryCreate(
                    config.BaseUrl,
                    UriKind.Absolute,
                    out Uri baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
            {
                result.AddError( "Gemini base URL must be a valid HTTPS URL.");
            }

            if (config.TimeoutSeconds <= 0)
                result.AddError("Timeout must be greater than zero.");

            return result;
        }

        public async Task<LLMResponse> SendAsync(LLMRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            string requestId = request?.RequestId ?? string.Empty;

            if (request == null)
            {
                return LLMResponse.Failed(
                    "LLM request is null.",
                    string.Empty,
                    0,
                    0);
            }

            ValidationResult validation = ValidateConfiguration();

            if (!validation.IsValid)
            {
                return LLMResponse.Failed(
                    validation.GetSummary(),
                    requestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }

            try
            {
                GeminiGenerateRequest payload = BuildPayload(request);
                string json = JsonUtility.ToJson(payload);

                string endpoint = config.BaseUrl.TrimEnd('/') + "/models/" + config.Model + ":generateContent";

                var headers = new Dictionary<string, string>
                {
                    { "x-goog-api-key", apiKey }
                };

                HttpRequestData httpRequest =
                    LLMHttpRequestFactory.CreateJsonPost(
                        endpoint,
                        json,
                        config.TimeoutSeconds,
                        headers);

                HttpResponseData httpResponse = await transport.SendAsync(httpRequest);

                stopwatch.Stop();

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
                    string message = !string.IsNullOrWhiteSpace(
                        httpResponse.Error)
                        ? httpResponse.Error
                        : "The HTTP request failed.";

                    LLMLogger.LogWarning($"Gemini request {requestId} failed at the transport layer.");

                    return LLMResponse.Failed(
                        message,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                if (!httpResponse.IsHttpSuccess)
                {
                    string message = ExtractErrorMessage(
                        httpResponse.Body,
                        httpResponse.Error);

                    LLMLogger.LogWarning($"Gemini returned HTTP {httpResponse.StatusCode} for request {requestId}.");

                    return LLMResponse.Failed(
                        message,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                GeminiGenerateResponse parsedResponse;

                try
                {
                    parsedResponse = JsonUtility.FromJson<GeminiGenerateResponse>(httpResponse.Body);
                }
                catch (Exception exception)
                {
                    LLMLogger.LogError($"Could not parse Gemini response for request {requestId}: {exception.Message}");

                    return LLMResponse.Failed(
                        "Could not parse Gemini response: " +
                        exception.Message,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                if (parsedResponse == null)
                {
                    return LLMResponse.Failed(
                        "Gemini returned an empty or invalid response.",
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                if (parsedResponse.candidates == null ||
                    parsedResponse.candidates.Count == 0 ||
                    parsedResponse.candidates[0] == null ||
                    parsedResponse.candidates[0].content == null ||
                    parsedResponse.candidates[0].content.parts == null)
                {
                    string reason = parsedResponse.promptFeedback?.blockReason;

                    string message = !string.IsNullOrWhiteSpace(reason)
                        ? $"Gemini did not return a candidate. Block reason: {reason}"
                        : "Gemini response contained no text candidate.";

                    return LLMResponse.Failed(
                        message,
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                var textBuilder = new System.Text.StringBuilder();

                foreach (GeminiPart part in parsedResponse.candidates[0].content.parts)
                {
                    if (!string.IsNullOrEmpty(part?.text))
                        textBuilder.Append(part.text);
                }

                string generatedText = textBuilder.ToString();

                if (string.IsNullOrWhiteSpace(generatedText))
                {
                    return LLMResponse.Failed(
                        "Gemini returned an empty message.",
                        requestId,
                        (int)httpResponse.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }

                LLMLogger.Log($"Gemini request {requestId} succeeded in {stopwatch.ElapsedMilliseconds} ms.");

                return LLMResponse.Successful(
                    generatedText,
                    requestId,
                    (int)httpResponse.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                // Never log the API key or authorization headers.
                LLMLogger.LogError($"Gemini request {requestId} failed: {exception.Message}");

                return LLMResponse.Failed(
                    "Gemini request failed: " + exception.Message,
                    requestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private GeminiGenerateRequest BuildPayload(LLMRequest request)
        {
            var payload = new GeminiGenerateRequest
            {
                contents = new List<GeminiContent>(),
                generationConfig = new GeminiGenerationConfig
                {
                    temperature = request.Temperature,
                    maxOutputTokens = request.MaxOutputTokens
                }
            };

            var systemParts = new List<GeminiPart>();

            foreach (ChatMessage message in request.GetMessages())
            {
                if (message == null ||
                    string.IsNullOrWhiteSpace(message.Content))
                {
                    continue;
                }

                string role = message.Role?.Trim().ToLowerInvariant();

                if (role == "system" || role == "developer")
                {
                    systemParts.Add(new GeminiPart(message.Content));
                    continue;
                }

                string geminiRole;

                if (role == "assistant" || role == "model")
                    geminiRole = "model";
                else if (role == "user")
                    geminiRole = "user";
                else
                    throw new InvalidOperationException($"Unsupported chat message role '{message.Role}'.");

                payload.contents.Add(new GeminiContent
                {
                    role = geminiRole,
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart(message.Content)
                    }
                });
            }

            if (systemParts.Count > 0)
            {
                payload.systemInstruction = new GeminiSystemInstruction
                {
                    parts = systemParts
                };
            }

            if (payload.contents.Count == 0)
            {
                throw new InvalidOperationException("The request must contain at least one user or assistant message.");
            }

            return payload;
        }

        private static string ExtractErrorMessage(
            string responseBody,
            string fallbackError)
        {
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    var errorResponse = JsonUtility.FromJson<GeminiGenerateResponse>(responseBody);

                    if (errorResponse?.error != null &&!string.IsNullOrWhiteSpace(errorResponse.error.message))
                    {
                        return errorResponse.error.message;
                    }
                }
                catch (Exception)
                {
                    // Fall through to the response body.
                }

                return responseBody;
            }

            return string.IsNullOrWhiteSpace(fallbackError)
                ? "Unknown Gemini HTTP error."
                : fallbackError;
        }
    }
}