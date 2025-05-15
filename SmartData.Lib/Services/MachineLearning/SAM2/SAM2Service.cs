using Interfaces.MachineLearning;
using Interfaces.MachineLearning.SAM2;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning.SAM2;

using System.Drawing;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    public class SAM2Service : ISAM2Service, INotifyProgress, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        private SAM2Encoder _encoder;
        private SAM2Decoder _decoder;

        public SAM2Service(IImageProcessorService imageProcessor, string encoderModelPath, string decoderModelPath)
        {
            _imageProcessor = imageProcessor;

            _encoder = new SAM2Encoder(_imageProcessor, encoderModelPath);
            _decoder = new SAM2Decoder(_imageProcessor, decoderModelPath);
        }

        /// <summary>
        /// Asynchronously segments an object in the image specified by a single point prompt,
        /// then saves the resulting mask to the given output path.
        /// </summary>
        /// <param name="inputPath">The path to the source image file to be segmented.</param>
        /// <param name="point">The pixel coordinate on the image to guide the segmentation.</param>
        /// <param name="outputPath">The path where the generated segmentation mask will be saved.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file specified by <paramref name="inputPath"/> does not exist.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an I/O error occurs while reading the source image or writing the mask file.
        /// </exception>
        public async Task SegmentObjectFromPointAsync(string inputPath, Point point, string outputPath)
        {
            SAM2EncoderOutputData encoderOutput = await _encoder.EncodeImageEmbeds(inputPath);
            SAM2DecoderOutputData result = await _decoder.GenerateImageMasksAsync(inputPath, encoderOutput, point);
            await _imageProcessor.SaveSAM2MaskAsync(result, outputPath);
        }

        /// <summary>
        /// Asynchronously segments an object in the image specified by a bounding‐box prompt,
        /// then saves the resulting mask to the given output path.
        /// </summary>
        /// <param name="inputPath">The path to the source image file to be segmented.</param>
        /// <param name="topLeftPoint">The top‐left corner of the bounding box, in pixel coordinates.</param>
        /// <param name="bottomRightPoint">The bottom‐right corner of the bounding box, in pixel coordinates.</param>
        /// <param name="outputPath">The path where the generated segmentation mask will be saved.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file specified by <paramref name="inputPath"/> does not exist.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an I/O error occurs while reading the source image or writing the mask file.
        /// </exception>
        public async Task SegmentObjectFromBoundingBoxAsync(string inputPath, Point topLeftPoint, Point bottomRightPoint, string outputPath)
        {
            SAM2EncoderOutputData encoderOutput = await _encoder.EncodeImageEmbeds(inputPath);
            SAM2DecoderOutputData result = await _decoder.GenerateImageMasksAsync(inputPath, encoderOutput, topLeftPoint, bottomRightPoint);
            await _imageProcessor.SaveSAM2MaskAsync(result, outputPath);
        }

        public void UnloadAIModel()
        {
            throw new NotImplementedException();
        }
    }
}
