using UnityEngine;
using System;

namespace LLMVRPrototype
{
    [Serializable]

    // Represents one message in a conversation
    // Allows preserving conversation context across multiple requests
    public class ChatMessage
    {

        public string Role;
        public string Content;

        public ChatMessage(string role, string content)
        {
            Role = role; 
            Content = content;
        }

        public ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Role))
            {
                return ValidationResult.Failure("A message role is required.");
            }
            if(Content == null)
            {
                return ValidationResult.Failure("Message content cannot be null.");
            }
            string normalizedRole = Role.Trim().ToLowerInvariant();

            if(normalizedRole != "system" && normalizedRole != "user" && normalizedRole != "assistant")
            {
                return ValidationResult.Failure($"Unsupported message role: {Role}");
            }

            return ValidationResult.Success();
        }
    }
}
