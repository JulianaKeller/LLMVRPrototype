using UnityEngine;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    public class LLMRequest
    {
        public string SystemPrompt { get; set; } // Defines the model's role and instructions, e.g. "You are an assistant that helps design immersive virtual environments."
        public string UserPrompt { get; set; } // Contains the user's current instruction
        public List<ChatMessage> ConversationHistory { get; set; }
        public float Temperature { get; set; } // generation variability for providers that support the parameter (Different providers may have different supported ranges or restrictions, so the provider adapter must validate provider-specific requirements)
        public int MaxOutputTokens { get; set; }
        public string RequestId { get; set; }

        public LLMRequest(string userPrompt, string systemPrompt = "")
        {
            UserPrompt = userPrompt;
            SystemPrompt = systemPrompt;
            ConversationHistory = new List<ChatMessage>();
            Temperature = 0.7f;
            MaxOutputTokens = 1024;
            RequestId = System.Guid.NewGuid().ToString();
        }

        // combines the system prompt, conversation history, and current user prompt into a single ordered list
        // most chat APIs do not automatically remember previous requests
        public List<ChatMessage> GetMessages() { 
            var messages = new List<ChatMessage>(); 
            if (!string.IsNullOrWhiteSpace(SystemPrompt)) 
                messages.Add(new ChatMessage("system", SystemPrompt)); 
            messages.AddRange(ConversationHistory); 
            messages.Add(new ChatMessage("user", UserPrompt)); 
            return messages; 
        }
    }
}
