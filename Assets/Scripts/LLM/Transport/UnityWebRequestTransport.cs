using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LLMVRPrototype
{
    public sealed class UnityWebRequestTransport : ILLMTransport
    {
        public async Task<HttpResponseData> SendAsync(HttpRequestData request)
        {
            if (request == null)
            {
                return new HttpResponseData(
                    0,
                    string.Empty,
                    "HTTP request is null.",
                    false);
            }

            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return new HttpResponseData(
                    0,
                    string.Empty,
                    "HTTP request URL is empty.",
                    false);
            }

            try
            {
                using (var webRequest = new UnityWebRequest(request.Url, request.Method)) // using disposes of the web request after completion
                {
                    byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(request.Body ?? string.Empty);

                    if (request.Method == UnityWebRequest.kHttpVerbPOST ||
                        request.Method == UnityWebRequest.kHttpVerbPUT ||
                        request.Method == "PATCH")
                    {
                        webRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
                    }

                    webRequest.downloadHandler = new DownloadHandlerBuffer();

                    webRequest.timeout = Math.Max(1, request.TimeoutSeconds);

                    if (request.Headers != null)
                    {
                        foreach (var header in request.Headers)
                        {
                            webRequest.SetRequestHeader(header.Key, header.Value);
                        }
                    }

                    var operation = webRequest.SendWebRequest();

                    var completion = new TaskCompletionSource<bool>();

                    operation.completed += _ => completion.TrySetResult(true);

                    await completion.Task;

                    string responseBody = webRequest.downloadHandler?.text ?? string.Empty;

                    bool transportSucceeded =
                        webRequest.result !=
                        UnityWebRequest.Result.ConnectionError &&
                        webRequest.result !=
                        UnityWebRequest.Result.DataProcessingError;

                    return new HttpResponseData(
                        webRequest.responseCode,
                        responseBody,
                        webRequest.error,
                        transportSucceeded);
                }
            }
            catch (Exception exception)
            {
                return new HttpResponseData(
                    0,
                    string.Empty,
                    exception.Message,
                    false);
            }
        }
    }
}
