using LLMVRPrototype.EditorTools;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class AssetImportWindow : EditorWindow
    {
        private AssetImportSession session;
        private VisualElement contentContainer;

        private AssetImportPreparationView preparationView;
        private AssetImportDetailsView detailsView;

        private const string AssetDefinitionFolder = DatabasePaths.DefinitionsFolder + "/Assets";

        public static void Open(List<AssetImportCandidate> candidates)
        {
            AssetImportWindow window = CreateInstance<AssetImportWindow>();

            window.session = new AssetImportSession(candidates);
            window.titleContent = new GUIContent("Asset Import");
            window.minSize = new Vector2(850, 500);

            window.Show();
        }

        public void CreateGUI()
        {
            contentContainer = rootVisualElement;
            contentContainer.Clear();

            ShowPreparationView();
        }

        private void ShowPreparationView()
        {
            preparationView = new AssetImportPreparationView(
                contentContainer,
                session,
                ShowDetailsView);
        }

        private void ShowDetailsView()
        {
            AssetImportPreparationController controller = new AssetImportPreparationController(session);

            if (!controller.Prepare(out string errorMessage))
            {
                Debug.LogError(errorMessage);
                EditorUtility.DisplayDialog("Asset Preparation Failed", errorMessage, "OK");
                return;
            }

            preparationView?.StopPreviewRefresh();
            preparationView = null;

            contentContainer.Clear();

            detailsView = new AssetImportDetailsView(contentContainer, session);
            detailsView.CancelRequested += Close;
            detailsView.SaveAllRequested += HandleSaveAll;
        }

        private void HandleSaveAll()
        {
            // Perform your existing save-all operation here.
            Close();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            preparationView?.StopPreviewRefresh();
            preparationView = null;

            if (detailsView != null)
            {
                detailsView.CancelRequested -= Close;
                detailsView.SaveAllRequested -= HandleSaveAll;

                detailsView.Dispose();
                detailsView = null;
            }

            session?.Dispose();
            session = null;
        }
    }
}
