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
                        errors.Add($"Parameter '{parameter.DisplayName}' in operation '{operation.OperationId}' has no display name.");
                    }

                    errors.AddRange(ValidateParameter(parameter, operation.OperationId));
                }
            }

            return errors;
        }

        private static List<string> ValidateParameter(OperationParameterDefinition parameter, string operationId)
        {
            var errors = new List<string>();
            string parameterName = parameter.ParameterId;

            ParameterConfiguration configuration = parameter.GetConfiguration();

            if (configuration == null)
            {
                errors.Add($"Parameter '{parameterName}' in operation '{operationId}' has no configuration for type '{parameter.ParameterType}'.");
                return errors;
            }

            switch (parameter.ParameterType)
            {
                case OperationParameterType.Integer:
                    {
                        var config = configuration as IntegerParameterConfiguration;

                        if (config == null)
                            break;

                        if (config.HasMinimum && config.HasMaximum && config.Minimum > config.Maximum)
                            errors.Add($"Integer parameter '{parameterName}' in '{operationId}' has a minimum greater than its maximum.");

                        if (config.HasDefaultValue &&
                            ((config.HasMinimum && config.DefaultValue < config.Minimum) ||
                             (config.HasMaximum && config.DefaultValue > config.Maximum)))
                        {
                            errors.Add($"Integer parameter '{parameterName}' in '{operationId}' has a default value outside its configured range.");
                        }

                        break;
                    }

                case OperationParameterType.Float:
                    {
                        var config = configuration as FloatParameterConfiguration;

                        if (config == null)
                            break;

                        if ((config.HasMinimum && !IsFinite(config.Minimum)) ||
                            (config.HasMaximum && !IsFinite(config.Maximum)) ||
                            (config.HasDefaultValue && !IsFinite(config.DefaultValue)))
                        {
                            errors.Add($"Float parameter '{parameterName}' in '{operationId}' contains a non-finite value.");
                        }

                        if (config.HasMinimum && config.HasMaximum && config.Minimum > config.Maximum)
                            errors.Add($"Float parameter '{parameterName}' in '{operationId}' has a minimum greater than its maximum.");

                        if (config.HasDefaultValue &&
                            ((config.HasMinimum && config.DefaultValue < config.Minimum) ||
                             (config.HasMaximum && config.DefaultValue > config.Maximum)))
                        {
                            errors.Add($"Float parameter '{parameterName}' in '{operationId}' has a default value outside its configured range.");
                        }

                        break;
                    }

                case OperationParameterType.Vector3:
                    {
                        var config = configuration as Vector3ParameterConfiguration;

                        if (config == null)
                            break;

                        if ((config.HasMinimum && !IsFinite(config.Minimum)) ||
                            (config.HasMaximum && !IsFinite(config.Maximum)) ||
                            (config.HasDefaultValue && !IsFinite(config.DefaultValue)))
                        {
                            errors.Add($"Vector3 parameter '{parameterName}' in '{operationId}' contains a non-finite component.");
                        }

                        if (config.HasMinimum && config.HasMaximum &&
                            (config.Minimum.x > config.Maximum.x ||
                             config.Minimum.y > config.Maximum.y ||
                             config.Minimum.z > config.Maximum.z))
                        {
                            errors.Add($"Vector3 parameter '{parameterName}' in '{operationId}' has a minimum component greater than its maximum.");
                        }

                        if (config.HasDefaultValue &&
                            ((config.HasMinimum && !IsWithinRange(config.DefaultValue, config.Minimum, true)) ||
                             (config.HasMaximum && !IsWithinRange(config.DefaultValue, config.Maximum, false))))
                        {
                            errors.Add($"Vector3 parameter '{parameterName}' in '{operationId}' has a default value outside its configured range.");
                        }

                        break;
                    }

                case OperationParameterType.Enum:
                    {
                        var config = configuration as EnumParameterConfiguration;

                        if (config == null)
                            break;

                        if (config.AllowedValues == null || config.AllowedValues.Count == 0)
                        {
                            errors.Add($"Enum parameter '{parameterName}' in '{operationId}' has no allowed values.");
                        }
                        else if (config.AllowedValues.Exists(string.IsNullOrWhiteSpace))
                        {
                            errors.Add($"Enum parameter '{parameterName}' in '{operationId}' contains an empty allowed value.");
                        }

                        if (config.HasDefaultValue &&
                            (config.AllowedValues == null || !config.AllowedValues.Contains(config.DefaultValue)))
                        {
                            errors.Add($"Enum parameter '{parameterName}' in '{operationId}' has a default value that is not in its allowed values.");
                        }

                        break;
                    }

                case OperationParameterType.String:
                    {
                        var config = configuration as StringParameterConfiguration;

                        if (config == null)
                            break;

                        if (config.HasDefaultValue &&
                            config.AllowedValues != null &&
                            config.AllowedValues.Count > 0 &&
                            !config.AllowedValues.Contains(config.DefaultValue))
                        {
                            errors.Add($"String parameter '{parameterName}' in '{operationId}' has a default value that is not in its allowed values.");
                        }

                        break;
                    }

                case OperationParameterType.AssetReference:
                    {
                        var config = configuration as AssetReferenceParameterConfiguration;

                        if (config == null)
                            break;

                        if (config.HasDefaultValue &&
                            config.AllowedValues != null &&
                            config.AllowedValues.Count > 0 &&
                            !config.AllowedValues.Contains(config.DefaultValue))
                        {
                            errors.Add($"Asset reference parameter '{parameterName}' in '{operationId}' has a default value that is not in its allowed values.");
                        }

                        break;
                    }

                case OperationParameterType.Boolean:
                    {
                        if (configuration is not BooleanParameterConfiguration)
                            errors.Add($"Boolean parameter '{parameterName}' in '{operationId}' has an invalid configuration type.");

                        break;
                    }

                case OperationParameterType.Color:
                    {
                        var config = configuration as ColorParameterConfiguration;

                        if (config == null)
                            break;

                        if (config.HasDefaultValue && !IsFinite(config.DefaultValue))
                            errors.Add($"Color parameter '{parameterName}' in '{operationId}' contains a non-finite default value.");

                        break;
                    }
            }

            return errors;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Color value)
        {
            return IsFinite(value.r) &&
                   IsFinite(value.g) &&
                   IsFinite(value.b) &&
                   IsFinite(value.a);
        }

        private static bool IsWithinRange(Vector3 value, Vector3 bound, bool isMinimum)
        {
            if (isMinimum)
                return value.x >= bound.x && value.y >= bound.y && value.z >= bound.z;

            return value.x <= bound.x && value.y <= bound.y && value.z <= bound.z;
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
