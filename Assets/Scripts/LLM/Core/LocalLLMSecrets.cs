using System;
using System.IO;
using UnityEngine;

namespace Project.LLM.Core
{
    [Serializable]
    internal sealed class LocalLLMSecretsData
    {
        public string academicCloudApiKey;
        public string geminiApiKey;
    }

    public class LocalLLMSecrets
    {
        public static string LoadAcademicCloudApiKey()
        {
            return LoadApiKey("academicCloud");
        }

        public static string LoadGeminiApiKey()
        {
            return LoadApiKey("gemini");
        }

        private static string LoadApiKey(string providerName)
        {
            // The Unity project root is the parent of Application.dataPath
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;

            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                Debug.LogError("Could not determine the Unity project root.");

                return null;
            }

            string configPath = Path.Combine(projectRoot, "LocalConfig", "llm.local.json");

            if (!File.Exists(configPath))
            {
                Debug.LogWarning("Local LLM configuration file was not found.");

                return null;
            }

            try
            {
                string json = File.ReadAllText(configPath);

                LocalLLMSecretsData data = JsonUtility.FromJson<LocalLLMSecretsData>(json);

                if (data == null)
                {
                    Debug.LogWarning("The local LLM configuration contains no data.");
                    return null;
                }
                switch (providerName)
                {
                    case "academicCloud":
                        if (string.IsNullOrWhiteSpace(data.academicCloudApiKey))
                        {
                            Debug.LogWarning("The local LLM configuration contains no AcademicCloud API Key.");
                            return null;
                        }
                        return data.academicCloudApiKey;
                    case "gemini":
                        if (string.IsNullOrWhiteSpace(data.geminiApiKey))
                        {
                            Debug.LogWarning("The local LLM configuration contains no Gemini API Key.");
                            return null;
                        }
                        return data.geminiApiKey;
                }

                return null;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to load local LLM configuration: " + exception.Message);

                return null;
            }
        }
    }
}
