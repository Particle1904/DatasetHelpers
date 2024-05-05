using Interfaces.MachineLearning;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning
{
    public class UpscalerService : BaseAIConsumer<UpscalerInputData, UpscalerOutputData>, IUpscalerService, INotifyProgress, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public UpscalerService(IImageProcessorService imageProcessor, string modelPath) : base(modelPath)
        {
            _imageProcessor = imageProcessor;
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "input" };
        }
        protected override string[] GetOutputColumns()
        {
            return new string[] { "output" };
        }

        /// <summary>
        /// Asynchronously upscales an input image and saves the upscaled image to the specified output path.
        /// </summary>
        /// <param name="inputImagePath">The path to the input image.</param>
        /// <param name="outputImagePath">The path to save the upscaled image.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpscaleImageAndSaveAsync(string inputImagePath, string outputImagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
                _isModelLoaded = true;
            }

            UpscalerInputData inputData = await _imageProcessor.ProcessImageForUpscalingAsync(inputImagePath);
            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns().FirstOrDefault(), inputData.Input)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
            {
                Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

                float[] outputArray = tensorPrediction.ToArray();
                UpscalerOutputData outputData = new UpscalerOutputData()
                {
                    Output = new DenseTensor<float>(outputArray, tensorPrediction.Dimensions.ToArray())
                };
                _imageProcessor.SaveUpscaledImage(outputImagePath, outputData);
            }
        }

        /// <summary>
        /// Asynchronously upscales an input image.
        /// </summary>
        /// <param name="inputImagePath">The path to the input image.</param>
        /// <returns>A task representing the asynchronous operation. The result is the upscaled image.</returns>
        public async Task<Image> UpscaleImageAsync(string inputImagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
                _isModelLoaded = true;
            }

            UpscalerInputData inputData = await _imageProcessor.ProcessImageForUpscalingAsync(inputImagePath);
            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns().FirstOrDefault(), inputData.Input)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
            {
                Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

                float[] outputArray = tensorPrediction.ToArray();
                UpscalerOutputData outputData = new UpscalerOutputData()
                {
                    Output = new DenseTensor<float>(outputArray, tensorPrediction.Dimensions.ToArray())
                };
                return _imageProcessor.GetUpscaledImage(outputData);
            }
        }

        public void UnloadAIModel()
        {
            UnloadModel();
        }
    }
}