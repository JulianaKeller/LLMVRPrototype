using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public sealed class AssetImportPreparationView
    {
        private readonly VisualElement root;
        private readonly AssetImportSession session;
        private readonly Action onContinue;

        private readonly List<Type> availableDefinitionTypes = new();

        private VisualElement assetListContainer;
        private Label summaryLabel;
        private Button continueButton;

        private readonly Dictionary<AssetImportCandidate, Image> previewImages = new();
        private IVisualElementScheduledItem previewRefreshTask;

        public AssetImportPreparationView(VisualElement parent, AssetImportSession session, Action onContinue)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.onContinue = onContinue;

            root = new VisualElement();
            root.style.flexGrow = 1;
            parent.Add(root);

            LoadAvailableDefinitionTypes();
            Build();
        }

        private void Build()
        {
            root.Clear();
            BuildHeader();
            BuildSummary();
            BuildAssetList();
            BuildFooter();
        }

        private void BuildHeader()
        {
            Label heading = new Label("Prepare Assets");
            heading.style.fontSize = 20;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 4;

            Label description = new Label(
                "Review the discovered assets, provide preliminary names and descriptions, and select a definition type."
            );

            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 12;

            root.Add(heading);
            root.Add(description);
        }

        private void BuildSummary()
        {
            summaryLabel = new Label();
            summaryLabel.style.marginBottom = 8;
            summaryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            root.Add(summaryLabel);
            UpdateSummary();
        }

        private void BuildAssetList()
        {
            ScrollView scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            scrollView.style.marginBottom = 10;

            assetListContainer = new VisualElement();
            assetListContainer.style.flexGrow = 1;

            scrollView.Add(assetListContainer);
            root.Add(scrollView);

            RefreshAssetList();

            previewRefreshTask?.Pause();
            previewRefreshTask = root.schedule.Execute(RefreshPreviews).Every(250);
        }

        private void BuildFooter()
        {
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.marginTop = 8;

            continueButton = new Button(HandleContinue)
            {
                text = "Continue"
            };

            continueButton.style.minWidth = 120;

            footer.Add(continueButton);
            root.Add(footer);

            UpdateContinueButton();
        }

        private void LoadAvailableDefinitionTypes()
        {
            availableDefinitionTypes.Clear();

            IEnumerable<Type> types = TypeCache.GetTypesDerivedFrom<AssetDefinition>();

            availableDefinitionTypes.AddRange(
                types.Where(type =>
                    !type.IsAbstract &&
                    !type.IsGenericType &&
                    type.GetConstructor(Type.EmptyTypes) != null
                )
            );

            availableDefinitionTypes.Sort(
                (a, b) => string.Compare(
                    AssetDefinitionTypeResolver.GetDisplayName(a),
                    AssetDefinitionTypeResolver.GetDisplayName(b),
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }

        private void RefreshAssetList()
        {
            previewImages.Clear();

            assetListContainer.Clear();

            foreach (AssetImportItem item in session.Items)
                assetListContainer.Add(CreateAssetRow(item));

            UpdateSummary();
            UpdateContinueButton();
        }

        private VisualElement CreateAssetRow(AssetImportItem item)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.paddingTop = 10;
            row.style.paddingBottom = 10;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.marginBottom = 4;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            row.Add(CreatePreview(item.Candidate));

            VisualElement information = new VisualElement();
            information.style.flexGrow = 1;
            information.style.marginLeft = 10;
            information.style.minWidth = 0;

            Label sourceNameLabel = new Label(item.Candidate.DisplayName);
            sourceNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            Label sourceTypeLabel = new Label($"Source: {item.Candidate.AssetType}");
            sourceTypeLabel.style.fontSize = 11;
            sourceTypeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);

            Label pathLabel = new Label(item.Candidate.AssetPath);
            pathLabel.style.fontSize = 10;
            pathLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            pathLabel.style.whiteSpace = WhiteSpace.Normal;

            TextField preliminaryNameField = new TextField("Preliminary Name")
            {
                value = item.Candidate.PreliminaryName
            };

            preliminaryNameField.style.marginTop = 8;

            preliminaryNameField.RegisterValueChangedCallback(evt =>
            {
                item.Candidate.PreliminaryName = evt.newValue;
            });

            TextField preliminaryDescriptionField = new TextField("Preliminary Description")
            {
                value = item.Candidate.PreliminaryDescription,
                multiline = true
            };

            preliminaryDescriptionField.style.marginTop = 4;
            preliminaryDescriptionField.style.minHeight = 55;

            preliminaryDescriptionField.RegisterValueChangedCallback(evt =>
            {
                item.Candidate.PreliminaryDescription = evt.newValue;
            });

            information.Add(sourceNameLabel);
            information.Add(sourceTypeLabel);
            information.Add(pathLabel);
            information.Add(preliminaryNameField);
            information.Add(preliminaryDescriptionField);

            row.Add(information);

            VisualElement controls = new VisualElement();
            controls.style.width = 190;
            controls.style.marginLeft = 10;

            Label definitionLabel = new Label("Definition Type");
            definitionLabel.style.fontSize = 11;
            definitionLabel.style.marginBottom = 2;

            List<string> definitionNames = availableDefinitionTypes
                .Select(AssetImportDefinitionDisplayName)
                .ToList();

            int selectedIndex = availableDefinitionTypes.IndexOf(item.Candidate.DefinitionType);

            if (selectedIndex < 0 && availableDefinitionTypes.Count > 0)
            {
                item.Candidate.DefinitionType = AssetDefinitionTypeResolver.Resolve(AssetDatabase.LoadMainAssetAtPath(item.Candidate.AssetPath));

                selectedIndex = availableDefinitionTypes.IndexOf(item.Candidate.DefinitionType);
            }

            DropdownField definitionDropdown = new DropdownField(
                definitionNames,
                Mathf.Max(0, selectedIndex)
            );

            definitionDropdown.SetEnabled(definitionNames.Count > 0);

            definitionDropdown.RegisterValueChangedCallback(evt =>
            {
                int index = definitionNames.IndexOf(evt.newValue);

                if (index >= 0 && index < availableDefinitionTypes.Count)
                    item.Candidate.DefinitionType = availableDefinitionTypes[index];

                UpdateContinueButton();
            });

            Button removeButton = new Button(() =>
            {
                session.RemoveItem(item);
                RefreshAssetList();
            })
            {
                text = "Remove"
            };

            removeButton.style.marginTop = 6;

            controls.Add(definitionLabel);
            controls.Add(definitionDropdown);
            controls.Add(removeButton);

            row.Add(controls);

            return row;
        }

        private Image CreatePreview(AssetImportCandidate candidate)
        {
            UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(candidate.AssetPath);

            Texture2D previewTexture = sourceAsset != null
                ? AssetPreview.GetAssetPreview(sourceAsset)
                : null;

            if (previewTexture != null)
                candidate.PreviewTexture = previewTexture;

            Image preview = new Image
            {
                image = previewTexture ?? (sourceAsset != null ? AssetPreview.GetMiniThumbnail(sourceAsset) : null),
                scaleMode = ScaleMode.ScaleToFit
            };

            preview.style.width = 72;
            preview.style.height = 72;
            preview.style.flexShrink = 0;

            previewImages[candidate] = preview;

            return preview;
        }

        private void RefreshPreviews()
        {
            bool allPreviewsReady = true;

            foreach (AssetImportItem item in session.Items)
            {
                if (item.Candidate.PreviewTexture != null)
                    continue;

                UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(item.Candidate.AssetPath);

                if (sourceAsset == null)
                    continue;

                Texture2D previewTexture = AssetPreview.GetAssetPreview(sourceAsset);

                if (previewTexture == null)
                {
                    allPreviewsReady = false;
                    continue;
                }

                item.Candidate.PreviewTexture = previewTexture;

                if (previewImages.TryGetValue(item.Candidate, out Image preview))
                    preview.image = previewTexture;
            }

            if (allPreviewsReady)
                previewRefreshTask?.Pause();
        }

        public void StopPreviewRefresh()
        {
            previewRefreshTask?.Pause();
            previewRefreshTask = null;
            previewImages.Clear();
        }

        private string AssetImportDefinitionDisplayName(Type type)
        {
            return AssetDefinitionTypeResolver.GetDisplayName(type);
        }

        private void UpdateSummary()
        {
            summaryLabel.text = $"Assets selected: {session.Items.Count}";
        }

        private void UpdateContinueButton()
        {
            if (continueButton == null)
                return;

            continueButton.SetEnabled(
                session.Items.Count > 0 &&
                session.Items.All(item =>
                    item.Candidate.DefinitionType != null &&
                    typeof(AssetDefinition).IsAssignableFrom(item.Candidate.DefinitionType)
                )
            );
        }

        private void HandleContinue()
        {
            if (session.Items.Count == 0)
                return;

            if (session.Items.Any(item => item.Candidate.DefinitionType == null))
                return;

            StopPreviewRefresh();

            onContinue?.Invoke();
        }
    }
}