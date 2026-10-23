using System.Collections.Generic;

namespace LLMVRPrototype
{
    public static class LLMHttpRequestFactory
    {
        public static HttpRequestData CreateJsonPost(
            string url,
            string body,
            int timeoutSeconds,
            IDictionary<string, string> additionalHeaders = null)
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Accept", "application/json" }
            };

            if (additionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in additionalHeaders)
                {
                    headers[header.Key] = header.Value;
                }
            }

            return new HttpRequestData(
                url: url,
                method: "POST",
                body: body,
                headers: headers,
                timeoutSeconds: timeoutSeconds);
        }
    }
}