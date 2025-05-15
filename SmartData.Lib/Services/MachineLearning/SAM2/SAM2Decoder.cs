using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning.SAM2;
using SmartData.Lib.Services.Base;

using System.Drawing;
using System.Numerics;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    class SAM2Decoder : BaseAIConsumer<SAM2DecoderInputData, SAM2DecoderOutputData>
    {
        private readonly IImageProcessorService _imageProcessor;

        public SAM2Decoder(IImageProcessorService imageProcessor, string modelPath) : base(modelPath)
        {
            _imageProcessor = imageProcessor;
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "image_embed", "high_res_feats_0", "high_res_feats_1", "point_coords",
                "point_labels", "mask_input", "has_mask_input", "orig_im_size" };
        }

        protected override string[] GetOutputColumns()
        {
            return new string[] { "masks", "iou_predictions" };
        }

        /// <summary>
        /// Asynchronously generates segmentation masks from an image using SAM2 decoder, 
        /// based on a user-specified point and precomputed image embeddings.
        /// </summary>
        /// <param name="imagePath">The path to the image file to be used for segmentation.</param>
        /// <param name="imageEmbeds">The precomputed encoder outputs representing the image embeddings.</param>
        /// <param name="point">The point on the image to guide the segmentation, in pixel coordinates.</param>
        /// <returns>A <see cref="SAM2DecoderOutputData"/> object containing the generated segmentation masks and their confidence scores.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="imagePath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while accessing the file specified by <paramref name="imagePath"/>.</exception>
        public async Task<SAM2DecoderOutputData> GenerateImageMasksAsync(string imagePath, SAM2EncoderOutputData imageEmbeds, Point point)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
            }

            Size originalImageSize = await _imageProcessor.GetImageSizeAsync(imagePath);

            Vector2 pointCoordinates = GetCanvasPoint(point, originalImageSize.Width, originalImageSize.Height);

            SAM2DecoderInputData inputData = new SAM2DecoderInputData()
            {
                ImageEmbed = imageEmbeds.ImageEmbed,
                HighResFeats0 = imageEmbeds.HighResFeats0,
                HighResFeats1 = imageEmbeds.HighResFeats1,

                // Point related data
                PointCoords = new DenseTensor<float>(new[] { 1, 1, 2 })
                {
                    [0, 0, 0] = pointCoordinates.X,
                    [0, 0, 1] = pointCoordinates.Y
                },
                PointLabels = new DenseTensor<float>(new[] { 1, 1 })
                {
                    [0, 0] = 1f // Foreground
                },

                // Prior mask related data
                MaskInput = new DenseTensor<float>(new[] { 1, 1, 256, 256 }),
                HasMaskInput = new DenseTensor<float>(new[] { 1 })
                {
                    [0] = 0f // No prior mask
                },
            };

            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[0], inputData.ImageEmbed),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[1], inputData.HighResFeats0),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[2], inputData.HighResFeats1),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[3], inputData.PointCoords),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[4], inputData.PointLabels),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[5], inputData.MaskInput),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[6], inputData.HasMaskInput),
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediciton = await Task.Run(() => _session.Run(inputValues)))
            {
                // Extract predicted values into arrays.
                Tensor<float> masksPrediction = prediciton[0].AsTensor<float>();
                float[] masks = masksPrediction.ToArray();

                Tensor<float> iouPredictions = prediciton[1].AsTensor<float>();
                float[] iou = iouPredictions.ToArray();

                SAM2DecoderOutputData outputData = new SAM2DecoderOutputData()
                {
                    Masks = (DenseTensor<float>)prediciton[0].AsTensor<float>(),
                    IouPredictions = (DenseTensor<float>)prediciton[1].AsTensor<float>(),
                    OriginalResolution = originalImageSize
                };

                return outputData;
            }
        }

        /// <summary>
        /// Generates segmentation masks using SAM2 decoder, driven by a bounding‐box prompt instead of a single point.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <param name="imageEmbeds">Precomputed encoder outputs for this image.</param>
        /// <param name="box">The bounding box in original‐image pixel coordinates.</param>
        /// <returns>A <see cref="SAM2DecoderOutputData"/> containing masks and IoU scores.</returns>
        public async Task<SAM2DecoderOutputData> GenerateImageMasksAsync(string imagePath, SAM2EncoderOutputData imageEmbeds, Point topLeftPoint, Point bottomRightPoint)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
            }

            Size originalImageSize = await _imageProcessor.GetImageSizeAsync(imagePath);

            // Compute the two corner points on the 1024×1024 canvas

            Vector2 topLeft = GetCanvasPoint(topLeftPoint, originalImageSize.Width, originalImageSize.Height);
            Vector2 bottomRight = GetCanvasPoint(bottomRightPoint, originalImageSize.Width, originalImageSize.Height);

            // Build the decoder inputs
            SAM2DecoderInputData inputData = new SAM2DecoderInputData
            {
                ImageEmbed = imageEmbeds.ImageEmbed,
                HighResFeats0 = imageEmbeds.HighResFeats0,
                HighResFeats1 = imageEmbeds.HighResFeats1,

                PointCoords = new DenseTensor<float>(new[] { 1, 2, 2 })
                {
                    [0, 0, 0] = topLeft.X,
                    [0, 0, 1] = topLeft.Y,
                    [0, 1, 0] = bottomRight.X,
                    [0, 1, 1] = bottomRight.Y
                },
                PointLabels = new DenseTensor<float>(new[] { 1, 2 })
                {

                    [0, 0] = 2f,
                    [0, 1] = 3f
                },

                // Prior mask related data
                MaskInput = new DenseTensor<float>(new[] { 1, 1, 256, 256 }),
                HasMaskInput = new DenseTensor<float>(new[] { 1 })
                {
                    [0] = 0f // No prior mask
                },
            };

            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[0], inputData.ImageEmbed),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[1], inputData.HighResFeats0),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[2], inputData.HighResFeats1),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[3], inputData.PointCoords),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[4], inputData.PointLabels),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[5], inputData.MaskInput),
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns()[6], inputData.HasMaskInput),
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediciton = await Task.Run(() => _session.Run(inputValues)))
            {
                // Extract predicted values into arrays.
                Tensor<float> masksPrediction = prediciton[0].AsTensor<float>();
                float[] masks = masksPrediction.ToArray();

                Tensor<float> iouPredictions = prediciton[1].AsTensor<float>();
                float[] iou = iouPredictions.ToArray();

                SAM2DecoderOutputData outputData = new SAM2DecoderOutputData()
                {
                    Masks = (DenseTensor<float>)prediciton[0].AsTensor<float>(),
                    IouPredictions = (DenseTensor<float>)prediciton[1].AsTensor<float>(),
                    OriginalResolution = originalImageSize
                };

                return outputData;
            }
        }

        /// <summary>
        /// Converts a point from original image coordinates to pixel coordinates relative to SAM2's 1024x1024 canvas,
        /// accounting for scaling and padding.
        /// </summary>
        private Vector2 GetCanvasPoint(Point originalPoint, int originalWidth, int originalHeight)
        {
            const int targetSize = 1024;

            float scale = Math.Min((float)targetSize / originalWidth, (float)targetSize / originalHeight);

            int resizedWidth = (int)Math.Round(originalWidth * scale);
            int resizedHeight = (int)Math.Round(originalHeight * scale);

            int padX = (targetSize - resizedWidth) / 2;
            int padY = (targetSize - resizedHeight) / 2;

            float scaledX = originalPoint.X * scale + padX;
            float scaledY = originalPoint.Y * scale + padY;

            return new Vector2(scaledX, scaledY);
        }
    }
}
