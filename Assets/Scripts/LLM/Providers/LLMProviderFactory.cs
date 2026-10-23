using LLMVRPrototype.LLM.Providers;
using Project.LLM.Providers.AcademicCloud;
using Project.LLM.Providers.Gemini;
using System;
using UnityEngine;

namespace LLMVRPrototype
{
    // TODO factory could use a provider registry or dependency-injection container

    public enum LLMProviderType
    {
        AcademicCloud
    }

    public static class LLMProviderFactory
    {
        public static ILLMProvider Create(
            ProviderConfig config,
            string apiKey)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty.", nameof(apiKey));

            switch (config)
            {
                case GeminiConfig geminiConfig:
                    return new GeminiProvider(geminiConfig, apiKey);

                case AcademicCloudConfig academicCloudConfig:
                    return new AcademicCloudProvider(
                        academicCloudConfig,
                        apiKey);

                default:
                    throw new NotSupportedException($"No provider is registered for config type '{config.GetType().FullName}'.");
            }
        }
    }
}
