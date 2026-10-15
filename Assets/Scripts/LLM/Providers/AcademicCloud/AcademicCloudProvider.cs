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

        public AcademicCloudProvider(AcademicCloudConfig config, string apiKey)
        {
            this.config = config;
            this.apiKey = apiKey;
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

            if (request == null)
            {
                return LLMResponse.Failed("LLM request is null.", string.Empty, 0, 0);
            }

            ValidationResult requestValidation = request.Validate();

            if (!requestValidation.IsValid)
            {
                return LLMResponse.Failed(
                    requestValidation.GetSummary(),
                    request.RequestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }

            ValidationResult configValidation = ValidateConfiguration();

            if (!configValidation.IsValid)
            {
                return LLMResponse.Failed(
                    configValidation.GetSummary(),
                    request.RequestId,
                    0,
                    stopwatch.ElapsedMilliseconds);
            }

            string requestId = request.RequestId;

            try
            {
                AcademicCloudChatRequest payload = BuildPayload(request); // payload = the actual data sent between client and server

                string json = JsonUtility.ToJson(payload);
                byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

                string endpoint = config.BaseUrl.TrimEnd('/') + "/chat/completions";

                using (var webRequest = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)) // using block disposes of the request after its response has been processed
                {
                    webRequest.uploadHandler = new UploadHandlerRaw(body); // tells the request what data to send out
                    webRequest.downloadHandler = new DownloadHandlerBuffer(); // allocates a block of memory (a buffer) to catch and store the incoming data that the server will eventually send back
                    webRequest.timeout = config.TimeoutSeconds;

                    webRequest.SetRequestHeader( 
                        "Authorization",
                        "Bearer " + this.apiKey); // HTTP headers are metadata sent alongside the request to give the server context

                    webRequest.SetRequestHeader(
                        "Content-Type",
                        "application/json");

                    webRequest.SetRequestHeader(
                        "Accept",
                        "application/json");

                    UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();

                    var completion = new TaskCompletionSource<bool>();

                    operation.completed += _ => completion.TrySetResult(true); // event subscription, adding anonymous (no function name, discard arguments) lambda function (_ =>)

                    // Wait until Unity's asynchronous HTTP operation finishes
                    await completion.Task;

                    stopwatch.Stop();

                    string responseBody = webRequest.downloadHandler?.text ?? string.Empty;

                    if (webRequest.result != UnityWebRequest.Result.Success)
                    {
                        string errorMessage = ExtractErrorMessage(responseBody, webRequest.error);

                        UnityEngine.Debug.LogWarning(
                            $"Academic Cloud request failed. " +
                            $"HTTP status: {webRequest.responseCode}. " +
                            $"Error: {errorMessage}");

                        return LLMResponse.Failed(
                            errorMessage,
                            requestId,
                            (int)webRequest.responseCode,
                            stopwatch.ElapsedMilliseconds);
                    }

                    AcademicCloudChatResponse parsedResponse;

                    try
                    {
                        parsedResponse = JsonUtility.FromJson<AcademicCloudChatResponse>(responseBody);
                    }
                    catch (Exception exception)
                    {
                        return LLMResponse.Failed(
                            "Could not parse Academic Cloud response: " +
                            exception.Message,
                            requestId,
                            (int)webRequest.responseCode,
                            stopwatch.ElapsedMilliseconds);
                    }

                    if (parsedResponse == null)
                    {
                        return LLMResponse.Failed(
                            "Academic Cloud returned an empty or invalid response.",
                            requestId,
                            (int)webRequest.responseCode,
                            stopwatch.ElapsedMilliseconds);
                    }

                    if (parsedResponse.choices == null ||
                        parsedResponse.choices.Count == 0 ||
                        parsedResponse.choices[0].message == null)
                    {
                        return LLMResponse.Failed(
                            "Academic Cloud response contained no message.",
                            requestId,
                            (int)webRequest.responseCode,
                            stopwatch.ElapsedMilliseconds);
                    }

                    string generatedText = parsedResponse.choices[0].message.content;

                    if (string.IsNullOrWhiteSpace(generatedText))
                    {
                        return LLMResponse.Failed(
                            "Academic Cloud returned an empty message.",
                            requestId,
                            (int)webRequest.responseCode,
                            stopwatch.ElapsedMilliseconds);
                    }

                    return LLMResponse.Successful(
                        generatedText,
                        requestId,
                        (int)webRequest.responseCode,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception exception)
            {
                stopwatch.Stop();

                UnityEngine.Debug.LogWarning($"Academic Cloud request failed: {exception.Message}");

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
                catch
                {
                    // Fall through to the generic HTTP error
                }
                return responseBody;
            }

            return string.IsNullOrWhiteSpace(fallbackError) ? "Unknown HTTP error." : fallbackError;
        }
    }
}
