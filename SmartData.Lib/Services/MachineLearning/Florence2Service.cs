using FlorenceTwoLab.Core;

using Interfaces.MachineLearning;

using SixLabors.ImageSharp;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Services.Base;

namespace Services.MachineLearning
{
    public class Florence2Service : CancellableServiceBase, IFlorence2Service, INotifyProgress, IUnloadModel
    {
        private readonly Florence2Config _florence2Config;
        private readonly IFileManagerService _fileManager;
        private Florence2Pipeline _florence2Pipeline;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public Florence2Service(IFileManagerService fileManager, string modelsPath)
        {
            _fileManager = fileManager;

            _florence2Config = new Florence2Config()
            {
                MetadataDirectory = modelsPath,
                OnnxModelDirectory = modelsPath
            };
        }

        public async Task CaptionImagesAsync(string inputFolderPath, string outputFolderPath, Florence2CaptionTask captionTask)
        {
            await LoadFlorence2Pipeline();

            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            Florence2Query query = null;
            switch (captionTask)
            {
                case Florence2CaptionTask.Caption:
                default:
                    query = Florence2Tasks.CreateQuery(Florence2TaskType.Caption);
                    break;
                case Florence2CaptionTask.Detailed_Caption:
                    query = Florence2Tasks.CreateQuery(Florence2TaskType.DetailedCaption);
                    break;
                case Florence2CaptionTask.More_Detailed_Caption:
                    query = Florence2Tasks.CreateQuery(Florence2TaskType.MoreDetailedCaption);
                    break;
            }

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string captionedImagePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(file)}.png");
                if (File.Exists(captionedImagePath))
                {
                    continue;
                }
                try
                {
                    using (Image inputImage = Image.Load(file))
                    {
                        Florence2Result result = await _florence2Pipeline.ProcessAsync(inputImage, query);

                        string resultPath = Path.Combine(outputFolderPath, Path.GetFileName(file));
                        File.Move(file, resultPath);
                        _fileManager.SaveTextToFile(Path.Combine(outputFolderPath, Path.ChangeExtension(Path.GetFileName(file), ".txt")), result.Text.TrimEnd());
                    }
                }
                catch (Exception)
                {
                    {
                        throw;
                    }
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
            UnloadAIModel();
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
    }
}
