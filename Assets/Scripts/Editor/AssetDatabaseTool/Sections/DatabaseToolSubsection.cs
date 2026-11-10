using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public abstract class DatabaseToolSubsection<TDefinition> : DatabaseToolSection
        where TDefinition : class
    {
        private TaxonomySearchField<TDefinition> searchField;
        private Label feedbackLabel;

        protected DatabaseToolSubsection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
        }

        protected abstract string DefinitionTypeName { get; }
        protected abstract string GetDisplayName(TDefinition definition);
        protected abstract string GetIdentifier(TDefinition definition);
        protected abstract IEnumerable<TDefinition> GetDefinitions();

        protected abstract void OpenForCreate(string displayName, Action<TDefinition> onClosed);
        protected abstract void OpenForEdit(TDefinition definition, Action<TDefinition> onClosed);

        protected virtual IEnumerable<string> GetSynonyms(TDefinition definition)
        {
            yield break;
        }

        protected virtual bool IsValidDefinition(TDefinition definition)
        {
            return definition != null &&
                   !string.IsNullOrWhiteSpace(GetDisplayName(definition)) &&
                   !string.IsNullOrWhiteSpace(GetIdentifier(definition));
        }

        protected override void BuildContent()
        {
            if (!HasValidConfiguration())
                return;

            AddHeading($"Create or Find a {DefinitionTypeName}");

            searchField = new TaxonomySearchField<TDefinition>(
                $"{DefinitionTypeName} Display Name",
                GetDefinitions().Where(IsValidDefinition),
                GetDisplayName,
                GetIdentifier,
                GetSynonyms,
                definition => OpenForEdit(definition, _ => RefreshSection()),
                onSubmit: HandleSearchSubmit);

            root.Add(searchField.Root);

            feedbackLabel = new Label();
            feedbackLabel.style.whiteSpace = WhiteSpace.Normal;
            feedbackLabel.style.marginBottom = 8;
            root.Add(feedbackLabel);

            AddHeading($"Registered {DefinitionTypeName}s");
            BuildRegisteredList();
        }

        private void HandleSearchSubmit(string query)
        {
            string displayName = query?.Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                feedbackLabel.text = $"Enter a {DefinitionTypeName.ToLowerInvariant()} display name first.";
                return;
            }

            TDefinition exactMatch = FindExactMatch(displayName);

            if (exactMatch != null)
            {
                OpenForEdit(exactMatch, _ => RefreshSection());
                return;
            }

            OpenForCreate(displayName, _ => RefreshSection());
        }

        private TDefinition FindExactMatch(string query)
        {
            return GetDefinitions().FirstOrDefault(definition =>
                IsValidDefinition(definition) &&
                (string.Equals(GetDisplayName(definition), query, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(GetIdentifier(definition), query, StringComparison.OrdinalIgnoreCase)));
        }

        private void BuildRegisteredList()
        {
            var list = new ScrollView();
            list.style.maxHeight = 280;
            list.style.marginTop = 4;

            List<TDefinition> definitions = GetDefinitions()
                .Where(IsValidDefinition)
                .OrderBy(GetDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (definitions.Count == 0)
            {
                list.Add(new Label($"No {DefinitionTypeName.ToLowerInvariant()}s have been registered yet."));
            }
            else
            {
                foreach (TDefinition definition in definitions)
                {
                    TDefinition capturedDefinition = definition;

                    var button = new Button(() =>
                    {
                        OpenForEdit(capturedDefinition, _ => RefreshSection());
                    })
                    {
                        text = $"{GetDisplayName(definition)}  ({GetIdentifier(definition)})"
                    };

                    button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    list.Add(button);
                }
            }

            root.Add(list);
        }

        private void RefreshSection()
        {
            Build();
        }
    }
}