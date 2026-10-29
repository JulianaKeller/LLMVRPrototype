using System;
using System.Threading.Tasks;

namespace LLMVRPrototype
{
    public sealed class LLMMetadataGenerationService
    {
        private readonly ILLMProvider provider;

        public LLMMetadataGenerationService(ILLMProvider provider)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<MetadataGenerationResult<string>> GenerateResponseTextAsync(MetadataGenerationInput input, MetadataPrompt prompt)
        {

            string systemPrompt = prompt.BuildSystemPrompt();
            string userPrompt = prompt.BuildUserPrompt(input);

            LLMResponse response;

            try
            {
                response = await provider.SendAsync(new LLMRequest(userPrompt, systemPrompt));
            }
            catch (Exception)
            {
                return MetadataGenerationResult<string>.Failed("The metadata request failed unexpectedly.");
            }

            if (response == null)
            {
                return MetadataGenerationResult<string>.Failed("The provider returned no response.");
            }

            if (!response.Success)
            {
                return MetadataGenerationResult<string>.Failed(
                    string.IsNullOrWhiteSpace(response.ErrorMessage)
                        ? "The provider could not generate metadata."
                        : response.ErrorMessage);
            }
            return MetadataGenerationResult<string>.Successful(response.Text);
        }

        public async Task<MetadataGenerationResult<GeneratedMetadata>> GenerateMetadataAsync(MetadataGenerationInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                return MetadataGenerationResult<GeneratedMetadata>.Failed("A name is required.");
            }

            MetadataPrompt prompt = new MetadataPrompt();

            MetadataGenerationResult<string> responseResult = await GenerateResponseTextAsync(input, prompt);

            if (!responseResult.Success)
            {
                return MetadataGenerationResult<GeneratedMetadata>.Failed(responseResult.ErrorMessage);
            }

            if (!MetadataJsonParser.TryParseGeneratedMetadata(
                responseResult.Data,
                out GeneratedTagMetadata metadata,
                out string parseError))
            {
                return MetadataGenerationResult<GeneratedMetadata>.Failed("Could not parse generated metadata: " + parseError);
            }

            if (!MetadataValidators.TryValidateGeneratedMetadata(
                metadata,
                out string validationError))
            {
                return MetadataGenerationResult<GeneratedMetadata>.Failed("Generated metadata failed validation: " + validationError);
            }

            return MetadataGenerationResult<GeneratedMetadata>.Successful(metadata);
        }

        #region Input specific methods

        public async Task<MetadataGenerationResult<GeneratedTagMetadata>> GenerateTagMetadataAsync(TagMetadataGenerationInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                return MetadataGenerationResult<GeneratedTagMetadata>.Failed("A name is required.");
            }

            MetadataPrompt prompt = new MetadataPrompt();

            MetadataGenerationResult<string> responseResult = await GenerateResponseTextAsync(input, prompt);

            if (!responseResult.Success)
            {
                return MetadataGenerationResult<GeneratedTagMetadata>.Failed(responseResult.ErrorMessage);
            }

            if (!MetadataJsonParser.TryParseGeneratedMetadata(
                responseResult.Data,
                out GeneratedTagMetadata metadata,
                out string parseError))
            {
                return MetadataGenerationResult<GeneratedTagMetadata>.Failed("Could not parse generated metadata: " + parseError);
            }

            if (!MetadataValidators.TryValidateGeneratedMetadata(
                metadata,
                out string validationError))
            {
                return MetadataGenerationResult<GeneratedTagMetadata>.Failed("Generated metadata failed validation: " + validationError);
            }

            return MetadataGenerationResult<GeneratedTagMetadata>.Successful(metadata);
        }

        public async Task<MetadataGenerationResult<GeneratedCategoryMetadata>>
            GenerateCategoryMetadataAsync(CategoryMetadataGenerationInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                return MetadataGenerationResult<GeneratedCategoryMetadata>.Failed("A name is required.");
            }

            MetadataPrompt prompt = new MetadataPrompt();

            MetadataGenerationResult<string> responseResult = await GenerateResponseTextAsync(input, prompt);

            if (!responseResult.Success)
            {
                return MetadataGenerationResult<GeneratedCategoryMetadata>.Failed(responseResult.ErrorMessage);
            }

            if (!MetadataJsonParser.TryParseGeneratedMetadata(
                responseResult.Data,
                out GeneratedCategoryMetadata metadata,
                out string parseError))
            {
                return MetadataGenerationResult<GeneratedCategoryMetadata>.Failed("Could not parse generated metadata: " + parseError);
            }

            if (!MetadataValidators.TryValidateGeneratedMetadata(
                metadata,
                out string validationError))
            {
                return MetadataGenerationResult<GeneratedCategoryMetadata>.Failed("Generated metadata failed validation: " + validationError);
            }

            return MetadataGenerationResult<GeneratedCategoryMetadata>.Successful(metadata);
        }
        #endregion
    }
}