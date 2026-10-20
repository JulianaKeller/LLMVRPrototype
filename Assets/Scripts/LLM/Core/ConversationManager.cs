using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    public sealed class ConversationManager
    {
        private readonly List<ChatMessage> messages = new List<ChatMessage>();

        // TODO keep system prompt separate from conversation messages so that it is never trimmed

        public IReadOnlyList<ChatMessage> Messages => messages.AsReadOnly();

        public void AddMessage(string role, string content)
        {
            if (string.IsNullOrWhiteSpace(role))
                return;

            if (string.IsNullOrWhiteSpace(content))
                return;

            messages.Add(new ChatMessage(role, content));
        }

        public void AddUserMessage(string content)
        {
            AddMessage("user", content);
        }

        public void AddAssistantMessage(string content)
        {
            AddMessage("assistant", content);
        }

        public void Clear()
        {
            messages.Clear();
        }

        public IReadOnlyList<ChatMessage> GetHistory()
        {
            return messages;
        }

        public void TrimOldestMessages(int maxMessages)
        {
            int limit = System.Math.Max(0, maxMessages);

            while (messages.Count > limit)
            {
                messages.RemoveAt(0);
            }
        }
    }
}
