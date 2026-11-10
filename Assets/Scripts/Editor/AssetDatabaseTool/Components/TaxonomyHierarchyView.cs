using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    // reusable expandable hierarchy
    public sealed class TaxonomyHierarchyView<T> where T : class
    {
        private readonly List<T> definitions;
        private readonly Func<T, string> getDisplayName;
        private readonly Func<T, T> getParent;
        private readonly Func<T, string> getId;
        private readonly Action<T> onSelected;
        private readonly Func<T, bool> isSelected;
        private readonly Func<T, bool> canSelect;

        private readonly VisualElement root;

        public VisualElement Root => root;

        private string whatIsBeingSelected = "";

        public TaxonomyHierarchyView(
            IEnumerable<T> definitions,
            Func<T, string> getDisplayName,
            Func<T, T> getParent,
            Func<T, string> getId,
            Action<T> onSelected,
            Func<T, bool> isSelected = null,
            Func<T, bool> canSelect = null,
            string whatIsBeingSelected = "")
        {
            if (getDisplayName == null)
                throw new ArgumentNullException(nameof(getDisplayName));

            if (getParent == null)
                throw new ArgumentNullException(nameof(getParent));

            this.definitions = definitions?.Where(item => item != null).ToList()
                ?? new List<T>();

            this.getDisplayName = getDisplayName;
            this.getParent = getParent;
            this.getId = getId;
            this.onSelected = onSelected;
            this.isSelected = isSelected;
            this.canSelect = canSelect;
            this.whatIsBeingSelected = whatIsBeingSelected;

            root = new VisualElement();
            root.style.flexGrow = 1;

            Build();
        }

        public void Refresh()
        {
            Build();
        }

        private void Build()
        {
            root.Clear();

            if (definitions.Count == 0)
            {
                root.Add(new Label("No " + whatIsBeingSelected + " definitions available."));
                return;
            }

            var visited = new HashSet<T>();

            List<T> roots = definitions
                .Where(item =>
                {
                    T parent = getParent(item);
                    return parent == null || !definitions.Contains(parent);
                })
                .OrderBy(getDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (T definition in roots)
            {
                VisualElement node = BuildNode(
                    definition,
                    visited,
                    new HashSet<T>());

                if (node != null)
                    root.Add(node);
            }

            // Include definitions that are not reachable from a root.
            // This also makes malformed or cyclic hierarchies visible.
            List<T> unvisited = definitions
                .Where(item => !visited.Contains(item))
                .ToList();

            if (unvisited.Count > 0)
            {
                var invalidGroup = new Foldout
                {
                    text = "Other " + whatIsBeingSelected + " definitions (check hierarchy)",
                    value = true
                };

                foreach (T definition in unvisited)
                {
                    if (visited.Contains(definition))
                        continue;

                    VisualElement node = BuildNode(
                        definition,
                        visited,
                        new HashSet<T>());

                    if (node != null)
                        invalidGroup.Add(node);
                }

                root.Add(invalidGroup);
            }
        }

        private VisualElement BuildNode(T definition, HashSet<T> visited, HashSet<T> currentPath)
        {
            if (definition == null || currentPath.Contains(definition) || visited.Contains(definition))
                return null;

            currentPath.Add(definition);
            visited.Add(definition);

            List<T> children = definitions
                .Where(item => item != null && !ReferenceEquals(item, definition) && ReferenceEquals(getParent(item), definition))
                .OrderBy(getDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nodeContainer = new VisualElement();
            nodeContainer.style.marginLeft = 4;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.minHeight = 26;

            VisualElement childrenContainer = null;
            Button expandButton = null;

            if (children.Count > 0)
            {
                expandButton = new Button { text = "▶" };
                expandButton.style.width = 24;
                expandButton.style.minWidth = 24;
                expandButton.style.marginRight = 4;
                row.Add(expandButton);

                childrenContainer = new VisualElement();
                childrenContainer.style.marginLeft = 24;
                childrenContainer.style.display = DisplayStyle.None;

                foreach (T child in children)
                {
                    VisualElement childNode = BuildNode(child, visited, currentPath);
                    if (childNode != null)
                        childrenContainer.Add(childNode);
                }

                expandButton.clicked += () =>
                {
                    bool expanded = childrenContainer.style.display == DisplayStyle.Flex;
                    childrenContainer.style.display = expanded ? DisplayStyle.None : DisplayStyle.Flex;
                    expandButton.text = expanded ? "▶" : "▼";
                };
            }
            else
            {
                var spacer = new VisualElement();
                spacer.style.width = 28;
                row.Add(spacer);
            }

            bool selected = isSelected?.Invoke(definition) == true;
            bool selectable = canSelect?.Invoke(definition) != false;

            var nameLabel = new Label(getDisplayName(definition));
            nameLabel.style.flexGrow = 1;
            row.Add(nameLabel);

            if (getId != null)
            {
                string id = getId(definition);

                if (!string.IsNullOrWhiteSpace(id))
                {
                    var idLabel = new Label(id);
                    idLabel.style.fontSize = 10;
                    idLabel.style.opacity = 0.6f;
                    idLabel.style.marginLeft = 6;
                    row.Add(idLabel);
                }
            }

            var selectButton = new Button(() => onSelected?.Invoke(definition))
            {
                text = selected ? "Current " + whatIsBeingSelected : "Select " + whatIsBeingSelected
            };

            selectButton.style.minWidth = 100;
            selectButton.SetEnabled(selectable);
            row.Add(selectButton);

            // Always display the node's own row, including leaf nodes.
            nodeContainer.Add(row);

            // Only the children are initially collapsed.
            if (childrenContainer != null)
                nodeContainer.Add(childrenContainer);

            currentPath.Remove(definition);
            return nodeContainer;
        }
    }
}
