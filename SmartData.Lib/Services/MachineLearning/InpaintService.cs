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
                await LoadModelAsync();
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
        /// and saves the result to the specified output path. This method uses an overlap-blend strategy to reduce visible seams.
        /// </summary>
        /// <param name="inputImagePath">The file path of the input image to be inpainted.</param>
        /// <param name="inputMaskPath">The file path of the input mask image used to indicate areas for inpainting.</param>
        /// <param name="outputImagePath">The file path where the inpainted output image will be saved.</param>
        /// <param name="tileSize">The size (width and height) of each square tile for processing. Defaults to 512.</param>
        /// <param name="overlap">The number of pixels that adjacent tiles will overlap. A larger value can improve quality but reduce performance. Defaults to 126.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method processes the input image and mask into overlapping tiles. It performs inpainting on each tile that contains a valid mask,
        /// then seamlessly blends the results together to form the final image. Tiles without a valid mask are copied directly.
        /// </remarks>
        public async Task InpaintImageTilesAsync(string inputImagePath, string inputMaskPath, string outputImagePath, int tileSize = 512, int overlap = 126)
        {
            if (!IsModelLoaded)
            {
                await LoadModelAsync();
            }

            TileData[] inputData = await _imageProcessor.ProcessImageForTileInpaintAsync(inputImagePath, inputMaskPath, tileSize, overlap);
            System.Drawing.Size originalSize;
            if (inputData.Length > 0)
            {
                SixLabors.ImageSharp.Point lamaPoint = inputData[0].LaMaInputData.OriginalSize;
                originalSize = new System.Drawing.Size(lamaPoint.X, lamaPoint.Y);
            }
            else
            {
                originalSize = new System.Drawing.Size(0, 0);
            }

            List<LaMaOutputData> outputs = new List<LaMaOutputData>();
            string[] inputColumns = GetInputColumns();
            for (int i = 0; i < inputData.Length; i++)
            {
                TileData currentTile = inputData[i];
                LaMaOutputData outputData = new LaMaOutputData()
                {
                    RowIndex = currentTile.RowIndex,
                    ColumnIndex = currentTile.ColumnIndex,
                    X = currentTile.X,
                    Y = currentTile.Y
                };

                if (CheckIfMaskIsValid(currentTile.LaMaInputData.InputMask.ToArray()))
                {
                    List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor<float>(inputColumns[0], currentTile.LaMaInputData.InputImage),
                        NamedOnnxValue.CreateFromTensor<float>(inputColumns[1], currentTile.LaMaInputData.InputMask)
                    };

                    using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
                    {
                        Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();
                        outputData.OutputImage = tensorPrediction.ToDenseTensor();
                    }
                }
                else
                {
                    DenseTensor<float> inputTensor = currentTile.LaMaInputData.InputImage;
                    ReadOnlySpan<int> dims = inputTensor.Dimensions;
                    DenseTensor<float> outputTensor = new DenseTensor<float>(dims);

                    int height = (int)dims[2];
                    int width = (int)dims[3];

                    for (int c = 0; c < 3; c++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                outputTensor[0, c, y, x] = inputTensor[0, c, y, x] * 255.0f;
                            }
                        }
                    }
                    outputData.OutputImage = outputTensor;
                }

                outputs.Add(outputData);
            }

            _imageProcessor.SaveInpaintedImage(outputImagePath, outputs.ToArray(), originalSize, tileSize, overlap);
        }

        /// <summary>
        /// Inpaints all images in a specified folder and saves the results to the specified output folder.
        /// </summary>
        /// <param name="inputFolderPath">The folder path containing the input images to be inpainted.</param>
        /// <param name="outputFolderPath">The folder path where the inpainted output images will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method finds all images in the input folder that do not contain "_mask" in their filenames,
        /// processes each image by calling <see cref="InpaintImageTilesAsync(string, string, string)"/>,
        /// and saves the resulting inpainted images to the output folder.
        /// It raises events to indicate total files to be processed and progress updates.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when an error occurs during inpainting of an image.</exception>
        public async Task InpaintImagesAsync(string inputFolderPath, string outputFolderPath)
        {
            if (!IsModelLoaded)
            {
                await LoadModelAsync();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, Utilities.GetSupportedImagesExtension)
                .Where(file => !file.Contains("_mask")).ToArray();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            string masksPath = Path.Combine(inputFolderPath, "masks");

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                string inputMask = Path.Combine(masksPath, $"{filenameWithoutExtension}_mask.jpeg");

                string outputImagePath = Path.Combine(outputFolderPath, $"{filenameWithoutExtension}.png");

                try
                {
                    await InpaintImageTilesAsync(file, inputMask, outputImagePath);
                }
                catch (FileNotFoundException)
                {
                    await Task.Run(() => File.Copy(file, outputImagePath), cancellationToken);
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

        /// <summary>
        /// Unloads the Inpaint model and disposes of its resources.
        /// </summary>
        public void UnloadAIModel()
        {
            UnloadModel();
        }
    }
}
