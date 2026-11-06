using LLMVRPrototype.Assets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LLMVRPrototype
{
    [Serializable]
    public struct ContextData
    {
        public string contextName;
        public string contextValue;
        public string requirements; // e.g. 3-8 synonyms
        public string requiredJSONFormat;

        public ContextData(string contextName, string contextValue = "", string requirements = "", string requiredJSONFormat = "")
        {
            this.contextName = contextName;
            this.contextValue = contextValue;
            this.requirements = requirements;
            this.requiredJSONFormat = requiredJSONFormat;
        }
    }

    [Serializable]
    public abstract class MetadataGenerationInput
    {
        public string EntityType;
        public string Name;
        public string generalRequirements = "";

        public List<ContextData> requiredContextData = new List<ContextData>();
        public List<ContextData> providedContextData = new List<ContextData>();

        protected void AddSynonymRequiredContext(int minSynonyms, int maxSynonyms)
        {
            var synonymRequirements = new StringBuilder();
            synonymRequirements.AppendLine("- Generate " + minSynonyms + " to " + maxSynonyms + " useful alternative names or synonymous terms.");
            synonymRequirements.AppendLine("- Terms must be relevant to the tag, not merely broad associations.");
            synonymRequirements.AppendLine("- Do not repeat the tag name as a synonym.");

            string synonymJSONFormat = $"  \"{MetadataJsonFields.Synonyms}\": [\"alternative term one\", \"alternative term two\"]";
            ContextData synonymContext = new ContextData("Synonyms: ", "", synonymRequirements.ToString(), synonymJSONFormat);

            requiredContextData.Add(synonymContext);
        }

        protected void AddDescriptionRequiredContext(string requirements = "", bool descriptionGiven = false)
        {
            var descriptionRequirements = new StringBuilder();
            if (descriptionGiven)
            {
                descriptionRequirements.AppendLine("Improve and enrich the given description. ");
                descriptionRequirements.AppendLine("Preserve all relevant existing information and factual details. ");
                descriptionRequirements.AppendLine("Do not remove useful information simply to make the description shorter.");
                descriptionRequirements.AppendLine("Add relevant context, clarify ambiguous wording, and improve readability.");
                descriptionRequirements.AppendLine("Return the complete improved description, not just the additions.");
            }
            else
            {
                descriptionRequirements.AppendLine("- Write a concise, informative description in one or two sentences.");
            }
                
            descriptionRequirements.AppendLine("- " + requirements);

            string descriptionJSONFormat = $"  \"{MetadataJsonFields.Description}\": \"A concise description.\",";

            ContextData descriptionRequiredContext = new ContextData(EntityType + " Description: ", "", descriptionRequirements.ToString(),  descriptionJSONFormat);
            requiredContextData.Add(descriptionRequiredContext);
        }

        protected void AddDescriptionProvidedContext(string description)
        {
            ContextData descriptionContext = new ContextData(EntityType + " Description: ", description);
            providedContextData.Add(descriptionContext);
        }

        protected void AddParentProvidedContext(string parentName, string parentDescription)
        {
            ContextData parentNameContext = new ContextData("Parent-" + EntityType + " Name: ", parentName);
            providedContextData.Add(parentNameContext);

            ContextData parentDescriptionContext = new ContextData("Parent-" + EntityType + " Description: ", parentDescription);
            providedContextData.Add(parentDescriptionContext);
        }
    }

    [Serializable]
    public sealed class TagMetadataGenerationInput : MetadataGenerationInput
    {
        // Call this to fill the fields with the correct values, e.g. building the requiredContextData and providedContextData lists
        public TagMetadataGenerationInput(string tagName, string tagDescription = "", string parentName = "", string parentDescription = "", int minSynonyms = 3, int maxSynonyms = 8)
        {
            EntityType = "Tag";
            Name = tagName;

            AddDescriptionProvidedContext(tagDescription);
            AddParentProvidedContext(parentName, parentDescription);

            AddDescriptionRequiredContext(descriptionGiven: tagDescription != "");
            AddSynonymRequiredContext(minSynonyms, maxSynonyms);
        }
    }

    [Serializable]
    public sealed class CategoryMetadataGenerationInput : MetadataGenerationInput
    {
        // Call this to fill the fields with the correct values, e.g. building the requiredContextData and providedContextData lists
        public CategoryMetadataGenerationInput(string categoryName, string categoryDescription = "", string parentName = "", string parentDescription = "", int minSynonyms = 3, int maxSynonyms = 8)
        {
            EntityType = "Category";
            Name = categoryName;

            AddDescriptionProvidedContext(categoryDescription);
            AddParentProvidedContext(parentName, parentDescription);

            AddDescriptionRequiredContext(descriptionGiven: categoryDescription != "");
            AddSynonymRequiredContext(minSynonyms, maxSynonyms);
        }
    }

    [Serializable]
    public sealed class OperationMetadataGenerationInput : MetadataGenerationInput
    {
        public OperationMetadataGenerationInput(string operationName, string description = "", string targetType = "", bool requiresTarget = false,
            IEnumerable<string> parameterNames = null)
        {
            EntityType = "Operation";
            Name = operationName;

            AddDescriptionProvidedContext(description);

            providedContextData.Add(
                new ContextData("Target Type: ", targetType));

            providedContextData.Add(
                new ContextData(
                    "Requires Target: ",
                    requiresTarget.ToString()));

            providedContextData.Add(
                new ContextData(
                    "Existing Parameters: ",
                    parameterNames != null
                        ? string.Join(", ", parameterNames)
                        : "None"));

            AddDescriptionRequiredContext("Generate a concise description of what the operation does. Describe the operation's purpose, not implementation details.", description != "");

            string targetTypeValues = "\"" + string.Join(", ", Enum.GetNames(typeof(OperationTargetType))) + "\"";

            requiredContextData.Add(
               new ContextData(
                   "Target Type: ",
                   "",
                   "Choose one single appropriate target type from the available enum values: " +
                   "[" + targetTypeValues + "]",
                   "\"TargetType\": \"EnumValueName\""));

            requiredContextData.Add(
                new ContextData(
                    "Requires Target: ",
                    "",
                    "Set true if the operation requires a target object. " +
                    "Set false if it operates independently of a target.",
                    "\"RequiresTarget\": true"));

            // Add has minimum/maximum and minimum/maximum requried context

            string parameterTypeValues = "\"" + string.Join("\", \"", Enum.GetNames(typeof(OperationParameterType))) + "\"";

            requiredContextData.Add(
                new ContextData(
                    "Parameters: ",
                    "",
                    "Generate only the parameters required to execute this operation. " +
                    "Each parameter must have a unique ParameterId, a clear DisplayName, " +
                    "a concise Description, and one single valid ParameterType selected from the provided list of parameter types. " +
                    "Only define parameters necessary for the operation. " +
                    "Use HasMinimum and Minimum, and HasMaximum and Maximum, only for Integer, Float, and Vector3 parameters. " +
                    "For Vector3 values, use arrays in [x, y, z] format. " +
                    "For Color values, use arrays in [r, g, b, a] format. " +
                    "Use AllowedValues only when the parameter has a meaningful restricted set of values. " +
                    "AllowedValues must contain values matching the parameter type. " +
                    "For Enum parameters, AllowedValues must contain the permitted enum names. " +
                    "Use HasDefaultValue and DefaultValue only when a sensible default can be established. " +
                    "Do not invent asset references or enum values. For AssetReference parameters, only provide known asset identifiers.",
                    "\"Parameters\": [{ " +
                    "\"ParameterId\": \"e.g. intensity\", " +
                    "\"DisplayName\": \"e.g. Intensity\", " +
                    "\"Description\": \"e.g. The intensity of the light.\", " +
                    "\"ParameterType\": [" + parameterTypeValues + "], " +
                    "\"IsRequired\": true, " + 
                    "\"HasMinimum\": true, " +
                    "\"Minimum\": 0.0, " +
                    "\"HasMaximum\": true, " +
                    "\"Maximum\": 10.0, " +
                    "\"AllowedValues\": [], " +
                    "\"HasDefaultValue\": true, " +
                    "\"DefaultValue\": \"1.0\" " +
                    "}]"));
        }
    }
}
