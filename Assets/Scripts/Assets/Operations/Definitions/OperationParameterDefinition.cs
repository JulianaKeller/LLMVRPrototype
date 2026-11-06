using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets
{
    public enum OperationParameterType
    {
        String,
        Boolean,
        Integer,
        Float,
        Vector3,
        Color,
        AssetReference,
        Enum
    }

    [Serializable]
    public class OperationParameterDefinition
    {
        [Header("Identity")]
        [SerializeField] private string parameterId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Type")]
        [SerializeField] private OperationParameterType parameterType;
        [SerializeField] private bool isRequired = true;

        [Header("Configuration")]
        [SerializeField] private FloatParameterConfiguration floatConfiguration = new FloatParameterConfiguration();
        [SerializeField] private Vector3ParameterConfiguration vector3Configuration = new Vector3ParameterConfiguration();
        [SerializeField] private ColorParameterConfiguration colorConfiguration = new ColorParameterConfiguration();
        [SerializeField] private StringParameterConfiguration stringConfiguration = new StringParameterConfiguration();
        [SerializeField] private BooleanParameterConfiguration booleanConfiguration = new BooleanParameterConfiguration();
        [SerializeField] private EnumParameterConfiguration enumConfiguration = new EnumParameterConfiguration();
        [SerializeField] private AssetReferenceParameterConfiguration assetReferenceConfiguration = new AssetReferenceParameterConfiguration();
        [SerializeField] private IntegerParameterConfiguration integerParameterConfiguration = new IntegerParameterConfiguration();

        public static List<string> ListConfigurationNames()
        {
            return new List<string>
            {
                nameof(FloatParameterConfiguration),
                nameof(Vector3ParameterConfiguration),
                nameof(ColorParameterConfiguration),
                nameof(StringParameterConfiguration),
                nameof(BooleanParameterConfiguration),
                nameof(EnumParameterConfiguration),
                nameof(AssetReferenceParameterConfiguration),
                nameof(IntegerParameterConfiguration)
            };
        }

        public static string GetConfigurationPropertyName(OperationParameterType type)
        {
            switch (type)
            {
                case OperationParameterType.String:
                    return "stringConfiguration";

                case OperationParameterType.Boolean:
                    return "booleanConfiguration";

                case OperationParameterType.Integer:
                    return "integerParameterConfiguration";

                case OperationParameterType.Float:
                    return "floatConfiguration";

                case OperationParameterType.Vector3:
                    return "vector3Configuration";

                case OperationParameterType.Color:
                    return "colorConfiguration";

                case OperationParameterType.AssetReference:
                    return "assetReferenceConfiguration";

                case OperationParameterType.Enum:
                    return "enumConfiguration";

                default:
                    return null;
            }
        }

        /*
        [Header("Numeric Constraints")]
        [SerializeField] private bool hasMinimum;
        [SerializeField] private float minimum;
        [SerializeField] private Vector3 vector3Minimum;
        [SerializeField] private bool hasMaximum;
        [SerializeField] private float maximum;
        [SerializeField] private Vector3 vector3Maximum;

        [Header("Allowed Values")]
        [SerializeField]
        private string[] allowedValues = System.Array.Empty<string>();

        [Header("Default Value")]
        [SerializeField] private bool hasDefaultValue;
        [SerializeField] private string defaultValue;
        */

        public string ParameterId => parameterId;
        public string DisplayName => displayName;
        public string Description => description;
        public OperationParameterType ParameterType => parameterType;
        public bool IsRequired => isRequired;


        public FloatParameterConfiguration FloatConfiguration => floatConfiguration;
        public Vector3ParameterConfiguration Vector3Configuration => vector3Configuration;
        public ColorParameterConfiguration ColorConfiguration => colorConfiguration;
        public StringParameterConfiguration StringConfiguration => stringConfiguration;
        public BooleanParameterConfiguration BooleanConfiguration => booleanConfiguration;
        public EnumParameterConfiguration EnumConfiguration => enumConfiguration;
        public AssetReferenceParameterConfiguration AssetReferenceConfiguration => assetReferenceConfiguration;
        public IntegerParameterConfiguration IntegerConfiguration => integerParameterConfiguration;


        public ParameterConfiguration GetConfiguration()
        {
            switch (parameterType)
            {
                case OperationParameterType.Integer: 
                    return integerParameterConfiguration;
                case OperationParameterType.Float:
                    return floatConfiguration;
                case OperationParameterType.Vector3:
                    return vector3Configuration;
                case OperationParameterType.Color:
                    return colorConfiguration;
                case OperationParameterType.String:
                    return stringConfiguration;
                case OperationParameterType.Boolean:
                    return booleanConfiguration;
                case OperationParameterType.Enum:
                    return enumConfiguration;
                case OperationParameterType.AssetReference:
                    return assetReferenceConfiguration;
                default:
                    return null;
            }
        }

        public override string ToString()
        {
            ParameterConfiguration configuration = GetConfiguration();

            string result = $"ParameterId: {parameterId}, DisplayName: {displayName}, " +
                            $"Description: {description}, ParameterType: {parameterType}, " +
                            $"IsRequired: {isRequired}";

            if (configuration == null)
            {
                return result;
            }

            string configurationDetails = "";

            switch (parameterType)
            {
                case OperationParameterType.String:
                    configurationDetails = $"HasDefaultValue: {StringConfiguration.HasDefaultValue}, " +
                    $"DefaultValue: {StringConfiguration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", StringConfiguration.AllowedValues ?? new List<string>())}]";
                    break;

                case OperationParameterType.Boolean:
                    configurationDetails = $"HasDefaultValue: {BooleanConfiguration.HasDefaultValue}, " +
                    $"DefaultValue: {BooleanConfiguration.DefaultValue}";
                    break;

                case OperationParameterType.Integer:
                    configurationDetails = $"HasMinimum: {IntegerConfiguration.HasMinimum}, Minimum: {IntegerConfiguration.Minimum}, " +
                    $"HasMaximum: {IntegerConfiguration.HasMaximum}, Maximum: {IntegerConfiguration.Maximum}, " +
                    $"HasDefaultValue: {IntegerConfiguration.HasDefaultValue}, DefaultValue: {IntegerConfiguration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", IntegerConfiguration.AllowedValues ?? new List<int>())}]";
                    break;

                case OperationParameterType.Float:
                    configurationDetails = $"HasMinimum: {FloatConfiguration.HasMinimum}, Minimum: {FloatConfiguration.Minimum}, " +
                    $"HasMaximum: {FloatConfiguration.HasMaximum}, Maximum: {FloatConfiguration.Maximum}, " +
                    $"HasDefaultValue: {FloatConfiguration.HasDefaultValue}, DefaultValue: {FloatConfiguration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", FloatConfiguration.AllowedValues ?? new List<float>())}]";
                    break;

                case OperationParameterType.Vector3:
                    configurationDetails = $"HasMinimum: {Vector3Configuration.HasMinimum}, Minimum: {Vector3Configuration.Minimum}, " +
                    $"HasMaximum: {Vector3Configuration.HasMaximum}, Maximum: {Vector3Configuration.Maximum}, " +
                    $"HasDefaultValue: {Vector3Configuration.HasDefaultValue}, DefaultValue: {Vector3Configuration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", Vector3Configuration.AllowedValues ?? new List<Vector3>())}]";
                    break;

                case OperationParameterType.Color:
                    configurationDetails = $"HasDefaultValue: {ColorConfiguration.HasDefaultValue}, " +
                    $"DefaultValue: {ColorConfiguration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", ColorConfiguration.AllowedValues ?? new List<Color>())}]";
                    break;

                case OperationParameterType.AssetReference:
                    configurationDetails = $"HasDefaultValue: {AssetReferenceConfiguration.HasDefaultValue}, " +
                    $"DefaultValue: {AssetReferenceConfiguration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", AssetReferenceConfiguration.AllowedValues ?? new List<string>())}]";
                    break;

                case OperationParameterType.Enum:
                    configurationDetails = $"HasDefaultValue: {EnumConfiguration.HasDefaultValue}, " +
                    $"DefaultValue: {EnumConfiguration.DefaultValue}, " +
                    $"AllowedValues: [{string.Join(", ", EnumConfiguration.AllowedValues ?? new List<string>())}]";
                    break;

                default:
                    break;
            }
            ;

            return result + $", {configurationDetails}";
        }
    }

    [Serializable]
    public class ParameterConfiguration
    {
        public bool HasDefaultValue = false;
    }

    [Serializable]
    public class StringParameterConfiguration : ParameterConfiguration
    {
        public string DefaultValue;
        public List<string> AllowedValues;
    }

    [Serializable]
    public class BooleanParameterConfiguration : ParameterConfiguration
    {
        public bool DefaultValue;
    }

    [Serializable]
    public class IntegerParameterConfiguration : ParameterConfiguration
    {
        public bool HasMinimum = false;
        public bool HasMaximum = false;
        public int Minimum;
        public int Maximum;
        public int DefaultValue;
        public List<int> AllowedValues;
    }

    [Serializable]
    public class AssetReferenceParameterConfiguration : ParameterConfiguration
    {
        public string DefaultValue;
        public List<string> AllowedValues;
    }

    [Serializable]
    public class EnumParameterConfiguration : ParameterConfiguration
    {
        public string DefaultValue;
        public List<string> AllowedValues;
    }

    [Serializable]
    public class FloatParameterConfiguration : ParameterConfiguration
    {
        public bool HasMinimum = false;
        public bool HasMaximum = false;
        public float Minimum;
        public float Maximum;
        public float DefaultValue;
        public List<float> AllowedValues;
    }

    [Serializable]
    public class Vector3ParameterConfiguration : ParameterConfiguration
    {
        public bool HasMinimum = false;
        public bool HasMaximum = false;
        public Vector3 Minimum;
        public Vector3 Maximum;
        public Vector3 DefaultValue;
        public List<Vector3> AllowedValues;
    }

    [Serializable]
    public class ColorParameterConfiguration : ParameterConfiguration
    {
        public Color DefaultValue;
        public List<Color> AllowedValues;
    }
}
