using Interfaces.MachineLearning;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using Models;
using Models.MachineLearning;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning
{
    public class InpaintService : BaseAIConsumer<LaMaInputData, LaMaOutputData>, IInpaintService, INotifyProgress, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public InpaintService(IImageProcessorService imageProcessor, string modelPath) : base(modelPath)
        {
            _imageProcessor = imageProcessor;
            _useGPU = false;
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "image", "mask" };
        }
        protected override string[] GetOutputColumns()
        {
            return new string[] { "output" };
        }

        /// <summary>
        /// Inpaints an image using the specified input image and mask file paths, and saves the result to the specified output path.
        /// </summary>
        /// <param name="inputImagePath">The file path of the input image to be inpainted.</param>
        /// <param name="inputMaskPath">The file path of the input mask image used to indicate areas for inpainting.</param>
        /// <param name="outputImagePath">The file path where the inpainted output image will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method processes the input image and mask,
        /// performs inpainting using the model, and saves the resulting image to the specified output path.
        /// </remarks>
        public async Task InpaintImageAsync(string inputImagePath, string inputMaskPath, string outputImagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
            }

            LaMaInputData inputData = await _imageProcessor.ProcessImageForInpaintAsync(inputImagePath, inputMaskPath);

            string[] inputColumns = GetInputColumns();
            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(inputColumns[0], inputData.InputImage),
                NamedOnnxValue.CreateFromTensor<float>(inputColumns[1], inputData.InputMask)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
            {
                Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

                float[] outputArray = tensorPrediction.ToArray();
                LaMaOutputData outputData = new LaMaOutputData()
                {
                    OutputImage = new DenseTensor<float>(outputArray, tensorPrediction.Dimensions.ToArray())
                };
                _imageProcessor.SaveInpaintedImage(outputImagePath, inputData, outputData);
            }
        }

        /// <summary>
        /// Inpaints an image by processing it in tiles using the specified input image and mask file paths, 
        /// and saves the result to the specified output path.
        /// </summary>
        /// <param name="inputImagePath">The file path of the input image to be inpainted.</param>
        /// <param name="inputMaskPath">The file path of the input mask image used to indicate areas for inpainting.</param>
        /// <param name="outputImagePath">The file path where the inpainted output image will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method processes the input image and mask into tiles, performs inpainting on each tile using the model,
        /// and saves the resulting image to the specified output path.
        /// Only tiles with a valid mask are inpainted; others are processed directly.
        /// </remarks>
        public async Task InpaintImageTilesAsync(string inputImagePath, string inputMaskPath, string outputImagePath)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
            }

            TileData[] inputData = await _imageProcessor.ProcessImageForTileInpaintAsync(inputImagePath, inputMaskPath);

            List<LaMaOutputData> outputs = new List<LaMaOutputData>();
            string[] inputColumns = GetInputColumns();
            for (int i = 0; i < inputData.Length; i++)
            {
                LaMaOutputData outputData = new LaMaOutputData()
                {
                    RowIndex = inputData[i].RowIndex,
                    ColumnIndex = inputData[i].ColumnIndex
                };

                // Run inference if the given tile have a valid Mask, otherwise just copy the original input data.
                if (CheckIfMaskIsValid(inputData[i].LaMaInputData.InputMask.ToArray()))
                {
                    List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor<float>(inputColumns[0], inputData[i].LaMaInputData.InputImage),
                        NamedOnnxValue.CreateFromTensor<float>(inputColumns[1], inputData[i].LaMaInputData.InputMask)
                    };

                    using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
                    {
                        Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

                        float[] outputArray = tensorPrediction.ToArray();

                        outputData.OutputImage = new DenseTensor<float>(outputArray, tensorPrediction.Dimensions.ToArray());
                    }
                }
                else
                {
                    float[] pixelData = inputData[i].LaMaInputData.InputImage.ToArray();
                    for (int j = 0; j < pixelData.Length; j++)
                    {
                        pixelData[j] = pixelData[j] * 255;
                    }

                    outputData.OutputImage = new DenseTensor<float>(pixelData, inputData[i].LaMaInputData.InputImage.Dimensions);
                }

                outputs.Add(outputData);
            }

            _imageProcessor.SaveInpaintedImage(outputImagePath, inputData, outputs.ToArray());
        }

        public async Task InpaintImagesAsync(string inputFolderPath, string outputFolderPath)
        {
            if (!IsModelLoaded)
            {
                await LoadModel();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, _imageSearchPattern)
                .Where(file => !file.Contains("_mask")).ToArray();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                string inputMask = Path.Combine(inputFolderPath, $"{filenameWithoutExtension}_mask.jpeg");

                string outputImagePath = Path.Combine(outputFolderPath, $"{filenameWithoutExtension}.png");

                try
                {
                    await InpaintImageTilesAsync(file, inputMask, outputImagePath);
                }
                catch (Exception)
                {
                    throw new ArgumentException($"An error occured while trying to inpaint the image.");
                }
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Checks if the provided mask data contains any valid values greater than 0.0.
        /// </summary>
        /// <param name="maskData">An array of float values representing the mask data.</param>
        /// <returns>True if any value in the mask data is greater than 0.0; otherwise, false.</returns>
        /// <remarks>
        /// This method iterates through the provided mask data to determine if there are any valid mask values.
        /// </remarks>
        private bool CheckIfMaskIsValid(float[] maskData)
        {
            bool isMaskValid = false;
            for (int i = 0; i < maskData.Length; i++)
            {
                if (maskData[i] > 0.0f)
                {
                    isMaskValid = true;
                    break;
                }
            }

            return isMaskValid;
        }

        public void UnloadAIModel()
        {
            UnloadModel();
        }
    }
}
