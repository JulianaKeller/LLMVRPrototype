using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class AssetsSection : DatabaseToolSection
    {
        private string selectedFolderPath;
        private List<AssetImportCandidate> discoveredAssets = new();

        private Label folderLabel;
        private Label statusLabel;
        private Button importButton;

        private TaxonomySearchField<AssetDefinition> assetSearchField;

        public AssetsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
        }

        protected override string GetSectionTitle()
        {
            return "Assets";
        }

        protected override string GetSectionDescription()
        {
            return "Import new assets or find and edit existing asset definitions.";
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            BuildAssetSearch();
            BuildImportSection();
        }

        private void BuildAssetSearch()
        {
            AddHeading("Find Existing Assets");

            assetSearchField = new TaxonomySearchField<AssetDefinition>(
                "Search Asset Definitions",
                GetAssetDefinitions(),
                definition => definition.DisplayName,
                definition => definition.AssetId,
                definition => GetAssetSearchTerms(definition),
                OnOpenForEdit);

            root.Add(assetSearchField.Root);
        }

        private IEnumerable<AssetDefinition> GetAssetDefinitions()
        {
            string folder = DatabasePaths.DefinitionsFolder;

            if (!AssetDatabase.IsValidFolder(folder))
                yield break;

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDefinition definition = AssetDatabase.LoadAssetAtPath<AssetDefinition>(path);

                if (definition != null)
                    yield return definition;
            }
        }

        private IEnumerable<string> GetAssetSearchTerms(AssetDefinition definition)
        {
            if (definition == null)
                yield break;

            // DisplayName is already searched directly by TaxonomySearchField
            // Include it here as well so the searchable metadata is explicit
            yield return definition.DisplayName;

            if (!string.IsNullOrWhiteSpace(definition.CategoryId) &&
                context.DatabaseConfig.CategoryRegistry.TryGetById(
                    definition.CategoryId,
                    out AssetCategoryDefinition category))
            {
                yield return category.DisplayName;
            }

            foreach (string tagId in definition.TagIds)
            {
                if (context.DatabaseConfig.TagRegistry.TryGetById(tagId, out AssetTagDefinition tag))
                    yield return tag.DisplayName;
            }
        }

        private void OnOpenForEdit(AssetDefinition definition)
        {
            if (definition == null)
                return;


            // TODO: Open the appropriate asset definition editor window.
        }

        private void BuildImportSection()
        {
            AddHeading("Import Assets");

            AddButton("Choose Folder", OnSelectFolder);

            folderLabel = new Label("No folder selected.");
            folderLabel.style.marginTop = 5;
            folderLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(folderLabel);

            statusLabel = new Label();
            statusLabel.style.marginTop = 8;
            statusLabel.style.whiteSpace = WhiteSpace.Normal;
            root.Add(statusLabel);

            importButton = AddButton("Import to Database", OnImportAssets);
            importButton.SetEnabled(false);
        }

        private void OnSelectFolder()
        {
            string absoluteFolderPath = EditorUtility.OpenFolderPanel(
                "Choose Folder Containing Assets",
                Application.dataPath,
                string.Empty);

            if (string.IsNullOrWhiteSpace(absoluteFolderPath))
                return;

            selectedFolderPath = absoluteFolderPath;
            folderLabel.text = absoluteFolderPath;
            statusLabel.text = "Discovering assets...";

            discoveredAssets.Clear();
            importButton.SetEnabled(false);

            bool success = AssetImportDiscovery.TryDiscover(
                absoluteFolderPath,
                out List<AssetImportCandidate> candidates,
                out int skippedFileCount,
                out string errorMessage);

            if (!success)
            {
                statusLabel.text = errorMessage;
                return;
            }

            discoveredAssets = candidates;

            statusLabel.text =
                $"Found {discoveredAssets.Count} supported asset(s). " +
                $"Skipped {skippedFileCount} unsupported or unrecognized file(s).";

            importButton.SetEnabled(discoveredAssets.Count > 0);
        }

        private void OnImportAssets()
        {
            if (discoveredAssets == null || discoveredAssets.Count == 0)
                return;

            AssetImportWindow.Open(discoveredAssets);
        }
    }
}