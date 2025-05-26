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
    public class TextRemoverService : CancellableServiceBase, ITextRemoverService, INotifyProgress
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IFlorence2Service _florence2;
        private readonly ISAM2Service _sam2;
        private readonly IInpaintService _inpaint;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public TextRemoverService(IImageProcessorService imageProcessor, IFlorence2Service florence2, ISAM2Service sam2, IInpaintService inpaint)
        {
            _imageProcessor = imageProcessor;
            _florence2 = florence2;
            _sam2 = sam2;
            _inpaint = inpaint;
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
            (_florence2 as IUnloadModel).UnloadAIModel();
        }

        /// <summary>
        /// Asynchronously detects and removes visible text, watermarks, and logos from an image using Florence2-based grounding,
        /// SAM2 segmentation, and inpainting. If text-like regions are found, the image is processed and saved as PNG.
        /// Otherwise, the original image is copied to the output path, preserving its format.
        /// </summary>
        /// <param name="inputImagePath">
        /// Full path to the input image from which text-like regions will be detected and removed.
        /// </param>
        /// <param name="outputImagePath">
        /// Full path (including desired filename) where the result will be saved. If processing occurs, the output format will be PNG,
        /// regardless of the extension provided.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous workflow of region detection, segmentation, inpainting,
        /// and image saving.
        /// </returns>
        /// <exception cref="System.Exception">
        /// Thrown if an error occurs during detection, mask generation, inpainting, or saving the output image.
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
                // CaptionToGrounding best performance so far in actually finding text, logos and other types of watermarks.
                Florence2Query query = Florence2Tasks.CreateQuery(Florence2TaskType.CaptionToGrounding, "text, watermark, logo, website, patreon, twitter, artist signature");
                Florence2Result result = await _florence2.ProcessAsync(inputImage, query);

                if (result.BoundingBoxes == null || result.BoundingBoxes.Count <= 0)
                {
                    // If no bounding box is found, copy the original image and return
                    File.Copy(inputImagePath, Path.ChangeExtension(outputImagePath, Path.GetExtension(inputImagePath)));
                    return;
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
    }
}
