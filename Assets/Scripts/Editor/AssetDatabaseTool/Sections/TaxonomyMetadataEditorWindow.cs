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

            parentTreeContainer = scrollView;

            RefreshSelectedParentLabel();
            RefreshParentTree();
        }

        private void RefreshSelectedParentLabel()
        {
            selectedParentLabel.text = SelectedParent == null
                ? "Current parent: None (root)"
                : $"Current parent: {GetHierarchyItemName(SelectedParent)}";
        }

        private void RefreshParentTree()
        {
            if (parentTreeContainer == null)
                return;

            parentTreeContainer.Clear();

            List<TDefinition> definitions =
                new List<TDefinition>(GetAllDefinitions());

            definitions.RemoveAll(item => item == null);

            if (definitions.Count == 0)
            {
                parentTreeContainer.Add(
                    new Label($"No registered {DefinitionTypeName.ToLowerInvariant()}s."));
                return;
            }

            var visited = new HashSet<TDefinition>();

            foreach (TDefinition item in definitions)
            {
                TDefinition parent = GetParent(item);

                bool isRoot = parent == null || !definitions.Contains(parent);

                if (!isRoot || visited.Contains(item))
                    continue;

                VisualElement node =
                    BuildHierarchyNode(item, definitions, visited);

                if (node != null)
                    parentTreeContainer.Add(node);
            }

            // Display disconnected or malformed hierarchy branches too.
            foreach (TDefinition item in definitions)
            {
                if (visited.Contains(item))
                    continue;

                var invalidGroup = new Foldout
                {
                    text = "Other definitions (check hierarchy)",
                    value = true
                };

                VisualElement node =
                    BuildHierarchyNode(item, definitions, visited);

                if (node != null)
                    invalidGroup.Add(node);

                parentTreeContainer.Add(invalidGroup);
            }
        }

        private VisualElement BuildHierarchyNode(
            TDefinition node,
            List<TDefinition> definitions,
            HashSet<TDefinition> visited)
        {
            if (node == null || !visited.Add(node))
                return null;

            var foldout = new Foldout
            {
                text = GetHierarchyItemName(node),
                value = false
            };

            bool canSelect = CanSelectAsParent(node);

            var selectButton = new Button(() => SelectParent(node))
            {
                text = ReferenceEquals(node, SelectedParent)
                    ? "Current parent"
                    : "Use as parent"
            };

            selectButton.SetEnabled(canSelect);

            if (!canSelect)
            {
                selectButton.tooltip =
                    "A definition cannot be its own parent or be parented to one of its descendants.";
            }

            foldout.Add(selectButton);

            foreach (TDefinition child in definitions)
            {
                if (child == null ||
                    GetParent(child) != node ||
                    visited.Contains(child))
                {
                    continue;
                }

                VisualElement childNode =
                    BuildHierarchyNode(child, definitions, visited);

                if (childNode != null)
                    foldout.Add(childNode);
            }

            return foldout;
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
            RefreshParentTree();
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
