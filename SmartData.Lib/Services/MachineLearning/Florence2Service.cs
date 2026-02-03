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

        private const string _eosToken = "</s>";

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

        /// <summary>
        /// Asynchronously captions images from the specified input folder and saves the results to the output folder.
        /// </summary>
        /// <param name="inputFolderPath">The path to the folder containing input images.</param>
        /// <param name="outputFolderPath">The path to the folder where captioned images and text files will be saved.</param>
        /// <param name="captionTask">The captioning task type to apply to the images.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
        /// <exception cref="Exception">Propagates any exception that occurs during processing.</exception>
        public async Task CaptionImagesAsync(string inputFolderPath, string outputFolderPath, Florence2CaptionTask captionTask)
        {
            try
            {
                await Task.Run(LoadFlorence2PipelineAsync);
            }
            catch (Exception)
            {
                throw;
            }

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

            int counter = 0;

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string captionedImagePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(file)}.webp");
                if (File.Exists(captionedImagePath))
                {
                    continue;
                }
                try
                {
                    await Task.Run(async () =>
                    {
                        using (Image inputImage = Image.Load(file))
                        {
                            Florence2Result result = await _florence2Pipeline.ProcessAsync(inputImage, query);

                            string resultPath = Path.Combine(outputFolderPath, Path.GetFileName(file));
                            File.Move(file, resultPath);

                            string caption = result.Text.Trim();
                            // Make sure to remove EOS token, if its present.
                            if (caption.EndsWith(_eosToken))
                            {
                                caption = caption.Substring(0, caption.Length - _eosToken.Length);
                            }
                            await _fileManager.SaveTextToFileAsync(Path.Combine(outputFolderPath, Path.ChangeExtension(Path.GetFileName(file), ".txt")), caption.TrimEnd());
                        }
                    });
                }
                catch (Exception)
                {
                    {
                        throw;
                    }
                }

                // Try to be more aggressive with garbage collection. Perform GC every 10 images to free up memory.
                if (counter % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                counter++;
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
            UnloadAIModel();
        }

        /// <summary>
        /// Processes a single image with the specified Florence2 query and returns the result.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <param name="query">The Florence2 query configuration to use.</param>
        /// <returns>A task that represents the asynchronous operation, containing the result.</returns>
        public async Task<Florence2Result> ProcessAsync(Image image, Florence2Query query)
        {
            await LoadFlorence2PipelineAsync();

            return await Task.Run(() => _florence2Pipeline.ProcessAsync(image, query));
        }

        /// <summary>
        /// Unloads the Florence2 pipeline and disposes of its resources.
        /// </summary>
        public void UnloadAIModel()
        {
            _florence2Pipeline.Dispose();
            _florence2Pipeline = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Asynchronously initializes and loads the Florence2 pipeline if it has not already been loaded.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous model-loading operation.
        /// </returns>
        private async Task LoadFlorence2PipelineAsync()
        {
            if (_florence2Pipeline is null)
            {
                _florence2Pipeline = await Florence2Pipeline.CreateAsync(_florence2Config);
            }
        }
    }
}
