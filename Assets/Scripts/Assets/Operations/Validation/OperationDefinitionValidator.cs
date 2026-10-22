using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype
{
    public static class OperationDefinitionValidator
    {
        public static List<string> Validate(OperationCatalog catalog)
        {
            var errors = new List<string>();

            if (catalog == null)
            {
                errors.Add("Operation catalog is null.");
                return errors;
            }

            var operationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (OperationDefinition operation in catalog.Operations)
            {
                if (operation == null)
                {
                    errors.Add("Operation catalog contains a null entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(operation.OperationId))
                {
                    errors.Add($"Operation '{operation.name}' has no operation ID.");
                }
                else if (!operationIds.Add(operation.OperationId))
                {
                    errors.Add($"Duplicate operation ID '{operation.OperationId}'.");
                }

                if (string.IsNullOrWhiteSpace(operation.DisplayName))
                {
                    errors.Add($"Operation '{operation.name}' has no display name.");
                }

                if (string.IsNullOrWhiteSpace(operation.ExecutorId))
                {
                    errors.Add($"Operation '{operation.name}' has no executor ID.");
                }

                var parameterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (OperationParameterDefinition parameter in operation.Parameters)
                {
                    if (parameter == null)
                    {
                        errors.Add($"Operation '{operation.OperationId}' contains a null parameter.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(parameter.ParameterId))
                    {
                        errors.Add($"An operation parameter in '{operation.OperationId}' has no ID.");
                    }
                    else if (!parameterIds.Add(parameter.ParameterId))
                    {
                        errors.Add($"Operation '{operation.OperationId}' has duplicate parameter ID '{parameter.ParameterId}'.");
                    }

                    if (string.IsNullOrWhiteSpace(parameter.DisplayName))
                    {
                        errors.Add($"Parameter '{parameter.name}' in operation '{operation.OperationId}' has no display name.");
                    }

                    if (parameter.HasMinimum &&
                        parameter.HasMaximum &&
                        parameter.Minimum > parameter.Maximum)
                    {
                        errors.Add($"Parameter '{parameter.ParameterId}' in '{operation.OperationId}' has a minimum greater than its maximum.");
                    }

                    if (parameter.ParameterType == OperationParameterType.Enum &&
                        (parameter.AllowedValues == null ||
                         parameter.AllowedValues.Length == 0))
                    {
                        errors.Add($"Enum parameter '{parameter.ParameterId}' in '{operation.OperationId}' has no allowed values.");
                    }
                }
            }

            return errors;
        }

        public static List<string> ValidateAssetCompatibility(AssetCatalog assetCatalog, OperationCatalog operationCatalog)
        {
            var errors = new List<string>();

            if (assetCatalog == null)
            {
                errors.Add("Asset catalog is null.");
                return errors;
            }

            if (operationCatalog == null)
            {
                errors.Add("Operation catalog is null.");
                return errors;
            }

            foreach (AssetDefinition asset in assetCatalog.Assets)
            {
                if (asset == null)
                {
                    errors.Add("Asset catalog contains a null entry.");
                    continue;
                }

                foreach (string operationId in asset.SupportedOperationIds)
                {
                    if (string.IsNullOrWhiteSpace(operationId))
                    {
                        errors.Add($"Asset '{asset.AssetId}' has an empty supported operation ID.");
                        continue;
                    }

                    if (!operationCatalog.TryGetById(operationId, out OperationDefinition operation))
                    {
                        errors.Add($"Asset '{asset.AssetId}' references unknown operation '{operationId}'.");
                        continue;
                    }

                    if (operation == null)
                    {
                        errors.Add($"Asset '{asset.AssetId}' references an invalid operation '{operationId}'.");
                    }
                }
            }

            return errors;
        }
    }
}
