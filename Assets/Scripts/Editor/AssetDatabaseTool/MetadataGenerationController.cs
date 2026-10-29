using LLMVRPrototype.LLM.Providers;
using Project.LLM.Core;
using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class MetadataGenerationController : IDisposable
    {
        protected const string ConfigPreferenceKey = "LLMVRPrototype.TagEditor.SelectedProviderConfigPath";
        
        protected ObjectField apiConfigField;
        protected ProviderConfig selectedConfig;

        private Label statusLabel;

        // Read the current editor values through getInput
        private Func<MetadataGenerationInput> getInput;
        private Action<GeneratedMetadata> applyMetadata;

        private bool generationInProgress;
        private bool disposed;

        public void BuildUI(
        VisualElement parent,
        Label statusLabel,
        Func<MetadataGenerationInput> getInput,
        Action<GeneratedMetadata> applyMetadata)
        {
            this.statusLabel = statusLabel;
            this.getInput = getInput;
            this.applyMetadata = applyMetadata;

            var section = new Foldout
            {
                text = "AI Metadata Generation",
                value = true
            };

            section.style.marginTop = 12;
            parent.Add(section);

            apiConfigField = new ObjectField("LLM API Configuration")
            {
                objectType = typeof(ProviderConfig),
                allowSceneObjects = false
            };

            section.Add(apiConfigField);

            // Load the previously selected config first.
            selectedConfig = LoadSavedConfig();

            // If no saved config exists, use the config only when there is exactly one available.
            if (selectedConfig == null)
                selectedConfig = FindSingleConfig();

            // Explicitly restore the selection in the UI.
            apiConfigField.SetValueWithoutNotify(selectedConfig);

            apiConfigField.RegisterValueChangedCallback(evt =>
            {
                var config = evt.newValue as ProviderConfig;

                SaveSelectedConfig(config);

                if (selectedConfig == null)
                {
                    SetStatus("Select a provider configuration.");
                    return;
                }

                _ = GenerateAsync();
            });

            var retryButton = new Button(() =>
            {
                _ = GenerateAsync();
            })
            {
                text = "Generate / Retry Metadata"
            };

            section.Add(retryButton);

            if (selectedConfig == null)
            {
                statusLabel.text = "Select a provider configuration to generate metadata.";
            }
            else
            {
                _ = GenerateAsync();
            }
        }

        private async Task GenerateAsync()
        {
            if (disposed || generationInProgress)
                return;

            if (selectedConfig == null)
            {
                SetStatus("Select a provider configuration.");
                return;
            }

            MetadataGenerationInput input = getInput?.Invoke();

            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                SetStatus("Enter a name before generating metadata.");
                return;
            }

            generationInProgress = true;
            SetStatus($"Generating {input.EntityType.ToLowerInvariant()} metadata...");

            try
            {
                string apiKey = LocalLLMSecrets.LoadApiKey(selectedConfig.ApiKeySecretName);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    SetStatus("API key is unavailable. Check LocalLLMSecrets.");
                    return;
                }

                ILLMProvider provider = LLMProviderFactory.Create(selectedConfig, apiKey);

                var service = new LLMMetadataGenerationService(provider);

                GeneratedMetadata metadata;
                string error;

                MetadataGenerationResult<GeneratedMetadata> result = await service.GenerateMetadataAsync(input);

                metadata = result?.Data;
                error = result?.ErrorMessage;

                if (result == null || !result.Success)
                {
                    SetStatus(error ?? "Metadata generation failed.");
                    return;
                }

                if (disposed)
                    return;

                if (metadata == null)
                {
                    SetStatus("The metadata provider returned no metadata.");
                    return;
                }

                applyMetadata?.Invoke(metadata);

                SetStatus("Metadata generated. Review and edit it before saving.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                SetStatus("Metadata generation failed. See the Console.");
            }
            finally
            {
                generationInProgress = false;
            }
        }


        #region Config Save & Load

        private ProviderConfig FindSingleConfig()
        {
            string[] configGuids = AssetDatabase.FindAssets("t:ProviderConfig");

            if (configGuids.Length != 1)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(configGuids[0]);

            ProviderConfig config = AssetDatabase.LoadAssetAtPath<ProviderConfig>(path);

            if (config != null)
                SaveSelectedConfig(config);

            return config;
        }

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

        private void SetStatus(string message)
        {
            if (!disposed && statusLabel != null)
                statusLabel.text = message;
        }

        public void Dispose()
        {
            disposed = true;

            apiConfigField = null;
            statusLabel = null;
            getInput = null;
            applyMetadata = null;
        }
    }
}
