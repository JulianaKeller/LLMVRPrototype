using UnityEngine;

namespace LLMVRPrototype
{
    public class LLMLogger
    {
        public static bool EnableDebugLogs = true;

        public static void Log(string message)
        {
            if (EnableDebugLogs)
            {
                Debug.Log("[LLM] " + message);
            }
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning("[LLM] " + message);
        }

        public static void LogError(string message)
        {
            Debug.LogError("[LLM] " + message);
        }
    }
}
