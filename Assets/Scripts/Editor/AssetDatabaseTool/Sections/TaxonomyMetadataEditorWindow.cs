using LLMVRPrototype.EditorTools;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public abstract class TaxonomyMetadataEditorWindow<TDefinition>
        : MetadataEditorWindow<TDefinition>
        where TDefinition : ScriptableObject
    {
        private Label selectedParentLabel;
        private VisualElement parentTreeContainer;

        private TaxonomyHierarchyView<TDefinition> hierarchyView;

        protected abstract TDefinition SelectedParent { get; set; }
        protected abstract IEnumerable<TDefinition> GetAllDefinitions();
        protected abstract TDefinition GetParent(TDefinition item);
        protected abstract string GetHierarchyItemName(TDefinition item);

        protected abstract string ParentPropertyName { get; }

        protected override void BuildSpecificFields(VisualElement container)
        {
            BuildParentSelector(container);
        }

        private void BuildParentSelector(VisualElement container)
        {
            var section = new Foldout
            {
                text = $"Parent {DefinitionTypeName}",
                value = false
            };

            section.style.marginTop = 10;
            container.Add(section);

            selectedParentLabel = new Label();
            selectedParentLabel.style.whiteSpace = WhiteSpace.Normal;
            selectedParentLabel.style.marginBottom = 6;
            section.Add(selectedParentLabel);

            var noneButton = new Button(() => SelectParent(null))
            {
                text = "None (root)"
            };

            noneButton.style.marginBottom = 6;
            section.Add(noneButton);

            var scrollView = new ScrollView();
            scrollView.style.height = 220;
            scrollView.style.minHeight = 100;

            section.Add(scrollView);

            hierarchyView = new TaxonomyHierarchyView<TDefinition>(
                GetAllDefinitions(),
                GetHierarchyItemName,
                GetParent,
                GetDefinitionIdForHierarchy,
                SelectParent,
                item => ReferenceEquals(item, SelectedParent),
                CanSelectAsParent,
                "Parent");

            scrollView.Add(hierarchyView.Root);

            parentTreeContainer = scrollView;

            RefreshSelectedParentLabel();
        }

        private string GetDefinitionIdForHierarchy(TDefinition item)
        {
            return item != null ? item.name : string.Empty;
        }

        private void RefreshSelectedParentLabel()
        {
            selectedParentLabel.text = SelectedParent == null
                ? "Current parent: None (root)"
                : $"Current parent: {GetHierarchyItemName(SelectedParent)}";
        }

        protected bool CanSelectAsParent(TDefinition candidate)
        {
            if (candidate == null || candidate == definition)
                return false;

            var visited = new HashSet<TDefinition>();
            TDefinition current = candidate;

            while (current != null && visited.Add(current))
            {
                if (current == definition)
                    return false;

                current = GetParent(current);
            }

            return true;
        }

        private void SelectParent(TDefinition parent)
        {
            if (parent != null && !CanSelectAsParent(parent))
                return;

            SelectedParent = parent;

            RefreshSelectedParentLabel();
            hierarchyView?.Refresh();
        }

        protected override bool ValidateDefinition()
        {
            if (!base.ValidateDefinition())
                return false;

            if (SelectedParent != null &&
                !CanSelectAsParent(SelectedParent))
            {
                statusLabel.text = "That parent would create a hierarchy cycle. Choose another parent.";
                return false;
            }

            return true;
        }

        protected override void SaveAndClose()
        {
            serializedDefinition.Update();

            SerializedProperty parentProperty =
                serializedDefinition.FindProperty(ParentPropertyName);

            if (parentProperty == null)
            {
                statusLabel.text = $"The serialized property '{ParentPropertyName}' was not found.";
                return;
            }

            parentProperty.objectReferenceValue = SelectedParent;
            serializedDefinition.ApplyModifiedProperties();

            base.SaveAndClose();
        }
    }
}
