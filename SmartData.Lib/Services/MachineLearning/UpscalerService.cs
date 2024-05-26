using Interfaces.MachineLearning;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning
{
    public class UpscalerService : BaseAIConsumer<UpscalerInputData, UpscalerOutputData>, IUpscalerService, INotifyProgress, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;

        private readonly string _modelsPath;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public UpscalerService(IImageProcessorService imageProcessor, string modelPath) : base(modelPath)
        {
            _imageProcessor = imageProcessor;
            _modelsPath = Path.Combine(AppContext.BaseDirectory, "models");
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "input" };
        }
        protected override string[] GetOutputColumns()
        {
            return new string[] { "output" };
        }

        public async Task UpscaleImagesAsync(string inputFolderPath, string outputFolderPath, AvailableModels model)
        {
            await LoadUpscalerModelAsync(model);

            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _imageSearchPattern);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string upscaledImagePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(file)}.png");
                if (File.Exists(upscaledImagePath))
                {
                    continue;
                }

                try
                {
                    await UpscaleImageAndSaveAsync(file, upscaledImagePath);
                }
                catch (Exception)
                {
                    throw new ArgumentException($"An error occured while trying to upscale image.{Environment.NewLine}It could be that the selected model can only upscale images that have Width and Height Divisible by 16 or 64!");
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Asynchronously upscales an input image and saves the upscaled image to the specified output path.
        /// </summary>
        /// <param name="inputImagePath">The path to the input image.</param>
        /// <param name="outputImagePath">The path to save the upscaled image.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpscaleImageAndSaveAsync(string inputImagePath, string outputImagePath)
        {
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
        private async Task<Image> UpscaleImageAsync(string inputImagePath)
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

        /// <summary>
        /// Asynchronously load the upscaler model.
        /// </summary>
        /// <param name="model">The upscaler model to load.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task LoadUpscalerModelAsync(AvailableModels model)
        {
            if (IsModelLoaded)
            {
                UnloadAIModel();
            }

            switch (model)
            {
                case AvailableModels.ParimgCompact_x2:
                    ModelPath = Path.Combine(_modelsPath, Filenames.ParimgCompactFilename);
                    break;
                case AvailableModels.HFA2kCompact_x2:
                    ModelPath = Path.Combine(_modelsPath, Filenames.HFA2kCompactFilename);
                    break;
                case AvailableModels.HFA2kAVCSRFormerLight_x2:
                    ModelPath = Path.Combine(_modelsPath, Filenames.HFA2kAVCSRFormerLightFilename);
                    break;
                case AvailableModels.HFA2k_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.HFA2kFilename);
                    break;
                case AvailableModels.SwinIR_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.SwinIRFilename);
                    break;
                case AvailableModels.Swin2SR_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.Swin2SRFilename);
                    break;
                case AvailableModels.Nomos8kSCSRFormer_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.Nomos8kSCSRFormerFilename);
                    break;
                case AvailableModels.Nomos8kSC_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.Nomos8kSCFilename);
                    break;
                case AvailableModels.LSDIRplusReal_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.LSDIRplusRealFilename);
                    break;
                case AvailableModels.LSDIRplusNone_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.LSDIRplusNoneFilename);
                    break;
                case AvailableModels.LSDIRplusCompression_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.LSDIRplusCompressionFilename);
                    break;
                case AvailableModels.LSDIRCompact3_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.LSDIRCompact3Filename);
                    break;
                case AvailableModels.LSDIR_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.LSDIRFilename);
                    break;
                case AvailableModels.Nomos8k_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.Nomos8kFilename);
                    break;
                case AvailableModels.Nomos8kDAT_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.Nomos8kDATFilename);
                    break;
                case AvailableModels.NomosUni_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.NomosUniFilename);
                    break;
                case AvailableModels.RealWebPhoto_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.RealWebPhotoFilename);
                    break;
                case AvailableModels.RealWebPhotoDAT_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.RealWebPhotoDATFilename);
                    break;
                case AvailableModels.SPANkendata_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.SPANkendataFilename);
                    break;
                case AvailableModels.GTAV5_x4:
                    ModelPath = Path.Combine(_modelsPath, Filenames.GTAVFilename);
                    break;
                case AvailableModels.JoyTag:
                case AvailableModels.WD14v2:
                case AvailableModels.WDv3:
                case AvailableModels.Z3DE621:
                case AvailableModels.Yolov4:
                case AvailableModels.CLIPTokenizer:
                default:
                    throw new ArgumentException("Model is not a Upscaler Model!");
            }

            await LoadModel();
        }

        public void UnloadAIModel()
        {
            UnloadModel();
        }
    }
}