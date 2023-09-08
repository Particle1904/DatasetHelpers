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
            return _tagProcessorService.ApplyRedundancyRemoval(prompt);
        }
    }
}
