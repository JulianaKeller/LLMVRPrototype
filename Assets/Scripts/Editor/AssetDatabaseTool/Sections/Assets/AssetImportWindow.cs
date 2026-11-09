using LLMVRPrototype.EditorTools;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.ShadowCascadeGUI;

namespace LLMVRPrototype
{
    public class AssetImportWindow : EditorWindow
    {
        private AssetImportSession session;
        private VisualElement contentContainer;

        private AssetImportPreparationView preparationView;
        private AssetImportDetailsView detailsView;
        private AssetImportPersistence persistence;

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
            persistence = new AssetImportPersistence();
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
            detailsView.CancelRequested += HandleCancel;
            detailsView.SaveRequested += HandleSave;
            detailsView.SaveAllRequested += HandleSaveAll;
            detailsView.DiscardRequested += HandleDiscard;
        }

        #region Persistence

        private void HandleSave(AssetImportItem item)
        {
            if (item == null || persistence == null)
                return;

            if (!persistence.SaveItem(session, item, out string errorMessage))
            {
                EditorUtility.DisplayDialog(
                    "Asset Save Failed",
                    errorMessage,
                    "OK");

                return;
            }

            detailsView.RemoveProfilePage(item);

            CloseIfComplete();
        }

        private void HandleSaveAll()
        {
            if (persistence == null || detailsView == null)
                return;

            StringBuilder errors = new StringBuilder();

            // Iterate over the session items rather than profile pages because
            // successful saves remove their corresponding pages.
            foreach (AssetImportItem item in session.Items)
            {
                if (item == null || item.IsSaved || item.IsDiscarded)
                    continue;

                if (!persistence.SaveItem(session, item, out string errorMessage))
                {
                    errors.AppendLine($"{item.Candidate.DisplayName}: {errorMessage}");
                    continue;
                }

                detailsView.RemoveProfilePage(item);
            }

            if (errors.Length > 0)
            {
                EditorUtility.DisplayDialog(
                    "Some Assets Could Not Be Saved",
                    errors.ToString(),
                    "OK");
            }

            CloseIfComplete();
        }

        private void HandleDiscard(AssetImportItem item)
        {
            if (item == null || item.IsSaved || item.IsDiscarded)
                return;

            item.MarkDiscarded();
            item.DestroyTemporaryObjects();

            detailsView.RemoveProfilePage(item);

            CloseIfComplete();
        }

        private void CloseIfComplete()
        {
            if (session == null || session.RemainingCount > 0)
                return;

            Close();
        }

        #endregion

        private void HandleCancel()
        {
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
                detailsView.CancelRequested -= HandleCancel;
                detailsView.SaveRequested -= HandleSave;
                detailsView.SaveAllRequested -= HandleSaveAll;
                detailsView.DiscardRequested -= HandleDiscard;


                detailsView.Dispose();
                detailsView = null;
            }

            session?.Dispose();
            session = null;
            persistence = null;
        }
    }
}
