using System.Text;
using LLMVRPrototype.Assets;

namespace LLMVRPrototype
{
    public class TagMetadataPrompt : MetadataPrompt
    {

        /*public override string BuildUserPrompt(MetadataGenerationInput input)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("Generate metadata for a single asset-library tag.");
            prompt.AppendLine();

            prompt.AppendLine("Tag name:");
            prompt.AppendLine("<tag_name>");
            prompt.AppendLine(displayName.Trim());
            prompt.AppendLine("</tag_name>");
            prompt.AppendLine();

            if (parentTag != null)
            {
                prompt.AppendLine("Parent tag context:");
                prompt.AppendLine("<parent_name>");
                prompt.AppendLine(parentTag.DisplayName ?? string.Empty);
                prompt.AppendLine("</parent_name>");

                prompt.AppendLine("<parent_description>");
                prompt.AppendLine(parentTag.Description ?? string.Empty);
                prompt.AppendLine("</parent_description>");
            }
            else
            {
                prompt.AppendLine("Parent tag context: None. This is a root tag.");
            }

            prompt.AppendLine();
            prompt.AppendLine("Requirements:");
            prompt.AppendLine("- Write a concise, informative description in one or two sentences.");
            prompt.AppendLine("- Generate " + minSynonyms + " to " + maxSynonyms + " useful alternative names or synonymous terms.");
            prompt.AppendLine("- Terms must be relevant to the tag, not merely broad associations.");
            prompt.AppendLine("- Do not repeat the tag name as a synonym.");
            prompt.AppendLine("- Do not invent facts unsupported by the provided context.");
            prompt.AppendLine("- Do not include explanations, confidence scores, or extra fields.");
            prompt.AppendLine();

            prompt.AppendLine("Required JSON schema:");
            prompt.AppendLine("{");
            prompt.AppendLine("  \"description\": \"A concise description of the tag.\",");
            prompt.AppendLine("  \"synonyms\": [\"alternative term one\", \"alternative term two\"]");
            prompt.AppendLine("}");

            return prompt.ToString();
        }*/
    }
}