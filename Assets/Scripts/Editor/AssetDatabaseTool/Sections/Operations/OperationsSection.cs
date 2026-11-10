using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class OperationsSection : DatabaseToolSubsection<OperationDefinition>
    {
        public OperationsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
        }

        protected override string DefinitionTypeName => "Operation";

        protected override string GetDisplayName(OperationDefinition operation) => operation.DisplayName;

        protected override string GetIdentifier(OperationDefinition operation) => operation.OperationId;

        protected override IEnumerable<OperationDefinition> GetDefinitions()
        {
            return context.DatabaseConfig.OperationCatalog.Operations;
        }

        protected override string GetSectionTitle() => "Operations";

        protected override string GetSectionDescription() => "Create, find, and edit operations.";

        protected override void OpenForCreate(string displayName, Action<OperationDefinition> onClosed)
        {
            OperationEditorWindow.OpenForCreate(
                context.DatabaseConfig.OperationCatalog,
                displayName,
                onClosed);
        }

        protected override void OpenForEdit(OperationDefinition operation, Action<OperationDefinition> onClosed)
        {
            OperationEditorWindow.OpenForEdit(
                operation,
                context.DatabaseConfig.OperationCatalog,
                onClosed);
        }
    }
}