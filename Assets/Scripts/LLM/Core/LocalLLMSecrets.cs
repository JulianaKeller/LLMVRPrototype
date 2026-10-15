using System;
using System.IO;
using UnityEngine;

namespace Project.LLM.Core
{
    [Serializable]
    internal sealed class LocalLLMSecretsData
    {
        public string academicCloudApiKey;
    }

    public class LocalLLMSecrets
    {
        public static string LoadAcademicCloudApiKey()
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

                if (data == null || string.IsNullOrWhiteSpace(data.academicCloudApiKey))
                {
                    Debug.LogWarning("The local LLM configuration contains no API key.");

                    return null;
                }

                return data.academicCloudApiKey;
            }
            catch (Exception exception)
            {
                Debug.LogError("Failed to load local LLM configuration: " + exception.Message);

                return null;
            }
        }
    }
}
