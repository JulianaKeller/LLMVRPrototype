using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    // DTO = class used to represent data being transferred between two systems
    // DTO classes here represent the JSON structures that Unity send to and receives from the API
    public class AcademicCloudDtos
    {
        [Serializable]
        internal sealed class AcademicCloudChatRequest
        {
            public string model;
            public List<AcademicCloudMessage> messages;
            public float temperature;
            public int max_tokens;
        }

        [Serializable]
        internal sealed class AcademicCloudMessage
        {
            public string role;
            public string content;

            public AcademicCloudMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        [Serializable]
        internal sealed class AcademicCloudChatResponse
        {
            public List<AcademicCloudChoice> choices;
            public AcademicCloudError error;
        }

        [Serializable]
        internal sealed class AcademicCloudChoice
        {
            public AcademicCloudMessage message;
            public string finish_reason;
        }

        [Serializable]
        internal sealed class AcademicCloudError
        {
            public string message;
            public string type;
            public string code;
        }
    }
}
