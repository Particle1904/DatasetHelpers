using FlorenceTwoLab.Core;

using Interfaces.MachineLearning;

using SixLabors.ImageSharp;

namespace Services.MachineLearning
{
    public class Florence2Service : IFlorence2Service, IUnloadModel
    {
        private readonly Florence2Config _florence2Config;
        private Florence2Pipeline _florence2Pipeline;

        public Florence2Service(string modelsPath)
        {
            _florence2Config = new Florence2Config()
            {
                MetadataDirectory = modelsPath,
                OnnxModelDirectory = modelsPath
            };
        }

        /// <summary>
        /// Asynchronously initializes and loads the Florence2 pipeline if it has not already been loaded.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous model-loading operation.
        /// </returns>
        private async Task LoadFlorence2Pipeline()
        {
            if (_florence2Pipeline is null)
            {
                _florence2Pipeline = await Florence2Pipeline.CreateAsync(_florence2Config);
            }
        }

        public async Task<Florence2Result> ProcessAsync(Image image, Florence2Query query)
        {
            await LoadFlorence2Pipeline();

            return await _florence2Pipeline.ProcessAsync(image, query);
        }

        public void UnloadAIModel()
        {
            _florence2Pipeline?.Dispose();
            _florence2Pipeline = null;
        }
    }
}
