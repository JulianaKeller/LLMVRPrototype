using LLMVRPrototype.Assets;
using System.Text;
using UnityEngine;

namespace LLMVRPrototype
{
    public class CategoryMetadataPrompt : MetadataPrompt
    {
        /*public override string BuildUserPrompt(MetadataGenerationInput input)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("Generate metadata for a single asset-library category.");
            prompt.AppendLine();

            prompt.AppendLine("Category name:");
            prompt.AppendLine("<category_name>");
            prompt.AppendLine(displayName.Trim());
            prompt.AppendLine("</category_name>");
            prompt.AppendLine();

            if (parentCategory != null)
            {
                prompt.AppendLine("Parent category context:");
                prompt.AppendLine("<parent_name>");
                prompt.AppendLine(parentCategory.DisplayName ?? string.Empty);
                prompt.AppendLine("</parent_name>");

                prompt.AppendLine("<parent_description>");
                prompt.AppendLine(parentCategory.Description ?? string.Empty);
                prompt.AppendLine("</parent_description>");
            }
            else
            {
                prompt.AppendLine("Parent category context: None. This is a root category.");
            }

            prompt.AppendLine();
            prompt.AppendLine("Requirements:");
            prompt.AppendLine("- Write a concise, informative description in one or two sentences.");
            prompt.AppendLine("- Generate " + minSynonyms + " to " + maxSynonyms + " useful alternative names or synonymous terms.");
            prompt.AppendLine("- Terms must be relevant to the category, not merely broad associations.");
            prompt.AppendLine("- Do not repeat the category name as a synonym.");
            prompt.AppendLine("- Do not invent facts unsupported by the provided context.");
            prompt.AppendLine("- Do not include explanations, confidence scores, or extra fields.");
            prompt.AppendLine();

            prompt.AppendLine("Required JSON schema:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"description\": \"A concise description of the category.\",");
            prompt.AppendLine("  \"synonyms\": [\"alternative term one\", \"alternative term two\"]");
            prompt.AppendLine("}");

            return prompt.ToString();
        }*/
    }
}
