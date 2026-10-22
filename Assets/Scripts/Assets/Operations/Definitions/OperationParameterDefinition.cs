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

    [CreateAssetMenu(fileName = "OperationParameter", menuName = "LLM VR/Operations/Parameter Definition")]
    public class OperationParameterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string parameterId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Type")]
        [SerializeField] private OperationParameterType parameterType;
        [SerializeField] private bool isRequired = true;

        [Header("Numeric Constraints")]
        [SerializeField] private bool hasMinimum;
        [SerializeField] private float minimum;
        [SerializeField] private bool hasMaximum;
        [SerializeField] private float maximum;

        [Header("Allowed Values")]
        [SerializeField]
        private string[] allowedValues =
            System.Array.Empty<string>();

        [Header("Default Value")]
        [SerializeField] private bool hasDefaultValue;
        [SerializeField] private string defaultValue;

        public string ParameterId => parameterId;
        public string DisplayName => displayName;
        public string Description => description;
        public OperationParameterType ParameterType => parameterType;
        public bool IsRequired => isRequired;

        public bool HasMinimum => hasMinimum;
        public float Minimum => minimum;
        public bool HasMaximum => hasMaximum;
        public float Maximum => maximum;

        public string[] AllowedValues => allowedValues;

        public bool HasDefaultValue => hasDefaultValue;
        public string DefaultValue => defaultValue;
    }
}
