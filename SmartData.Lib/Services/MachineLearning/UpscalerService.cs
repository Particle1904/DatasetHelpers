using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning
{
    public class UpscalerService : BaseAIConsumer<UpscalerInputData, UpscalerOutputData>, IUpscalerService, INotifyProgress
    {
        private readonly IImageProcessorService _imageProcessor;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        private InferenceSession _session;

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

        public async Task UpscaleImageAsync(string inputImagePath, string outputImagePath)
        {
            // TODO: Change it so the model is loaded when upscaling
            // images from a folder instead of a single image.
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            UpscalerInputData inputData = await _imageProcessor.ProcessImageForUpscalingAsync(inputImagePath, 1.0f);
            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>("input", inputData.Input)
            };
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues));
            Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

            float[] outputArray = tensorPrediction.ToArray();
            var outputData = new UpscalerOutputData()
            {
                Output = new DenseTensor<float>(outputArray, tensorPrediction.Dimensions.ToArray())
            };

            _imageProcessor.SaveUpscaledImage(outputImagePath, outputData);
        }

        protected override async Task LoadModel()
        {
            SessionOptions sessionOptions = new SessionOptions();
            try
            {
                sessionOptions.AppendExecutionProvider_DML();
            }
            catch (Exception)
            {
                // LOG here
            }
            try
            {
                sessionOptions.AppendExecutionProvider_CUDA();
            }
            catch (Exception)
            {
                // LOG here
            }

            _session = new InferenceSession(ModelPath, sessionOptions);
        }
    }
}
