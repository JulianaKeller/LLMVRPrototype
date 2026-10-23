using LLMVRPrototype.LLM.Providers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace LLMVRPrototype.EditorTools
{
    public class DatabaseEditorWindow : EditorWindow
    {
        protected const string ConfigPreferenceKey = "LLMVRPrototype.TagEditor.SelectedProviderConfigPath";
        protected ObjectField apiConfigField;
        protected ProviderConfig selectedConfig;
        protected bool metadataGenerationStarted;
        protected bool metadataGenerationInProgress;

        #region Config Selection

        protected void SaveSelectedConfig(ProviderConfig config)
        {
            selectedConfig = config;

            if (config == null)
            {
                EditorPrefs.DeleteKey(ConfigPreferenceKey);
                Debug.Log("[EditorWindow] Cleared saved provider config.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(config);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[EditorWindow] Cannot save config selection: the selected config is not a project asset.");
                return;
            }

            EditorPrefs.SetString(ConfigPreferenceKey, path);

            Debug.Log($"[EditorWindow] Saved provider config: {path}");
        }

        protected ProviderConfig LoadSavedConfig()
        {
            if (!EditorPrefs.HasKey(ConfigPreferenceKey))
            {
                Debug.Log("[EditorWindow] No saved provider config found.");
                return null;
            }

            string path = EditorPrefs.GetString(ConfigPreferenceKey);

            if (string.IsNullOrWhiteSpace(path))
            {
                EditorPrefs.DeleteKey(ConfigPreferenceKey);
                return null;
            }

            ProviderConfig config = AssetDatabase.LoadAssetAtPath<ProviderConfig>(path);

            if (config == null)
            {
                Debug.LogWarning($"[EditorWindow] Saved provider config could not be loaded: {path}");

                EditorPrefs.DeleteKey(ConfigPreferenceKey);
                return null;
            }

            Debug.Log($"[EditorWindow] Restored provider config: {path}");
            return config;
        }

        #endregion
    }
}
