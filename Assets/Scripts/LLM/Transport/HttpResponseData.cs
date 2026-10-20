using UnityEngine;

namespace LLMVRPrototype
{
    public sealed class HttpResponseData
    {
        public long StatusCode { get; }
        public string Body { get; }
        public string Error { get; }
        public bool TransportSucceeded { get; }

        // Note: an HTTP 401 response means the transport successfully received an HTTP response, but the server rejected the request. A DNS failuremay produce no HTTP status code at all.

        public bool IsHttpSuccess =>
            StatusCode >= 200 && StatusCode < 300;

        public HttpResponseData(
            long statusCode,
            string body,
            string error,
            bool transportSucceeded)
        {
            StatusCode = statusCode;
            Body = body;
            Error = error;
            TransportSucceeded = transportSucceeded;
        }
    }
}
