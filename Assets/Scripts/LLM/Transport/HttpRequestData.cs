using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    // represents an outgoing HTTP request without depending on Unity's networking API
    public sealed class HttpRequestData
    {
        public string Url { get; }
        public string Method { get; }
        public string Body { get; }
        public Dictionary<string, string> Headers { get; } // stores values such as Authorization and Content-Type
        public int TimeoutSeconds { get; }

        public HttpRequestData(
            string url,
            string method,
            string body,
            Dictionary<string, string> headers,
            int timeoutSeconds = 90)
        {
            Url = url;
            Method = method;
            Body = body;
            Headers = headers ?? new Dictionary<string, string>();
            TimeoutSeconds = timeoutSeconds;
        }
    }
}
