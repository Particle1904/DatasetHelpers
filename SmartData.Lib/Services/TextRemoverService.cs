using FlorenceTwoLab.Core;

using Interfaces.MachineLearning;
using Interfaces.MachineLearning.SAM2;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services.Base;

namespace Services
{
    public class TextRemoverService : CancellableServiceBase, ITextRemoverService, INotifyProgress, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly ISAM2Service _sam2;
        private readonly IInpaintService _inpaint;
        private readonly Florence2Config _florence2Config;
        private Florence2Pipeline _florence2Pipeline;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public TextRemoverService(IImageProcessorService imageProcessor, ISAM2Service sam2, IInpaintService inpaint, string modelsPath)
        {
            _imageProcessor = imageProcessor;
            _sam2 = sam2;
            _inpaint = inpaint;

            _florence2Config = new Florence2Config()
            {
                MetadataDirectory = modelsPath,
                OnnxModelDirectory = modelsPath
            };
        }

        /// <summary>
        /// Asynchronously processes all supported image files in the specified input folder by detecting and removing
        /// text regions using OCR and inpainting, then saves the resulting images to the output folder.
        /// </summary>
        /// <param name="inputFolderPath">
        /// The path to the folder containing the original images. A subfolder named <c>masks</c> will be created inside it
        /// to store intermediate binary mask files.
        /// </param>
        /// <param name="outputFolderPath">
        /// The path to the folder where the final text-free PNG images will be saved. The folder must be writable.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous batch text-removal and save operation.
        /// </returns>
        /// <exception cref="System.Exception">
        /// Thrown if an error occurs while processing an individual image.
        /// </exception>
        /// <exception cref="System.OperationCanceledException">
        /// Thrown if the operation is canceled via the associated <see cref="CancellationToken"/>.
        /// </exception>
        public async Task RemoveTextFromImagesAsync(string inputFolderPath, string outputFolderPath)
        {
            await LoadFlorence2Pipeline();

            // Get the folder path for masks
            string masksPath = Path.Combine(inputFolderPath, "masks");
            if (!Directory.Exists(masksPath))
            {
                Directory.CreateDirectory(masksPath);
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string textRemovedImagePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(file)}.png");
                if (File.Exists(textRemovedImagePath))
                {
                    continue;
                }

                try
                {
                    await RemoveTextFromImageAndSaveAsync(file, textRemovedImagePath);
                }
                catch (Exception exception)
                {
                    throw new Exception($"An error processing file {file}: {exception.Message}");
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }

            // Unload Florence2 Pipeline to free up resources.
            UnloadAIModel();
        }

        /// <summary>
        /// Asynchronously removes detected text regions from a single image using OCR, SAM2-based segmentation, and inpainting,
        /// then saves the result to the specified output path.
        /// </summary>
        /// <param name="inputImagePath">
        /// The full file path of the input image from which text will be removed.
        /// </param>
        /// <param name="outputImagePath">
        /// The file path where the final inpainted image will be saved. The parent directory must be writable.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation of OCR-based detection, mask generation,
        /// inpainting, and file-saving.
        /// </returns>
        /// <exception cref="System.Exception">
        /// Thrown if an error occurs during text region detection, mask generation, or image saving.
        /// </exception>
        private async Task RemoveTextFromImageAndSaveAsync(string inputImagePath, string outputImagePath)
        {
            // Get the folder path for masks
            string inputFolderPath = Path.GetDirectoryName(inputImagePath);
            string masksPath = Path.Combine(inputFolderPath, "masks");

            string filenameWithoutExtension = Path.GetFileNameWithoutExtension(inputImagePath);
            string outputMaskPath = Path.Combine(masksPath, $"{filenameWithoutExtension}_mask.jpeg");

            using (Image inputImage = Image.Load(inputImagePath))
            {
                // Optical Character Recognition (OCR) with Regions
                Florence2Query query = Florence2Tasks.CreateQuery(Florence2TaskType.OcrWithRegions);
                //Florence2Query query = new Florence2Query(Florence2TaskType.RegionProposal, "Locate the text regions in the image");
                Florence2Result result = await _florence2Pipeline.ProcessAsync(inputImage, query);

                if (result.BoundingBoxes.Count <= 0)
                {
                    // If no bounding box is found, copy the original image
                    File.Copy(inputImagePath, outputImagePath);
                }

                List<Image<L8>> imageMasks = new List<Image<L8>>();
                try
                {
                    foreach (Rectangle item in result.BoundingBoxes)
                    {
                        System.Drawing.Point topLeft = new System.Drawing.Point(item.Left, item.Top);
                        System.Drawing.Point bottomRight = new System.Drawing.Point(item.Right, item.Bottom);
                        Image<L8> currentMask = await _sam2.SegmentObjectFromBoundingBoxAsync(inputImagePath, topLeft, bottomRight);
                        imageMasks.Add(currentMask);
                    }
                    await _imageProcessor.CombineListOfMasksAsync(imageMasks, outputMaskPath);
                    await _inpaint.InpaintImageTilesAsync(inputImagePath, outputMaskPath, outputImagePath);
                }
                finally
                {
                    foreach (Image<L8> imageMask in imageMasks)
                    {
                        imageMask.Dispose();
                    }
                }
            }
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

        public void UnloadAIModel()
        {
            _florence2Pipeline?.Dispose();
            _florence2Pipeline = null;
        }
    }
}
