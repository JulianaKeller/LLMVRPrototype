using LLMVRPrototype.EditorTools;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class AssetsSection : DatabaseToolSection
    {
        private string selectedFolderPath;
        private List<AssetImportCandidate> discoveredAssets = new List<AssetImportCandidate>();

        private Label folderLabel;
        private Label statusLabel;
        private Button importButton;

        public AssetsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {

        }

        protected override string GetSectionTitle()
        {
            return "Asset Import";
        }

        protected override string GetSectionDescription()
        {
            return "Select a folder containing assets to discover and import into the database.";
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

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

            // An empty result means the user cancelled the dialog.
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

            statusLabel.text = $"Found {discoveredAssets.Count} supported asset(s). Skipped {skippedFileCount} unsupported or unrecognized file(s).";

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
