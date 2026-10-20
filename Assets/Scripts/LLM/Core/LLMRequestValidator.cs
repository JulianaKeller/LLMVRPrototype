using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace LLMVRPrototype
{
    public class LLMRequestValidator
    {
        public ValidationResult Validate(LLMRequest request)
        {
            var result = new ValidationResult();

            if (request == null)
            {
                result.AddError("LLM request is null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(request.UserPrompt))
                result.AddError("The user prompt cannot be empty.");
            if (request.Temperature < 0f || request.Temperature > 2f)
                result.AddError("Temperature must be between 0 and 2.");
            if (request.MaxOutputTokens <= 0)
                result.AddError("MaxOutputTokens must be greater than zero.");
            if (string.IsNullOrWhiteSpace(request.RequestId))
                result.AddError("RequestId cannot be empty.");
            if (request.ConversationHistory == null)
                result.AddError("ConversationHistory cannot be null.");
            else
            {
                for (int i = 0;
                     i < request.ConversationHistory.Count;
                     i++)
                {
                    ChatMessage message =
                        request.ConversationHistory[i];

                    if (message == null)
                    {
                        result.AddError(
                            $"Conversation message {i} is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(message.Role))
                    {
                        result.AddError(
                            $"Conversation message {i} has no role.");
                    }
                    else if (
                        message.Role != "system" &&
                        message.Role != "user" &&
                        message.Role != "assistant")
                    {
                        result.AddError(
                            $"Conversation message {i} has an unsupported role.");
                    }

                    if (string.IsNullOrWhiteSpace(message.Content))
                    {
                        result.AddError(
                            $"Conversation message {i} has empty content.");
                    }
                }
            }
            return result;
        }
    }
}
