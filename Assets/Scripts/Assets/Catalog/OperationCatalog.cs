using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    [CreateAssetMenu(fileName = "OperationCatalog", menuName = "LLM VR/Operations/Operation Catalog")]
    public class OperationCatalog : ScriptableObject
    {
        [SerializeField]
        private List<OperationDefinition> operations = new();

        public IReadOnlyList<OperationDefinition> Operations => operations;

        private Dictionary<string, OperationDefinition> operationsById;

        public void RebuildIndex()
        {
            operationsById =
                new Dictionary<string, OperationDefinition>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (OperationDefinition operation in operations)
            {
                if (operation == null)
                    continue;

                if (string.IsNullOrWhiteSpace(operation.OperationId))
                    continue;

                if (operationsById.ContainsKey(operation.OperationId))
                {
                    Debug.LogError(
                        $"Duplicate operation ID: '{operation.OperationId}'.",
                        operation);

                    continue;
                }

                operationsById.Add(
                    operation.OperationId,
                    operation);
            }
        }

        public bool TryGetById(
            string operationId,
            out OperationDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(operationId))
                return false;

            if (operationsById == null)
                RebuildIndex();

            return operationsById.TryGetValue(
                operationId,
                out definition);
        }

        public List<OperationDefinition> GetEnabledOperations()
        {
            var result = new List<OperationDefinition>();

            foreach (OperationDefinition operation in operations)
            {
                if (operation != null && operation.IsEnabled)
                    result.Add(operation);
            }

            return result;
        }

        public void AddOperation(OperationDefinition operation)
        {
            operations.Add(operation);
            RebuildIndex();
        }
    }
}
