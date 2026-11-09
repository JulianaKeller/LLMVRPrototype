using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    // Header, navigation arrows, footer buttons, active profile switching and keyboard navigation
    public sealed class AssetImportDetailsView
    {
        private readonly VisualElement root;
        private readonly AssetImportSession session;

        // create all profile pages once and retain them
        private readonly List<AssetImportProfilePage> profilePages = new();

        private VisualElement metadataContainer;
        private Label headingLabel;
        private Label pageIndicator;

        private Button previousButton;
        private Button nextButton;
        private Button saveButton;
        private Button saveAllButton;
        private Button discardButton;
        private Button cancelButton;

        private int currentPageIndex;
        private bool isDisposed;

        public event Action<AssetImportItem> SaveRequested;
        public event Action SaveAllRequested;
        public event Action<AssetImportItem> DiscardRequested;
        public event Action CancelRequested;

        public AssetImportItem CurrentItem => currentPageIndex >= 0 && currentPageIndex < profilePages.Count ? profilePages[currentPageIndex].Item : null;

        public int CurrentPageIndex => currentPageIndex;
        public int PageCount => profilePages.Count;

        public AssetImportDetailsView(VisualElement parent, AssetImportSession session)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.session = session ?? throw new ArgumentNullException(nameof(session));

            root = new VisualElement();
            root.style.flexGrow = 1;
            root.focusable = true;
            root.tabIndex = 0;

            parent.Add(root);

            Build();

            InitializeProfilePages();

            root.RegisterCallback<KeyDownEvent>(HandleKeyDown);
        }

        private void Build()
        {
            root.Clear();

            BuildHeader();
            BuildMetadataSection();
            BuildFooter();
        }

        #region Header

        private void BuildHeader()
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 12;

            VisualElement titleContainer = new VisualElement();
            titleContainer.style.flexGrow = 1;

            headingLabel = new Label("Asset Details");
            headingLabel.style.fontSize = 20;
            headingLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headingLabel.style.marginBottom = 4;

            Label description = new Label("Review and configure the metadata for each selected asset.");

            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.fontSize = 11;

            titleContainer.Add(headingLabel);
            titleContainer.Add(description);

            VisualElement navigation = new VisualElement();
            navigation.style.flexDirection = FlexDirection.Row;
            navigation.style.alignItems = Align.Center;

            previousButton = new Button(ShowPreviousPage)
            {
                text = "Previous"
            };

            previousButton.tooltip = "Previous asset";
            previousButton.style.minWidth = 75;

            pageIndicator = new Label();
            pageIndicator.style.marginLeft = 10;
            pageIndicator.style.marginRight = 10;
            pageIndicator.style.minWidth = 55;
            pageIndicator.style.unityTextAlign = TextAnchor.MiddleCenter;

            nextButton = new Button(ShowNextPage)
            {
                text = "Next"
            };

            nextButton.tooltip = "Next asset";
            nextButton.style.minWidth = 75;

            navigation.Add(previousButton);
            navigation.Add(pageIndicator);
            navigation.Add(nextButton);

            header.Add(titleContainer);
            header.Add(navigation);

            root.Add(header);
        }

        private void UpdateHeader()
        {
            int count = profilePages.Count;

            if (count == 0)
            {
                pageIndicator.text = "0 / 0";
                previousButton.SetEnabled(false);
                nextButton.SetEnabled(false);
                headingLabel.text = "Asset Details";
                return;
            }

            AssetImportItem currentItem = CurrentItem;

            pageIndicator.text = $"{currentPageIndex + 1} / {count}";

            previousButton.SetEnabled(currentPageIndex > 0);
            nextButton.SetEnabled(currentPageIndex < count - 1);

            headingLabel.text = currentItem?.Candidate.PreliminaryName ?? "Asset Details";
        }

        #endregion

        #region Metadata

        private void BuildMetadataSection()
        {
            metadataContainer = new ScrollView();
            metadataContainer.style.flexGrow = 1;
            metadataContainer.style.marginTop = 8;
            metadataContainer.style.marginBottom = 12;

            root.Add(metadataContainer);
        }

        private void InitializeProfilePages()
        {
            profilePages.Clear();
            metadataContainer.Clear();

            foreach (AssetImportItem item in session.Items)
            {
                if (item.IsSaved || item.IsDiscarded || item.Definition == null)
                    continue;

                AssetImportProfilePage page = new AssetImportProfilePage(
                    metadataContainer,
                    item,
                    session.ReplaceGeneratedThumbnail
                );

                page.SetVisible(false);
                profilePages.Add(page);
            }

            if (profilePages.Count > 0)
                SwitchToPage(0);

            UpdateHeader();
            UpdateFooter();
        }

        private void SwitchToPage(int index)
        {
            if (index < 0 || index >= profilePages.Count)
                return;

            if (currentPageIndex >= 0 && currentPageIndex < profilePages.Count)
                profilePages[currentPageIndex].SetVisible(false);

            currentPageIndex = index;

            profilePages[currentPageIndex].SetVisible(true);

            UpdateHeader();
            UpdateFooter();
        }

        private void ShowPreviousPage()
        {
            if (currentPageIndex > 0)
                SwitchToPage(currentPageIndex - 1);
        }

        private void ShowNextPage()
        {
            if (currentPageIndex < profilePages.Count - 1)
                SwitchToPage(currentPageIndex + 1);
        }

        #endregion

        #region Footer

        private void BuildFooter()
        {
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Center;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.paddingTop = 8;
            footer.style.borderTopWidth = 1;
            footer.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);

            VisualElement leftButtons = new VisualElement();
            leftButtons.style.flexDirection = FlexDirection.Row;

            discardButton = new Button(HandleDiscard)
            {
                text = "Discard"
            };

            cancelButton = new Button(HandleCancel)
            {
                text = "Cancel"
            };

            discardButton.style.minWidth = 90;
            cancelButton.style.minWidth = 90;
            cancelButton.style.marginLeft = 6;

            leftButtons.Add(discardButton);
            leftButtons.Add(cancelButton);

            VisualElement rightButtons = new VisualElement();
            rightButtons.style.flexDirection = FlexDirection.Row;

            saveButton = new Button(HandleSave)
            {
                text = "Save"
            };

            saveAllButton = new Button(HandleSaveAll)
            {
                text = "Save All"
            };

            saveButton.style.minWidth = 90;
            saveAllButton.style.minWidth = 90;
            saveAllButton.style.marginLeft = 6;

            rightButtons.Add(saveButton);
            rightButtons.Add(saveAllButton);

            footer.Add(leftButtons);
            footer.Add(rightButtons);

            root.Add(footer);
        }

        private void UpdateFooter()
        {
            bool hasCurrentItem = CurrentItem != null;
            bool hasItems = profilePages.Count > 0;

            saveButton?.SetEnabled(hasCurrentItem);
            discardButton?.SetEnabled(hasCurrentItem);
            saveAllButton?.SetEnabled(hasItems);
        }

        private void HandleSave()
        {
            AssetImportItem item = CurrentItem;

            if (item == null)
                return;

            profilePages[currentPageIndex].ApplyChanges();

            SaveRequested?.Invoke(item);
        }

        private void HandleDiscard()
        {
            AssetImportItem item = CurrentItem;

            if (item == null)
                return;

            DiscardRequested?.Invoke(item);
        }

        private void HandleSaveAll()
        {
            foreach (AssetImportProfilePage page in profilePages)
                page.ApplyChanges();

            SaveAllRequested?.Invoke();
        }

        private void HandleCancel()
        {
            CancelRequested?.Invoke();
        }

        #endregion

        #region Profile Management

        // Called by the controller after a successful save or discard.
        public void RemoveProfilePage(AssetImportItem item)
        {
            if (item == null)
                return;

            int removedIndex = profilePages.FindIndex(page => page.Item == item);

            if (removedIndex < 0)
                return;

            AssetImportProfilePage removedPage = profilePages[removedIndex];

            removedPage.SetVisible(false);
            removedPage.Dispose();

            profilePages.RemoveAt(removedIndex);

            if (profilePages.Count == 0)
            {
                currentPageIndex = -1;
            }
            else if (removedIndex < currentPageIndex)
            {
                currentPageIndex--;
            }
            else if (removedIndex == currentPageIndex)
            {
                currentPageIndex = Mathf.Min(removedIndex, profilePages.Count - 1);
            }

            if (currentPageIndex >= 0)
                profilePages[currentPageIndex].SetVisible(true);

            UpdateHeader();
            UpdateFooter();
        }

        private void DisposeProfilePages()
        {
            foreach (AssetImportProfilePage page in profilePages)
            {
                page.Dispose();
            }

            profilePages.Clear();
        }

        #endregion

        #region Keyboard Navigation

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (IsEditingTextField(evt.target as VisualElement))
                return;

            if (evt.keyCode == KeyCode.LeftArrow)
            {
                ShowPreviousPage();
                evt.StopPropagation();
                evt.PreventDefault();
            }
            else if (evt.keyCode == KeyCode.RightArrow)
            {
                ShowNextPage();
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        private bool IsEditingTextField(VisualElement target)
        {
            VisualElement current = target;

            while (current != null && current != root)
            {
                if (current is TextField || current is TextElement)
                    return true;

                Type currentType = current.GetType();

                if (currentType.IsGenericType &&
                    currentType.GetGenericTypeDefinition() == typeof(BaseField<>))
                    return true;

                current = current.parent;
            }

            return false;
        }

        #endregion


        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            root.UnregisterCallback<KeyDownEvent>(HandleKeyDown);

            DisposeProfilePages();

            root.RemoveFromHierarchy();
        }
    }
}