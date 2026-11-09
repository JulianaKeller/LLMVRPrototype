using LLMVRPrototype.Assets;
using LLMVRPrototype.EditorTools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public sealed class OperationEditorWindow : MetadataEditorWindow<OperationDefinition>
    {
        private const string OperationFolder = DatabaseFolder + "/Operations";

        private OperationCatalog registry;
        private VisualElement parametersContainer;

        protected override string DefinitionTypeName => "Operation";
        protected override string IdPropertyName => "operationId";
        protected override string DisplayNamePropertyName => "displayName";
        protected override string DescriptionPropertyName => "description";
        protected override string DefaultAssetFolder => OperationFolder;

        protected override void OnDefinitionSaved(OperationDefinition item)
        {
        }

        public static void OpenForCreate(
            OperationCatalog registry,
            string displayName,
            Action<OperationDefinition> onSaved = null)
        {
            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var window = CreateInstance<OperationEditorWindow>();
            window.registry = registry;
            window.InitializeCreate(displayName, onSaved);
            window.ShowEditor("Create Operation", true);
        }

        public static void OpenForEdit(
            OperationDefinition operation,
            OperationCatalog registry,
            Action<OperationDefinition> onSaved = null)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (registry == null)
                throw new ArgumentNullException(nameof(registry));

            var window = CreateInstance<OperationEditorWindow>();
            window.registry = registry;
            window.InitializeEdit(operation, onSaved);
            window.ShowEditor("Edit Operation");
        }

        protected override string GetDefinitionId(OperationDefinition item)
        {
            return item.OperationId;
        }

        protected override string GetDefinitionDisplayName(OperationDefinition item)
        {
            return item.DisplayName;
        }

        protected override void BuildSpecificFields(VisualElement container)
        {
            AddProperty(container, "targetType", "Target Type");
            AddProperty(container, "requiresTarget", "Requires Target");

            BuildParameterList(container);

            AddProperty(container, "isEnabled", "Enabled");
            AddProperty(container, "promptPolicy", "Prompt Policy");
            AddProperty(container, "executorId", "Executor ID");
        }

        #region Parameter UI

        private void BuildParameterList(VisualElement parent)
        {
            var section = new VisualElement();
            section.style.marginTop = 10;
            section.style.marginBottom = 10;

            var heading = new Label("Parameters");
            heading.style.fontSize = 14;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6;
            section.Add(heading);

            parametersContainer = new VisualElement();
            section.Add(parametersContainer);

            var addButton = new Button(AddParameter)
            {
                text = "Add Parameter"
            };

            addButton.style.marginTop = 6;
            section.Add(addButton);

            parent.Add(section);

            RefreshParameterList();
        }

        private void RefreshParameterList()
        {
            if (parametersContainer == null || serializedDefinition == null)
                return;

            parametersContainer.Clear();

            SerializedProperty parametersProperty =
                serializedDefinition.FindProperty("parameters");

            if (parametersProperty == null || !parametersProperty.isArray)
            {
                parametersContainer.Add(
                    new HelpBox("Could not find the serialized parameters list.", HelpBoxMessageType.Error));
                return;
            }

            if (parametersProperty.arraySize == 0)
            {
                var emptyLabel = new Label("No parameters defined.");
                emptyLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
                emptyLabel.style.marginBottom = 6;
                parametersContainer.Add(emptyLabel);
                return;
            }

            for (int i = 0; i < parametersProperty.arraySize; i++)
            {
                int parameterIndex = i;

                SerializedProperty parameterProperty =
                    parametersProperty.GetArrayElementAtIndex(parameterIndex);

                BuildParameterElement(parametersContainer, parameterProperty, parameterIndex);
            }
        }

        private void BuildParameterElement(VisualElement parent, SerializedProperty parameterProperty, int index)
        {
            SerializedProperty displayNameProperty = parameterProperty.FindPropertyRelative("displayName");
            string displayName = displayNameProperty != null && !string.IsNullOrWhiteSpace(displayNameProperty.stringValue)
                ? displayNameProperty.stringValue
                : $"Parameter {index + 1}";

            var foldout = new Foldout { text = displayName, value = true };
            foldout.style.marginBottom = 5;
            foldout.style.paddingLeft = 4;
            foldout.style.paddingRight = 4;

            AddParameterProperty(foldout, parameterProperty, "parameterId", "Parameter ID");
            AddParameterProperty(foldout, parameterProperty, "displayName", "Display Name");
            AddParameterProperty(foldout, parameterProperty, "description", "Description");
            AddParameterProperty(foldout, parameterProperty, "parameterType", "Parameter Type");
            AddParameterProperty(foldout, parameterProperty, "isRequired", "Required");

            SerializedProperty parameterTypeProperty = parameterProperty.FindPropertyRelative("parameterType");

            var configurationFields = new VisualElement();
            configurationFields.style.marginLeft = 10;
            foldout.Add(configurationFields);

            var removeButton = new Button(() => RemoveParameter(index))
            {
                text = "Remove Parameter"
            };
            removeButton.style.marginTop = 6;
            removeButton.style.marginBottom = 6;
            removeButton.style.color = new Color(0.8f, 0.2f, 0.2f);
            foldout.Add(removeButton);

            parent.Add(foldout);

            void UpdateConfigurationFields()
            {
                configurationFields.Clear();

                if (parameterTypeProperty == null ||
                    parameterTypeProperty.propertyType != SerializedPropertyType.Enum)
                    return;

                string typeName = parameterTypeProperty.enumNames[parameterTypeProperty.enumValueIndex];

                string configurationName = OperationParameterDefinition.GetConfigurationPropertyName((OperationParameterType)Enum.Parse(typeof(OperationParameterType), typeName));

                if (configurationName == null)
                    return;

                SerializedProperty configurationProperty =
                    parameterProperty.FindPropertyRelative(configurationName);

                if (configurationProperty == null)
                {
                    configurationFields.Add(new HelpBox(
                        $"Configuration '{configurationName}' was not found.",
                        HelpBoxMessageType.Error));
                    return;
                }

                bool supportsMinimumMaximum =
                    typeName == nameof(OperationParameterType.Integer) ||
                    typeName == nameof(OperationParameterType.Float) ||
                    typeName == nameof(OperationParameterType.Vector3);

                bool supportsAllowedValues =
                    typeName != nameof(OperationParameterType.Boolean);

                if (supportsMinimumMaximum)
                {
                    AddConfigurationProperty(configurationFields, configurationProperty, "HasMinimum", "Enable Minimum");
                    PropertyField minimumField = AddConfigurationProperty(configurationFields, configurationProperty, "Minimum", "Minimum");

                    AddConfigurationProperty(configurationFields, configurationProperty, "HasMaximum", "Enable Maximum");
                    PropertyField maximumField = AddConfigurationProperty(configurationFields, configurationProperty, "Maximum", "Maximum");

                    SerializedProperty hasMinimumProperty = configurationProperty.FindPropertyRelative("HasMinimum");
                    SerializedProperty hasMaximumProperty = configurationProperty.FindPropertyRelative("HasMaximum");

                    void UpdateConstraintVisibility()
                    {
                        if (minimumField != null)
                            minimumField.style.display = hasMinimumProperty != null && hasMinimumProperty.boolValue
                                ? DisplayStyle.Flex : DisplayStyle.None;

                        if (maximumField != null)
                            maximumField.style.display = hasMaximumProperty != null && hasMaximumProperty.boolValue
                                ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    if (hasMinimumProperty != null)
                        configurationFields.TrackPropertyValue(hasMinimumProperty, _ => UpdateConstraintVisibility());

                    if (hasMaximumProperty != null)
                        configurationFields.TrackPropertyValue(hasMaximumProperty, _ => UpdateConstraintVisibility());

                    UpdateConstraintVisibility();
                }

                AddConfigurationProperty(configurationFields, configurationProperty, "HasDefaultValue", "Enable Default Value");
                PropertyField defaultValueField = AddConfigurationProperty(configurationFields, configurationProperty, "DefaultValue", "Default Value");

                SerializedProperty hasDefaultValueProperty =
                    configurationProperty.FindPropertyRelative("HasDefaultValue");

                void UpdateDefaultVisibility()
                {
                    if (defaultValueField != null)
                        defaultValueField.style.display =
                            hasDefaultValueProperty != null && hasDefaultValueProperty.boolValue
                                ? DisplayStyle.Flex
                                : DisplayStyle.None;
                }

                if (hasDefaultValueProperty != null)
                    configurationFields.TrackPropertyValue(hasDefaultValueProperty, _ => UpdateDefaultVisibility());

                UpdateDefaultVisibility();

                if (supportsAllowedValues)
                    AddConfigurationProperty(configurationFields, configurationProperty, "AllowedValues", "Allowed Values");
            }

            if (parameterTypeProperty != null)
                foldout.TrackPropertyValue(parameterTypeProperty, _ => UpdateConfigurationFields());

            UpdateConfigurationFields();
        }

        // Adding fields from nested parameterconfiguration fields
        private PropertyField AddConfigurationProperty(
            VisualElement parent,
            SerializedProperty configurationProperty,
            string propertyName,
            string label)
        {
            SerializedProperty property = configurationProperty.FindPropertyRelative(propertyName);

            if (property == null)
            {
                Debug.LogWarning($"Configuration property '{propertyName}' was not found.");
                return null;
            }

            var field = new PropertyField(property, label)
            {
                name = propertyName
            };

            field.style.marginBottom = 4;
            parent.Add(field);
            field.Bind(serializedDefinition);

            return field;
        }

        // Adding fields from the parameter definition itself
        private PropertyField AddParameterProperty(
            VisualElement parent,
            SerializedProperty parameterProperty,
            string propertyName,
            string label)
        {
            SerializedProperty property = parameterProperty.FindPropertyRelative(propertyName);

            if (property == null)
            {
                Debug.LogWarning($"OperationParameterDefinition property '{propertyName}' was not found.");
                return null;
            }

            var field = new PropertyField(property, label)
            {
                name = propertyName
            };

            field.style.marginBottom = 4;
            parent.Add(field);
            field.Bind(serializedDefinition);

            return field;
        }

        private void AddParameter()
        {
            if (serializedDefinition == null)
                return;

            serializedDefinition.Update();

            SerializedProperty parametersProperty =
                serializedDefinition.FindProperty("parameters");

            if (parametersProperty == null || !parametersProperty.isArray)
                return;

            int index = parametersProperty.arraySize;
            parametersProperty.InsertArrayElementAtIndex(index);

            SerializedProperty parameterProperty =
                parametersProperty.GetArrayElementAtIndex(index);

            SetStringProperty(parameterProperty, "parameterId", GenerateParameterId());
            SetStringProperty(parameterProperty, "displayName", "New Parameter");
            SetStringProperty(parameterProperty, "description", string.Empty);
            SetEnumProperty(parameterProperty, "parameterType", OperationParameterType.String);
            SetBoolProperty(parameterProperty, "isRequired", true);

            ResetParameterProperties(parameterProperty);
            serializedDefinition.ApplyModifiedProperties();

            RefreshParameterList();
        }

        private void RemoveParameter(int index)
        {
            if (serializedDefinition == null)
                return;

            serializedDefinition.Update();

            SerializedProperty parametersProperty =
                serializedDefinition.FindProperty("parameters");

            if (parametersProperty == null ||
                !parametersProperty.isArray ||
                index < 0 ||
                index >= parametersProperty.arraySize)
                return;

            parametersProperty.DeleteArrayElementAtIndex(index);

            serializedDefinition.ApplyModifiedProperties();

            RefreshParameterList();
        }

        private string GenerateParameterId()
        {
            string slug = MakeSlug(definition.DisplayName);

            if (string.IsNullOrEmpty(slug))
                slug = "parameter";

            return $"{slug}-{Guid.NewGuid():N}";
        }

        #endregion

        #region Metadata Generation

        protected override MetadataGenerationInput GetMetadataInput()
        {
            if (definition == null)
                return null;

            IEnumerable<string> parameterNames =
                definition.Parameters == null
                    ? Enumerable.Empty<string>()
                    : definition.Parameters
                        .Where(parameter => parameter != null)
                        .Select(parameter => parameter.DisplayName);

            return new OperationMetadataGenerationInput(
                definition.DisplayName,
                definition.Description,
                definition.TargetType.ToString(),
                definition.RequiresTarget,
                parameterNames);
        }

        protected override void OnGeneratedMetadataApplied(GeneratedMetadata metadata)
        {
            if (!(metadata is GeneratedOperationMetadata operationMetadata))
                return;

            ApplyGeneratedOperationSettings(operationMetadata);
            ApplyGeneratedParameters(operationMetadata.Parameters);

            RefreshParameterList();
        }

        private void ApplyGeneratedOperationSettings(GeneratedOperationMetadata metadata)
        {
            if (serializedDefinition == null)
                return;

            serializedDefinition.Update();

            SerializedProperty targetTypeProperty = serializedDefinition.FindProperty("targetType");

            if (targetTypeProperty != null && targetTypeProperty.propertyType == SerializedPropertyType.Enum)
            {
                int enumIndex = Array.IndexOf(
                    targetTypeProperty.enumNames,
                    metadata.TargetType.ToString());

                if (enumIndex >= 0)
                    targetTypeProperty.enumValueIndex = enumIndex;
            }

            SerializedProperty requiresTargetProperty = serializedDefinition.FindProperty("requiresTarget");

            if (requiresTargetProperty != null)
                requiresTargetProperty.boolValue = metadata.RequiresTarget;

            serializedDefinition.ApplyModifiedProperties();
        }

        private void ApplyGeneratedParameters(List<GeneratedOperationParameterMetadata> generatedParameters)
        {
            if (serializedDefinition == null || generatedParameters == null || generatedParameters.Count == 0)
                return;

            serializedDefinition.Update();

            SerializedProperty parametersProperty = serializedDefinition.FindProperty("parameters");

            if (parametersProperty == null || !parametersProperty.isArray)
            {
                Debug.LogError("Could not find the serialized operation parameters list.");
                return;
            }

            // Store indices rather than SerializedProperty references.
            var existingParameters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < parametersProperty.arraySize; i++)
            {
                SerializedProperty element = parametersProperty.GetArrayElementAtIndex(i);
                SerializedProperty idProperty = element.FindPropertyRelative("parameterId");

                if (idProperty != null && !string.IsNullOrWhiteSpace(idProperty.stringValue))
                    existingParameters[idProperty.stringValue] = i;
            }

            bool changed = false;

            foreach (GeneratedOperationParameterMetadata generatedParameter in generatedParameters)
            {
                if (!ValidateGeneratedParameter(generatedParameter))
                    continue;

                if (existingParameters.TryGetValue(generatedParameter.ParameterId, out int existingIndex))
                {
                    SerializedProperty existingProperty = parametersProperty.GetArrayElementAtIndex(existingIndex);

                    SerializedProperty typeProperty = existingProperty.FindPropertyRelative("parameterType");

                    if (typeProperty == null)
                        continue;

                    string existingTypeName = typeProperty.enumNames[typeProperty.enumValueIndex];
                    string generatedTypeName = generatedParameter.ParameterType.ToString();

                    if (!string.Equals(existingTypeName, generatedTypeName, StringComparison.Ordinal))
                    {
                        Debug.LogWarning(
                            $"Generated parameter '{generatedParameter.ParameterId}' has type '{generatedTypeName}', " +
                            $"but the existing parameter has type '{existingTypeName}'. The existing parameter was not modified.");

                        continue;
                    }

                    changed |= EnrichParameterProperties(existingProperty, generatedParameter);
                    continue;
                }

                int newIndex = parametersProperty.arraySize;
                parametersProperty.InsertArrayElementAtIndex(newIndex);

                SerializedProperty newProperty = parametersProperty.GetArrayElementAtIndex(newIndex);

                ResetParameterProperties(newProperty);

                if (!PopulateParameterProperties(newProperty, generatedParameter))
                {
                    parametersProperty.DeleteArrayElementAtIndex(newIndex);
                    continue;
                }

                existingParameters[generatedParameter.ParameterId] = newIndex;
                changed = true;
            }

            if (changed)
            {
                serializedDefinition.ApplyModifiedProperties();
                serializedDefinition.Update();
            }
        }

        private void ResetParameterProperties(SerializedProperty parameterProperty)
        {
            foreach (string configurationName in OperationParameterDefinition.ListConfigurationNames())
            {
                SerializedProperty configuration = parameterProperty.FindPropertyRelative(configurationName);

                if (configuration == null)
                    continue;

                SetBooleanProperty(configuration, "HasMinimum", false);
                SetBooleanProperty(configuration, "HasMaximum", false);
                SetBooleanProperty(configuration, "HasDefaultValue", false);

                ResetValueProperty(configuration.FindPropertyRelative("Minimum"));
                ResetValueProperty(configuration.FindPropertyRelative("Maximum"));
                ResetValueProperty(configuration.FindPropertyRelative("DefaultValue"));

                SerializedProperty allowedValues = configuration.FindPropertyRelative("AllowedValues");

                if (allowedValues != null && allowedValues.isArray)
                    allowedValues.arraySize = 0;
            }
        }

        private void SetBooleanProperty(SerializedProperty parent, string propertyName, bool value)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);

            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
                property.boolValue = value;
        }

        private void ResetValueProperty(SerializedProperty property)
        {
            if (property == null)
                return;

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    break;

                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;

                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    break;

                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;

                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    break;

                case SerializedPropertyType.Color:
                    property.colorValue = Color.clear;
                    break;
            }
        }

        private bool EnrichParameterProperties(
    SerializedProperty parameterProperty,
    GeneratedOperationParameterMetadata generatedParameter)
        {
            bool changed = false;

            void SetStringIfEmpty(string propertyName, string generatedValue)
            {
                SerializedProperty property =
                    parameterProperty.FindPropertyRelative(propertyName);

                if (property != null &&
                    string.IsNullOrWhiteSpace(property.stringValue) &&
                    !string.IsNullOrWhiteSpace(generatedValue))
                {
                    property.stringValue = generatedValue;
                    changed = true;
                }
            }

            SetStringIfEmpty("displayName", generatedParameter.DisplayName);
            SetStringIfEmpty("description", generatedParameter.Description);

            SerializedProperty configuration = parameterProperty.FindPropertyRelative(OperationParameterDefinition.GetConfigurationPropertyName(generatedParameter.ParameterType));

            if (configuration == null)
                return changed;

            EnrichConstraint(
                configuration,
                "HasMinimum",
                "Minimum",
                generatedParameter.HasMinimum,
                generatedParameter.Minimum,
                ref changed);

            EnrichConstraint(
                configuration,
                "HasMaximum",
                "Maximum",
                generatedParameter.HasMaximum,
                generatedParameter.Maximum,
                ref changed);

            SerializedProperty hasDefaultProperty =
                configuration.FindPropertyRelative("HasDefaultValue");

            if (hasDefaultProperty != null &&
                !hasDefaultProperty.boolValue &&
                generatedParameter.HasDefaultValue)
            {
                SerializedProperty defaultProperty =
                    configuration.FindPropertyRelative("DefaultValue");

                if (SetTokenValue(defaultProperty, generatedParameter.DefaultValue))
                {
                    hasDefaultProperty.boolValue = true;
                    changed = true;
                }
            }

            SerializedProperty allowedValuesProperty =
                configuration.FindPropertyRelative("AllowedValues");

            if (allowedValuesProperty != null &&
                allowedValuesProperty.isArray &&
                allowedValuesProperty.arraySize == 0 &&
                generatedParameter.AllowedValues != null &&
                generatedParameter.AllowedValues.Count > 0)
            {
                if (SetTokenArrayProperty(
                    configuration,
                    "AllowedValues",
                    generatedParameter.AllowedValues))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private bool SetTokenValue(SerializedProperty property, JToken token)
        {
            if (property == null || token == null || token.Type == JTokenType.Null)
                return false;

            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.String:
                        if (token.Type != JTokenType.String)
                            return false;

                        property.stringValue = token.Value<string>();
                        return true;

                    case SerializedPropertyType.Boolean:
                        if (token.Type != JTokenType.Boolean)
                            return false;

                        property.boolValue = token.Value<bool>();
                        return true;

                    case SerializedPropertyType.Integer:
                        if (token.Type != JTokenType.Integer)
                            return false;

                        property.intValue = token.Value<int>();
                        return true;

                    case SerializedPropertyType.Float:
                        if (!IsNumericToken(token))
                            return false;

                        property.floatValue = token.Value<float>();
                        return true;

                    case SerializedPropertyType.Vector3:
                        if (!IsNumericArray(token, 3))
                            return false;

                        property.vector3Value = new Vector3(
                            token[0].Value<float>(),
                            token[1].Value<float>(),
                            token[2].Value<float>());

                        return true;

                    case SerializedPropertyType.Color:
                        if (!IsNumericArray(token, 4))
                            return false;

                        property.colorValue = new Color(
                            token[0].Value<float>(),
                            token[1].Value<float>(),
                            token[2].Value<float>(),
                            token[3].Value<float>());

                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not convert generated value '{token}' to {property.propertyType}: {exception.Message}");
                return false;
            }
        }

        private bool SetTokenArrayProperty(
            SerializedProperty parent,
            string propertyName,
            IReadOnlyList<JToken> values)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);

            if (property == null || !property.isArray ||
                property.propertyType == SerializedPropertyType.String)
                return false;

            int originalSize = property.arraySize;
            property.arraySize = values?.Count ?? 0;

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);

                if (!SetTokenValue(element, values[i]))
                {
                    property.arraySize = originalSize;
                    return false;
                }
            }

            return true;
        }

        private void EnrichConstraint(
            SerializedProperty configuration,
            string flagName,
            string valueName,
            bool generatedFlag,
            JToken generatedValue,
            ref bool changed)
        {
            if (!generatedFlag || generatedValue == null)
                return;

            SerializedProperty flagProperty =
                configuration.FindPropertyRelative(flagName);

            SerializedProperty valueProperty =
                configuration.FindPropertyRelative(valueName);

            if (flagProperty == null || valueProperty == null)
                return;

            if (flagProperty.boolValue)
                return;

            if (SetTokenValue(valueProperty, generatedValue))
            {
                flagProperty.boolValue = true;
                changed = true;
            }
        }

        private bool ValidateGeneratedParameter(GeneratedOperationParameterMetadata parameter)
        {
            if (parameter == null)
                return false;

            if (string.IsNullOrWhiteSpace(parameter.ParameterId) ||
                string.IsNullOrWhiteSpace(parameter.DisplayName))
                return false;

            if (!Enum.IsDefined(typeof(OperationParameterType), parameter.ParameterType))
                return false;

            bool supportsConstraints =
                parameter.ParameterType == OperationParameterType.Integer ||
                parameter.ParameterType == OperationParameterType.Float ||
                parameter.ParameterType == OperationParameterType.Vector3;

            if (!supportsConstraints && (parameter.HasMinimum || parameter.HasMaximum))
            {
                Debug.LogWarning(
                    $"Parameter '{parameter.ParameterId}' defines minimum or maximum constraints for an unsupported type.");

                return false;
            }

            if (parameter.HasMinimum && !IsValidValue(parameter.Minimum, parameter.ParameterType))
                return false;

            if (parameter.HasMaximum && !IsValidValue(parameter.Maximum, parameter.ParameterType))
                return false;

            if (parameter.HasMinimum && parameter.HasMaximum)
            {
                if (!AreConstraintsValid(parameter.Minimum, parameter.Maximum, parameter.ParameterType))
                {
                    Debug.LogWarning(
                        $"Parameter '{parameter.ParameterId}' has invalid minimum and maximum values.");

                    return false;
                }
            }

            if (parameter.HasDefaultValue &&
                !IsValidValue(parameter.DefaultValue, parameter.ParameterType))
            {
                Debug.LogWarning(
                    $"Parameter '{parameter.ParameterId}' has an invalid default value.");

                return false;
            }

            if (parameter.AllowedValues != null)
            {
                foreach (JToken allowedValue in parameter.AllowedValues)
                {
                    if (!IsValidValue(allowedValue, parameter.ParameterType))
                    {
                        Debug.LogWarning(
                            $"Parameter '{parameter.ParameterId}' contains an invalid allowed value.");

                        return false;
                    }
                }
            }

            if (parameter.ParameterType == OperationParameterType.Enum)
            {
                if (parameter.AllowedValues == null || parameter.AllowedValues.Count == 0)
                {
                    Debug.LogWarning(
                        $"Enum parameter '{parameter.ParameterId}' must define at least one allowed value.");

                    return false;
                }

                if (parameter.HasDefaultValue &&
                    !parameter.AllowedValues.Any(value =>
                        string.Equals(
                            value.Value<string>(),
                            parameter.DefaultValue.Value<string>(),
                            StringComparison.Ordinal)))
                {
                    Debug.LogWarning(
                        $"Default value for enum parameter '{parameter.ParameterId}' must be one of its allowed values.");

                    return false;
                }
            }

            return true;
        }

        private bool IsValidValue(JToken token, OperationParameterType type)
        {
            if (token == null || token.Type == JTokenType.Null)
                return false;

            switch (type)
            {
                case OperationParameterType.String:
                case OperationParameterType.AssetReference:
                case OperationParameterType.Enum:
                    return token.Type == JTokenType.String;

                case OperationParameterType.Boolean:
                    return token.Type == JTokenType.Boolean;

                case OperationParameterType.Integer:
                    return token.Type == JTokenType.Integer &&
                           token.Value<long>() >= int.MinValue &&
                           token.Value<long>() <= int.MaxValue;

                case OperationParameterType.Float:
                    return IsNumericToken(token);

                case OperationParameterType.Vector3:
                    return IsNumericArray(token, 3);

                case OperationParameterType.Color:
                    return IsNumericArray(token, 4);

                default:
                    return false;
            }
        }

        private bool IsNumericToken(JToken token)
        {
            return token != null &&
                   (token.Type == JTokenType.Integer || token.Type == JTokenType.Float);
        }

        private bool IsNumericArray(JToken token, int expectedLength)
        {
            if (!(token is JArray array) || array.Count != expectedLength)
                return false;

            return array.All(IsNumericToken);
        }

        private bool AreConstraintsValid(
            JToken minimum,
            JToken maximum,
            OperationParameterType type)
        {
            switch (type)
            {
                case OperationParameterType.Integer:
                    return minimum.Value<long>() <= maximum.Value<long>();

                case OperationParameterType.Float:
                    return minimum.Value<double>() <= maximum.Value<double>();

                case OperationParameterType.Vector3:
                    for (int i = 0; i < 3; i++)
                    {
                        if (minimum[i].Value<double>() > maximum[i].Value<double>())
                            return false;
                    }

                    return true;

                default:
                    return false;
            }
        }

        private bool PopulateParameterProperties(
            SerializedProperty parameterProperty,
            GeneratedOperationParameterMetadata metadata)
        {
            bool success = true;

            success &= SetStringProperty(parameterProperty, "parameterId", metadata.ParameterId);
            success &= SetStringProperty(parameterProperty, "displayName", metadata.DisplayName);
            success &= SetStringProperty(parameterProperty, "description", metadata.Description);
            success &= SetEnumProperty(parameterProperty, "parameterType", metadata.ParameterType);
            success &= SetBoolProperty(parameterProperty, "isRequired", metadata.IsRequired);

            string configurationName = OperationParameterDefinition.GetConfigurationPropertyName(metadata.ParameterType);

            if (configurationName == null)
                return false;

            SerializedProperty configuration =
                parameterProperty.FindPropertyRelative(configurationName);

            if (configuration == null)
            {
                Debug.LogError(
                    $"Could not find configuration '{configurationName}' for parameter '{metadata.ParameterId}'.");

                return false;
            }

            success &= SetBoolProperty(configuration, "HasMinimum", metadata.HasMinimum);
            success &= SetBoolProperty(configuration, "HasMaximum", metadata.HasMaximum);
            success &= SetBoolProperty(configuration, "HasDefaultValue", metadata.HasDefaultValue);

            if (metadata.HasMinimum)
                success &= SetTokenValue(configuration.FindPropertyRelative("Minimum"), metadata.Minimum);

            if (metadata.HasMaximum)
                success &= SetTokenValue(configuration.FindPropertyRelative("Maximum"), metadata.Maximum);

            if (metadata.HasDefaultValue)
                success &= SetTokenValue(configuration.FindPropertyRelative("DefaultValue"), metadata.DefaultValue);

            if (metadata.AllowedValues != null)
                success &= SetTokenArrayProperty(
                    configuration,
                    "AllowedValues",
                    metadata.AllowedValues);

            return success;
        }

        #endregion

        #region Serialized Property Helpers

        private bool SetStringProperty(
            SerializedProperty parent,
            string propertyName,
            string value)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);

            if (property == null || property.propertyType != SerializedPropertyType.String)
                return false;

            property.stringValue = value ?? string.Empty;
            return true;
        }

        private bool SetBoolProperty(
            SerializedProperty parent,
            string propertyName,
            bool value)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);

            if (property == null || property.propertyType != SerializedPropertyType.Boolean)
                return false;

            property.boolValue = value;
            return true;
        }

        private bool SetEnumProperty<TEnum>(
            SerializedProperty parent,
            string propertyName,
            TEnum value)
            where TEnum : struct, Enum
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);

            if (property == null || property.propertyType != SerializedPropertyType.Enum)
                return false;

            int enumIndex = Array.IndexOf(property.enumNames, value.ToString());

            if (enumIndex < 0)
                return false;

            property.enumValueIndex = enumIndex;
            return true;
        }

        #endregion

        #region Validation

        protected override bool ValidateDefinition()
        {
            if (!base.ValidateDefinition())
                return false;

            if (definition.Parameters == null)
                return true;

            var parameterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (OperationParameterDefinition parameter in definition.Parameters)
            {
                if (parameter == null)
                {
                    statusLabel.text = "The operation contains an empty parameter entry.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(parameter.ParameterId))
                {
                    statusLabel.text =
                        $"Parameter '{parameter.DisplayName}' must have an ID.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(parameter.DisplayName))
                {
                    statusLabel.text =
                        $"Parameter '{parameter.ParameterId}' must have a display name.";
                    return false;
                }

                if (!parameterIds.Add(parameter.ParameterId))
                {
                    statusLabel.text = $"Duplicate parameter ID: '{parameter.ParameterId}'.";
                    return false;
                }

                //Validate parameter configuration based on its type
                if (!ValidateParameterConfiguration(parameter))
                    return false;
            }

            return true;
        }

        private bool ValidateParameterConfiguration(OperationParameterDefinition parameter)
        {
            ParameterConfiguration configuration = parameter.GetConfiguration();

            if (configuration == null)
            {
                statusLabel.text = $"Parameter '{parameter.DisplayName}' has no configuration for type '{parameter.ParameterType}'.";

                return false;
            }

            switch (parameter.ParameterType)
            {
                case OperationParameterType.Integer:
                    {
                        IntegerParameterConfiguration config = parameter.IntegerConfiguration;

                        if (config.HasMinimum && config.HasMaximum &&
                            config.Minimum > config.Maximum)
                        {
                            statusLabel.text = $"Parameter '{parameter.DisplayName}' has an invalid minimum and maximum.";

                            return false;
                        }

                        if (config.HasDefaultValue &&
                            config.HasMinimum && config.DefaultValue < config.Minimum)
                        {
                            statusLabel.text = $"Parameter '{parameter.DisplayName}' has a default value below its minimum.";

                            return false;
                        }

                        if (config.HasDefaultValue &&
                            config.HasMaximum && config.DefaultValue > config.Maximum)
                        {
                            statusLabel.text = $"Parameter '{parameter.DisplayName}' has a default value above its maximum.";

                            return false;
                        }

                        break;
                    }

                case OperationParameterType.Float:
                    {
                        FloatParameterConfiguration config = parameter.FloatConfiguration;

                        if (config.HasMinimum && config.HasMaximum &&
                            config.Minimum > config.Maximum)
                        {
                            statusLabel.text = $"Parameter '{parameter.DisplayName}' has an invalid minimum and maximum.";

                            return false;
                        }

                        if (config.HasDefaultValue &&
                            config.HasMinimum && config.DefaultValue < config.Minimum)
                        {
                            statusLabel.text = $"Parameter '{parameter.DisplayName}' has a default value below its minimum.";

                            return false;
                        }

                        if (config.HasDefaultValue && config.HasMaximum && config.DefaultValue > config.Maximum)
                        {
                            statusLabel.text = $"Parameter '{parameter.DisplayName}' has a default value above its maximum.";

                            return false;
                        }

                        break;
                    }

                case OperationParameterType.Vector3:
                    {
                        Vector3ParameterConfiguration config = parameter.Vector3Configuration;

                        if (config.HasMinimum && config.HasMaximum &&
                            (config.Minimum.x > config.Maximum.x ||
                             config.Minimum.y > config.Maximum.y ||
                             config.Minimum.z > config.Maximum.z))
                        {
                            statusLabel.text =
                                $"Parameter '{parameter.DisplayName}' has invalid Vector3 constraints.";

                            return false;
                        }

                        if (config.HasDefaultValue)
                        {
                            Vector3 value = config.DefaultValue;

                            if ((config.HasMinimum &&
                                 (value.x < config.Minimum.x ||
                                  value.y < config.Minimum.y ||
                                  value.z < config.Minimum.z)) ||
                                (config.HasMaximum &&
                                 (value.x > config.Maximum.x ||
                                  value.y > config.Maximum.y ||
                                  value.z > config.Maximum.z)))
                            {
                                statusLabel.text =
                                    $"Parameter '{parameter.DisplayName}' has a default Vector3 outside its constraints.";

                                return false;
                            }
                        }

                        break;
                    }

                case OperationParameterType.Enum:
                    {
                        EnumParameterConfiguration config = parameter.EnumConfiguration;

                        if (config.AllowedValues == null || config.AllowedValues.Count == 0)
                        {
                            statusLabel.text =
                                $"Enum parameter '{parameter.DisplayName}' must define at least one allowed value.";

                            return false;
                        }

                        if (config.HasDefaultValue &&
                            !config.AllowedValues.Contains(config.DefaultValue))
                        {
                            statusLabel.text =
                                $"Enum parameter '{parameter.DisplayName}' has a default value that is not in its allowed values.";

                            return false;
                        }

                        break;
                    }
            }

            return true;
        }

        #endregion

        #region Registry

        protected override bool HasId(string id)
        {
            return registry?.Operations != null &&
                   registry.Operations.Any(operation =>
                       operation != null &&
                       string.Equals(
                           operation.OperationId,
                           id,
                           StringComparison.OrdinalIgnoreCase));
        }

        protected override bool HasDuplicateId(OperationDefinition candidate)
        {
            return registry?.Operations != null &&
                   registry.Operations.Any(operation =>
                       operation != null &&
                       operation != candidate &&
                       string.Equals(
                           operation.OperationId,
                           candidate.OperationId,
                           StringComparison.OrdinalIgnoreCase));
        }

        protected override bool RegisterDefinition(OperationDefinition item)
        {
            return registry != null &&
                   item != null &&
                   registry.RegisterOperation(item);
        }

        protected override void UnregisterDefinition(OperationDefinition item)
        {
            if (registry != null && item != null)
                registry.UnregisterOperation(item);
        }

        protected override void RebuildRegistryIndex()
        {
            registry?.RebuildIndex();
        }

        protected override void MarkRegistryDirty()
        {
            if (registry != null)
                EditorUtility.SetDirty(registry);
        }

        #endregion
    }
}