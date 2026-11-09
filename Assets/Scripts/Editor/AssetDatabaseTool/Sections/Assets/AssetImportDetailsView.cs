using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    // Header, navigation arrows, footer buttons, active profile switching and keyboard navigation
    public sealed class AssetImportDetailsView
    {
        private readonly VisualElement root;
        private readonly AssetImportSession session;

        private VisualElement metadataContainer;

        public AssetImportDetailsView(VisualElement parent, AssetImportSession session)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.session = session ?? throw new ArgumentNullException(nameof(session));

            root = new VisualElement();
            root.style.flexGrow = 1;
            parent.Add(root);

            Build();
        }

        private void Build()
        {
            root.Clear();

            Label heading = new Label("Asset Details");
            heading.style.fontSize = 20;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 4;

            Label description = new Label(
                "Review and configure the metadata for the assets selected during preparation."
            );

            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 12;

            Label summary = new Label($"Assets prepared: {session.Items.Count}"
            );

            summary.style.unityFontStyleAndWeight = FontStyle.Bold;
            summary.style.marginBottom = 12;

            Label placeholder = new Label(
                "Asset metadata configuration will be implemented in this stage."
            );

            placeholder.style.whiteSpace = WhiteSpace.Normal;

            root.Add(heading);
            root.Add(description);
            root.Add(summary);
            root.Add(placeholder);
        }

        private void BuildMetadataSection()
        {
            metadataContainer = new VisualElement();
            metadataContainer.style.marginTop = 12;

            root.Add(metadataContainer);

            // Initialize metadata generation when the workflow is ready.
        }
    }
}