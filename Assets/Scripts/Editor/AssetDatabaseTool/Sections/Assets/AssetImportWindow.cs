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
            new AssetImportPreparationView(
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

            contentContainer.Clear();
            new AssetImportDetailsView(contentContainer, session);
        }

        private void OnDestroy()
        {
            session?.Dispose();
        }
    }
}
