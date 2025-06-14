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
        /// Asynchronously processes all supported image files in a folder to detect and remove text-like regions.
        /// The operation is performed in three distinct, memory-efficient stages to handle large batches of images
        /// without consuming excessive RAM.
        /// <list type="number">
        ///     <item>
        ///         <term>Stage 1: Detection</term>
        ///         <description>Uses the Florence-2 model to scan all images and identify bounding boxes for text, logos, and watermarks.</description>
        ///     </item>
        ///     <item>
        ///         <term>Stage 2: Segmentation</term>
        ///         <description>Uses the SAM2 model to generate precise segmentation masks for the bounding boxes found in the previous stage.</description>
        ///     </item>
        ///     <item>
        ///         <term>Stage 3: Inpainting</term>
        ///         <description>Uses an inpainting model (e.g., LaMa) to fill in the masked regions, effectively removing the text.</description>
        ///     </item>
        /// </list>
        /// Each model is loaded into memory only for its stage and unloaded immediately after, ensuring a stable memory footprint.
        /// </summary>
        /// <param name="inputFolderPath">
        /// The path to the folder containing the original images. A subfolder named <c>masks</c> will be created inside it
        /// to store intermediate segmentation mask files.
        /// </param>
        /// <param name="outputFolderPath">
        /// The path to the folder where the final, text-free PNG images will be saved. The folder must be writable.
        /// </-param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous, multi-stage text removal operation.
        /// </returns>
        /// <exception cref="System.OperationCanceledException">
        /// Thrown if the operation is canceled via the associated <see cref="CancellationToken"/>.
        /// </exception>
        public async Task RemoveTextFromImagesAsync(string inputFolderPath, string outputFolderPath)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            Dictionary<string, Florence2Result> florence2QueryResults = new Dictionary<string, Florence2Result>();
            Dictionary<string, string> imageMaskPaths = new Dictionary<string, string>();

            // STAGE 1: OBJECT DETECTION
            TotalFilesChanged?.Invoke(this, files.Length);
            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (Image inputImage = Image.Load(file))
                {
                    // CaptionToGrounding best performance so far in actually finding text, logos and other types of watermarks.
                    Florence2Query query = Florence2Tasks.CreateQuery(Florence2TaskType.CaptionToGrounding, "text, watermark, logo, website, patreon, twitter, artist signature");
                    Florence2Result result = await _florence2.ProcessAsync(inputImage, query);
                    florence2QueryResults.Add(file, result);
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
            (_florence2 as IUnloadModel).UnloadAIModel();


            // STAGE 2: IMAGE SEGMENTATION
            // Get the folder path for masks
            string masksPath = Path.Combine(inputFolderPath, "masks");
            if (!Directory.Exists(masksPath))
            {
                Directory.CreateDirectory(masksPath);
            }

            TotalFilesChanged?.Invoke(this, florence2QueryResults.Count);
            foreach (var keyValuePair in florence2QueryResults)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string file = keyValuePair.Key;
                Florence2Result result = keyValuePair.Value;

                if (result.BoundingBoxes == null || result.BoundingBoxes.Count == 0)
                {
                    continue;
                }

                List<Image<L8>> imageMasks = new List<Image<L8>>();
                try
                {
                    foreach (Rectangle item in result.BoundingBoxes)
                    {
                        System.Drawing.Point topLeft = new System.Drawing.Point(item.Left, item.Top);
                        System.Drawing.Point bottomRight = new System.Drawing.Point(item.Right, item.Bottom);
                        Image<L8> currentMask = await _sam2.SegmentObjectFromBoundingBoxAsync(file, topLeft, bottomRight);
                        imageMasks.Add(currentMask);
                    }

                    string filenameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    string outputMaskPath = Path.Combine(masksPath, $"{filenameWithoutExtension}_mask.jpeg");
                    await _imageProcessor.CombineListOfMasksAsync(imageMasks, outputMaskPath);
                    imageMaskPaths.Add(file, outputMaskPath);
                }
                finally
                {
                    foreach (Image<L8> image in imageMasks)
                    {
                        image.Dispose();
                    }
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
            (_sam2 as IUnloadModel).UnloadAIModel();

            // STAGE 3: IMAGE INPAINTING
            TotalFilesChanged?.Invoke(this, files.Length);
            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string textRemovedImagePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(file)}.png");

                if (imageMaskPaths.TryGetValue(file, out string maskPath))
                {
                    await _inpaint.InpaintImageTilesAsync(file, maskPath, textRemovedImagePath);
                }
                else
                {
                    File.Copy(file, Path.ChangeExtension(textRemovedImagePath, Path.GetExtension(file)), true);
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
            (_inpaint as IUnloadModel).UnloadAIModel();
        }
    }
}
