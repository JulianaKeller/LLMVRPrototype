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

        private async Task<MetadataGenerationResult<string>> GenerateResponseTextAsync(MetadataGenerationInput input, MetadataPrompt prompt)
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

        #region Input specific methods

        private async Task<MetadataGenerationResult<TMetadata>> GenerateMetadataAsync<TMetadata>(MetadataGenerationInput input) where TMetadata : GeneratedMetadata
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Name))
            {
                return MetadataGenerationResult<TMetadata>.Failed("A name is required.");
            }

            MetadataPrompt prompt = new MetadataPrompt();

            MetadataGenerationResult<string> responseResult = await GenerateResponseTextAsync(input, prompt);

            if (!responseResult.Success)
            {
                return MetadataGenerationResult<TMetadata>.Failed(responseResult.ErrorMessage);
            }

            if (!MetadataJsonParser.TryParseGeneratedMetadata(
                responseResult.Data,
                out TMetadata metadata,
                out string parseError))
            {
                return MetadataGenerationResult<TMetadata>.Failed("Could not parse generated metadata: " + parseError);
            }

            if (!MetadataValidators.TryValidateGeneratedMetadata(
                metadata,
                out string validationError))
            {
                return MetadataGenerationResult<TMetadata>.Failed("Generated metadata failed validation: " + validationError);
            }

            return MetadataGenerationResult<TMetadata>.Successful(metadata);
        }

        public Task<MetadataGenerationResult<GeneratedTagMetadata>> GenerateMetadataAsync(TagMetadataGenerationInput input)
        {
            return GenerateMetadataAsync<GeneratedTagMetadata>(input);
        }

        public Task<MetadataGenerationResult<GeneratedCategoryMetadata>> GenerateMetadataAsync(CategoryMetadataGenerationInput input)
        {
            return GenerateMetadataAsync<GeneratedCategoryMetadata>(input);
        }

        public Task<MetadataGenerationResult<GeneratedOperationMetadata>> GenerateMetadataAsync(OperationMetadataGenerationInput input)
        {
            return GenerateMetadataAsync<GeneratedOperationMetadata>(input);
        }
        #endregion

        #region Runtime type dispatch methods

        public Task<MetadataGenerationResult<GeneratedMetadata>> GenerateMetadataAsync(MetadataGenerationInput input)
        {
            return input switch
            {
                TagMetadataGenerationInput tagInput => ConvertResult(GenerateMetadataAsync(tagInput)),
                CategoryMetadataGenerationInput categoryInput => ConvertResult(GenerateMetadataAsync(categoryInput)),
                OperationMetadataGenerationInput operationInput => ConvertResult(GenerateMetadataAsync(operationInput)),
                _ => Task.FromResult(MetadataGenerationResult<GeneratedMetadata>.Failed(
                    $"Unsupported metadata input type: {input?.GetType().Name ?? "null"}"))
            };
        }

        private async Task<MetadataGenerationResult<GeneratedMetadata>> ConvertResult<TMetadata>(
            Task<MetadataGenerationResult<TMetadata>> task) where TMetadata : GeneratedMetadata
        {
            MetadataGenerationResult<TMetadata> result = await task;

            if (!result.Success)
                return MetadataGenerationResult<GeneratedMetadata>.Failed(result.ErrorMessage);

            return MetadataGenerationResult<GeneratedMetadata>.Successful(result.Data);
        }

        #endregion
    }
}