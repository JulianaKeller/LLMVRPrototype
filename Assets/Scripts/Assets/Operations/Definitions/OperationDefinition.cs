using System.Collections.Generic;
using UnityEngine;

namespace LLMVRPrototype.Assets
{
    public enum OperationTargetType
    {
        None,
        SceneObject,
        Light,
        AudioSource,
        Material,
        Environment,
        Camera
    }

    public enum OperationPromptPolicy
    {
        AlwaysInclude,
        ContextDependent,
        TargetDependent,
        NeverInclude
    }

    [CreateAssetMenu(fileName = "Operation", menuName = "LLM VR/Operations/Operation Definition")]
    public class OperationDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string operationId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Target")]
        [SerializeField] private OperationTargetType targetType;
        [SerializeField] private bool requiresTarget;

        [Header("Parameters")]
        [SerializeField]
        private List<OperationParameterDefinition> parameters = new();

        [Header("Availability")]
        [SerializeField] private bool isEnabled = true;
        [SerializeField]
        private OperationPromptPolicy promptPolicy =
            OperationPromptPolicy.ContextDependent;

        [Header("Execution")]
        [Tooltip(
            "Identifier used to find the C# executor. This is not a class name supplied by the LLM.")]
        [SerializeField] private string executorId;

        public string OperationId => operationId;
        public string DisplayName => displayName;
        public string Description => description;

        public OperationTargetType TargetType => targetType;
        public bool RequiresTarget => requiresTarget;

        public IReadOnlyList<OperationParameterDefinition> Parameters =>
            parameters;

        public bool IsEnabled => isEnabled;
        public OperationPromptPolicy PromptPolicy => promptPolicy;

        public string ExecutorId => executorId;
    }
}
