using UnityEngine;

namespace LLMVRPrototype
{
    // represents the standardized result returned by the service
    public class LLMResponse
    {
        public bool Success { get; private set; }
        public string Text { get; private set; }
        public string ErrorMessage { get; private set; }
        public long StatusCode { get; private set; }
        public string RequestId { get; private set; }
        public long ElapsedMilliseconds { get; private set; }

        // TODO extend LLMResponse with structured failure information so LLMService can decide whether a retry is appropriate.

        // Private constructor so that no LLMResponses with contradictory information (success with error) can be created
        private LLMResponse(bool success, string text, string errorMessage, long statusCode, string requestId, long elapsedMilliseconds)
        {
            Success = success;
            Text = text;
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
            RequestId = requestId;
            ElapsedMilliseconds = elapsedMilliseconds;
        }

        public static LLMResponse Successful(string text, string requestId, long elapsedMilliseconds = 0, long statusCode = 200)
        {
            return new LLMResponse(true, text ?? string.Empty, null, statusCode, requestId, elapsedMilliseconds);
        }

        public static LLMResponse Failed(string errorMessage, string requestId, long statusCode = 0, long elapsedMilliseconds = 0)
        {
            return new LLMResponse(false, string.Empty, errorMessage ?? "An unknown error occurred.", statusCode, requestId, elapsedMilliseconds);
        }
    }
}
