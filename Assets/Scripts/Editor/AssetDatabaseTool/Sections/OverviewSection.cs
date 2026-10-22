using LLMVRPrototype.Assets;
using LLMVRPrototype.Assets.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public class OverviewSection : DatabaseToolSection
    {
        private Label statusLabel;

        public OverviewSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {

        }

        protected override string GetSectionTitle()
        {
            return "Database Overview";
        }

        protected override string GetSectionDescription()
        {
            return "Inspect database references, review statistics, and validate database definitions.";
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            AddHeading("Configuration");

            AddReferenceRow("Configuration Asset", context.DatabaseConfig);

            AddHeading("Database Contents");

            AddReferenceRow("Asset Catalog", context.DatabaseConfig.AssetCatalog);

            AddReferenceRow("Category Registry", context.DatabaseConfig.CategoryRegistry);

            AddReferenceRow("Tag Registry", context.DatabaseConfig.TagRegistry);

            AddReferenceRow("Operation Catalog", context.DatabaseConfig.OperationCatalog);

            BuildStatistics();
            BuildValidation();
        }

        private void BuildStatistics()
        {
            AddHeading("Statistics");

            AssetCatalog assetCatalog = context.DatabaseConfig.AssetCatalog;

            int totalAssets = 0;
            int enabledAssets = 0;

            foreach (AssetDefinition asset in assetCatalog.Assets)
            {
                if (asset == null)
                    continue;

                totalAssets++;

                if (asset.IsEnabled)
                    enabledAssets++;
            }

            int totalOperations = 0;
            int enabledOperations = 0;

            foreach (OperationDefinition operation in context.DatabaseConfig.OperationCatalog.Operations)
            {
                if (operation == null)
                    continue;

                totalOperations++;

                if (operation.IsEnabled)
                    enabledOperations++;
            }

            AddStatistic("Assets", totalAssets);
            AddStatistic("Enabled Assets", enabledAssets);

            AddStatistic("Categories", context.DatabaseConfig.CategoryRegistry.Categories.Count);

            AddStatistic("Tags", context.DatabaseConfig.TagRegistry.Tags.Count);

            AddStatistic("Operations", totalOperations);
            AddStatistic("Enabled Operations", enabledOperations);
        }

        private void BuildValidation()
        {
            AddHeading("Validation");

            statusLabel = new Label();
            statusLabel.style.marginTop = 5;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;

            AddButton("Validate Database", ValidateDatabase);

            root.Add(statusLabel);
        }

        private void ValidateDatabase()
        {
            var errors = new List<string>();

            errors.AddRange(
                AssetDefinitionValidator.Validate(
                    context.DatabaseConfig.AssetCatalog,
                    context.DatabaseConfig.CategoryRegistry,
                    context.DatabaseConfig.TagRegistry));

            errors.AddRange(
                OperationDefinitionValidator.Validate(
                    context.DatabaseConfig.OperationCatalog));

            errors.AddRange(
                OperationDefinitionValidator.ValidateAssetCompatibility(
                    context.DatabaseConfig.AssetCatalog,
                    context.DatabaseConfig.OperationCatalog));

            if (errors.Count == 0)
            {
                statusLabel.text = "Validation passed. No errors were found.";

                UnityEngine.Debug.Log("Asset database validation passed.", context.DatabaseConfig);

                return;
            }

            statusLabel.text = $"Validation found {errors.Count} error(s). See the Console for details.";

            foreach (string error in errors)
            {
                UnityEngine.Debug.LogError(error, context.DatabaseConfig);
            }
        }
    }
}
