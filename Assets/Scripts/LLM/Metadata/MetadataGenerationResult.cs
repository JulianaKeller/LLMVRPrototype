using UnityEngine;

namespace LLMVRPrototype
{
    public sealed class MetadataGenerationResult<T>
    {
        public bool Success { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }

        private MetadataGenerationResult()
        {
        }

        public static MetadataGenerationResult<T> Successful(T data)
        {
            return new MetadataGenerationResult<T>
            {
                Success = true,
                Data = data,
                ErrorMessage = null
            };
        }

        public static MetadataGenerationResult<T> Failed(string errorMessage)
        {
            return new MetadataGenerationResult<T>
            {
                Success = false,
                Data = default(T),
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "Metadata generation failed."
                    : errorMessage
            };
        }
    }
}
