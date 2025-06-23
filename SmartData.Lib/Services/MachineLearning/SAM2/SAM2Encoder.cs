using Interfaces.MachineLearning;
using Interfaces.MachineLearning.SAM2;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning.SAM2;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    public class SAM2Encoder : BaseAIConsumer<SAM2EncoderInputData, SAM2EncoderOutputData>, ISAM2Encoder, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;

        public SAM2Encoder(IImageProcessorService imageProcessor, string modelPath) : base(modelPath)
        {
            _imageProcessor = imageProcessor;
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "image" };
        }

        protected override string[] GetOutputColumns()
        {
            return new string[] { "high_res_feats_0", "high_res_feats_1", "image_embed" };
        }

        /// <summary>
        /// Asynchronously encodes an input image into SAM2-compatible feature embeddings using a pretrained model.
        /// </summary>
        /// <param name="inputImagePath">The file path to the input image to be encoded.</param>
        /// <returns>A <see cref="SAM2EncoderOutputData"/> object containing the encoded image embeddings and high-resolution features.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputImagePath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while processing the image file.</exception>
        public async Task<SAM2EncoderOutputData> EncodeImageEmbeds(string inputImagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModelAsync();
            }

            SAM2EncoderInputData inputData = await _imageProcessor.ProcessImageForSAM2EncodingAsync(inputImagePath);
            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns().FirstOrDefault(), inputData.InputImage)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
            {
                // Extract predicted values into arrays.
                Tensor<float> highResFeats0Prediction = prediction[0].AsTensor<float>();
                float[] highResFeats0 = highResFeats0Prediction.ToArray();

                Tensor<float> highResFeats1Prediction = prediction[1].AsTensor<float>();
                float[] highResFeats1 = highResFeats1Prediction.ToArray();

                Tensor<float> imageEmbedsPrediction = prediction[2].AsTensor<float>();
                float[] imageEmbeds = imageEmbedsPrediction.ToArray();

                SAM2EncoderOutputData outputData = new SAM2EncoderOutputData()
                {
                    HighResFeats0 = (DenseTensor<float>)prediction[0].AsTensor<float>().Clone(),
                    HighResFeats1 = (DenseTensor<float>)prediction[1].AsTensor<float>().Clone(),
                    ImageEmbed = (DenseTensor<float>)prediction[2].AsTensor<float>().Clone()
                };

                return outputData;
            }
        }

        public void UnloadAIModel()
        {
            UnloadModel();
        }
    }
}