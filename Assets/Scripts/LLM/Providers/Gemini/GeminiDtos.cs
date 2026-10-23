using System;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    // DTO = class used to represent data being transferred between two systems
    // DTO classes here represent the JSON structures that Unity send to and receives from the API
    public static class GeminiDtos
    {
        /// <summary>
        /// Represents a Gemini generateContent request, including optional system instructions, conversation messages, and generation settings.
        /// </summary>
        [Serializable]
        internal sealed class GeminiGenerateRequest
        {
            public GeminiSystemInstruction systemInstruction;
            public List<GeminiContent> contents;
            public GeminiGenerationConfig generationConfig;
        }

        /// <summary>
        /// Represents one message in the conversation. The role identifies the speaker, such as "user" or "model", and parts contain the message content.
        /// </summary>
        [Serializable]
        internal sealed class GeminiContent
        {
            public string role;
            public List<GeminiPart> parts;
        }

        /// <summary>
        /// Holds system-level instructions that guide the model's behavior across the request.
        /// </summary>
        [Serializable]
        internal sealed class GeminiSystemInstruction
        {
            public List<GeminiPart> parts;
        }

        /// <summary>
        /// Represents a piece of message content. This implementation uses text parts.
        /// </summary>
        [Serializable]
        internal sealed class GeminiPart
        {
            public string text;

            public GeminiPart(string text)
            {
                this.text = text;
            }
        }

        /// <summary>
        /// Specifies how Gemini should generate its response, including randomness and the maximum output token count.
        /// </summary>
        [Serializable]
        internal sealed class GeminiGenerationConfig
        {
            public float temperature;
            public int maxOutputTokens;
        }

        /// <summary>
        /// Represents the top-level response returned by Gemini. Contains generated candidates, prompt feedback, or API error details.
        /// </summary>
        [Serializable]
        internal sealed class GeminiGenerateResponse
        {
            public List<GeminiCandidate> candidates;
            public GeminiPromptFeedback promptFeedback;
            public GeminiError error;
        }

        /// <summary>
        /// Represents one possible model response. Contains the generated content and the reason generation ended.
        /// </summary>
        [Serializable]
        internal sealed class GeminiCandidate
        {
            public GeminiContent content;
            public string finishReason;
        }

        /// <summary>
        /// Contains feedback about whether Gemini blocked the prompt, for example because of safety restrictions.
        /// </summary>
        [Serializable]
        internal sealed class GeminiPromptFeedback
        {
            public string blockReason;
        }

        /// <summary>
        /// Represents an API error, including its HTTP-like status code, human-readable message, and status identifier.
        /// </summary>
        [Serializable]
        internal sealed class GeminiError
        {
            public int code;
            public string message;
            public string status;
        }
    }
}