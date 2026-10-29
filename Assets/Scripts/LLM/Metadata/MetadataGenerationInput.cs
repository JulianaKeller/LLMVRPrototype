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

        protected void AddDescriptionRequiredContext()
        {
            var descriptionRequirements = new StringBuilder();
            descriptionRequirements.AppendLine("- Write a concise, informative description in one or two sentences.");

            string descriptionJSONFormat = $"  \"{MetadataJsonFields.Description}\": \"A concise description of the tag.\",";

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

            AddDescriptionRequiredContext();
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

            AddDescriptionRequiredContext();
            AddSynonymRequiredContext(minSynonyms, maxSynonyms);
        }
    }

    [Serializable]
    public sealed class OperationMetadataGenerationInput : MetadataGenerationInput
    {
        public OperationMetadataGenerationInput(string operationName, string description = "")
        {

        }
    }
}
