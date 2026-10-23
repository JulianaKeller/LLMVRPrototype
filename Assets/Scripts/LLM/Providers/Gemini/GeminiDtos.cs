using System;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    // DTO = class used to represent data being transferred between two systems
    // DTO classes here represent the JSON structures that Unity send to and receives from the API
    public static class GeminiDtos
    {
        [Serializable]
        internal sealed class GeminiGenerateRequest
        {
            public GeminiSystemInstruction systemInstruction;
            public List<GeminiContent> contents;
            public GeminiGenerationConfig generationConfig;
        }

        [Serializable]
        internal sealed class GeminiContent
        {
            public string role;
            public List<GeminiPart> parts;
        }

        [Serializable]
        internal sealed class GeminiSystemInstruction
        {
            public List<GeminiPart> parts;
        }

        [Serializable]
        internal sealed class GeminiPart
        {
            public string text;

            public GeminiPart(string text)
            {
                this.text = text;
            }
        }

        [Serializable]
        internal sealed class GeminiGenerationConfig
        {
            public float temperature;
            public int maxOutputTokens;
        }

        [Serializable]
        internal sealed class GeminiGenerateResponse
        {
            public List<GeminiCandidate> candidates;
            public GeminiPromptFeedback promptFeedback;
            public GeminiError error;
        }

        [Serializable]
        internal sealed class GeminiCandidate
        {
            public GeminiContent content;
            public string finishReason;
        }

        [Serializable]
        internal sealed class GeminiPromptFeedback
        {
            public string blockReason;
        }

        [Serializable]
        internal sealed class GeminiError
        {
            public int code;
            public string message;
            public string status;
        }
    }
}