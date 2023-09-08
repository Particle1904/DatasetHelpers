using SmartData.Lib.Interfaces;

using System.Text;

namespace SmartData.Lib.Services
{
    public class PromptGeneratorService : IPromptGeneratorService
    {
        protected readonly ITagProcessorService _tagProcessorService;
        protected readonly IFileManipulatorService _fileManipulatorService;

        StringBuilder _stringBuilder;
        Random _random;

        public PromptGeneratorService(ITagProcessorService tagProcessorService, IFileManipulatorService fileManipulatorService)
        {
            _tagProcessorService = tagProcessorService;
            _fileManipulatorService = fileManipulatorService;
            _stringBuilder = new StringBuilder();
            _random = new Random();
        }

        /// <summary>
        /// Generates a prompt based on provided tags, with optional prefix and suffix tags.
        /// </summary>
        /// <param name="tags">An array of tags from which the prompt is generated.</param>
        /// <param name="prependTags">Optional tags to prepend to the generated prompt (can be empty).</param>
        /// <param name="appendTags">Optional tags to append to the generated prompt (can be empty).</param>
        /// <param name="amountOfTags">The number of tags to include in the generated prompt.</param>
        /// <returns>
        /// A string representing the generated prompt, constructed from the provided tags
        /// with optional prefix and suffix tags.
        /// </returns>
        public string GeneratePromptFromDataset(string[] tags, string prependTags, string appendTags, int amountOfTags)
        {
            _stringBuilder.Clear();

            if (!string.IsNullOrEmpty(prependTags))
            {
                _stringBuilder.Append(prependTags);
                _stringBuilder.Append(", ");
            }

            for (int i = 0; i < amountOfTags; i++)
            {
                string tag = tags[_random.Next(tags.Length)];
                _stringBuilder.Append(tag);
                if (i != amountOfTags - 1)
                {
                    _stringBuilder.Append(", ");
                }
            }

            if (!string.IsNullOrEmpty(appendTags))
            {
                _stringBuilder.Append(", ");
                _stringBuilder.Append(appendTags);
            }

            string prompt = _stringBuilder.ToString().Replace(", , ", ", ").Replace(", ,", ", ").Replace("  ", " ");
            return prompt;
        }

        /// <summary>
        /// Generates multiple prompts based on provided tags and saves them to a specified file.
        /// </summary>
        /// <param name="outputFile">The path to the output file where prompts will be saved.</param>
        /// <param name="tags">An array of tags from which the prompts are generated.</param>
        /// <param name="prependTags">Optional tags to prepend to the generated prompts (can be empty).</param>
        /// <param name="appendTags">Optional tags to append to the generated prompts (can be empty).</param>
        /// <param name="amountOfTags">The number of tags to include in each generated prompt.</param>
        /// <param name="amountOfPrompts">The total number of prompts to generate and save.</param>
        /// <returns>A Task representing the asynchronous operation of generating and saving prompts.</returns>
        public async Task GeneratePromptsAndSaveToFile(string outputFile, string[] tags, string prependTags,
            string appendTags, int amountOfTags, int amountOfPrompts)
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            string[] prompts = new string[amountOfPrompts];

            for (int i = 0; i < amountOfPrompts; i++)
            {
                string generatedPrompt = GeneratePromptFromDataset(tags, prependTags, appendTags, amountOfTags);
                string cleanedPrompt = _tagProcessorService.ApplyRedundancyRemoval(generatedPrompt);
                prompts[i] = cleanedPrompt;
            }

            await File.AppendAllLinesAsync(outputFile, prompts);
        }
    }
}
