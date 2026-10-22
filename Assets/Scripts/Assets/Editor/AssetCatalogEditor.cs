using UnityEngine;
using UnityEditor;

namespace LLMVRPrototype.Assets.Editor
{
    [CustomEditor(typeof(AssetCatalog))]
    public class AssetCatalogEditor : UnityEditor.Editor
    {
        private AssetDatabaseConfig databaseConfig;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Database Validation", EditorStyles.boldLabel);

            databaseConfig = (AssetDatabaseConfig)EditorGUILayout.ObjectField(
                "Database Configuration",
                databaseConfig,
                typeof(AssetDatabaseConfig),
                false);

            using (new EditorGUI.DisabledScope(databaseConfig == null))
            {
                if (GUILayout.Button("Validate Database"))
                {
                    ValidateDatabase();
                }
            }
        }

        private void ValidateDatabase()
        {
            if (databaseConfig == null)
            {
                Debug.LogError("No AssetDatabaseConfig has been assigned.");
                return;
            }

            if (!databaseConfig.HasRequiredReferences)
            {
                Debug.LogError("Database configuration has missing references. Assign the asset catalog, category registry, and tag registry.");

                return;
            }

            AssetCatalog catalog = databaseConfig.AssetCatalog;

            if (catalog != target)
            {
                Debug.LogError("The configuration references a different AssetCatalog. Assign the correct configuration for this catalog.", catalog);

                return;
            }

            var errors = AssetDefinitionValidator.Validate(
                databaseConfig.AssetCatalog,
                databaseConfig.CategoryRegistry,
                databaseConfig.TagRegistry);

            var operationErrors = OperationDefinitionValidator.Validate(databaseConfig.OperationCatalog);
            errors.AddRange(operationErrors);

            if (errors.Count == 0)
            {
                Debug.Log("Asset database validation successful.", catalog);
                return;
            }

            Debug.LogError($"Asset database validation found {errors.Count} error(s).");

            foreach (string error in errors)
            {
                Debug.LogError(error, catalog);
            }
        }
    }
}
