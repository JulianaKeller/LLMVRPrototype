using System;
using System.Threading.Tasks;
using LLMVRPrototype.Assets;

namespace LLMVRPrototype
{
    public sealed class LLMMetadataGenerationService
    {
        private readonly ILLMProvider provider;

        public LLMMetadataGenerationService(ILLMProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<MetadataGenerationResult<TagMetadata>> GenerateTagMetadataAsync(string displayName, AssetTagDefinition parentTag = null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return MetadataGenerationResult<TagMetadata>.Failed("A tag display name is required.");
            }

            string systemPrompt = TagMetadataPrompt.BuildSystemPrompt();
            string userPrompt = TagMetadataPrompt.BuildUserPrompt(displayName, parentTag);

            LLMResponse response;

            try
            {
                var request = new LLMRequest(userPrompt, systemPrompt);

                response = await provider.SendAsync(request);
            }
            catch (Exception)
            {
                return MetadataGenerationResult<TagMetadata>.Failed("The metadata request failed unexpectedly.");
            }

            if (response == null)
            {
                return MetadataGenerationResult<TagMetadata>.Failed("The provider returned no response.");
            }

            if (!response.Success)
            {
                return MetadataGenerationResult<TagMetadata>.Failed(
                    string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? "The provider could not generate metadata."
                        : response.ErrorMessage);
            }

            if (!MetadataJsonParser.TryParseTagMetadata(
                response.Text,
                out TagMetadata metadata,
                out string parseError))
            {
                return MetadataGenerationResult<TagMetadata>.Failed("Could not parse generated metadata: " + parseError);
            }

            if (!MetadataValidators.TryValidateTagMetadata(
                metadata,
                displayName,
                out string validationError))
            {
                return MetadataGenerationResult<TagMetadata>.Failed("Generated metadata failed validation: " + validationError);
            }

            return MetadataGenerationResult<TagMetadata>.Successful(metadata);
        }
    }
}