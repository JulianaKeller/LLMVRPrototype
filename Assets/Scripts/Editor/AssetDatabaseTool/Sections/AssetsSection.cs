using LLMVRPrototype.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class AssetsSection : DatabaseToolSection
    {

        public AssetsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {

        }

        protected override string GetSectionTitle()
        {
            return "Asset Import";
        }

        protected override string GetSectionDescription()
        {
            return "Import assets and generate their database definitions.";
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            AddHeading("Import Assets");

            AddButton("Select Assets", OnSelectAssets);

            // Additional asset-ingestion UI goes here.
        }

        private void OnSelectAssets()
        {
            // Implement later.
        }
    }
}
