using Project.LLM.Providers.AcademicCloud;
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
            LLMProviderType providerType,
            AcademicCloudConfig academicCloudConfig,
            string apiKey)
        {
            switch (providerType)
            {
                case LLMProviderType.AcademicCloud:
                    return new AcademicCloudProvider(academicCloudConfig, apiKey);

                default:
                    throw new System.NotSupportedException("Unsupported LLM provider: " + providerType);
            }
        }
    }
}
