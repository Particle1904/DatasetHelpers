using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;

using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services.Base
{
    public abstract class BaseAIConsumer<TInput, TOutput>
        where TInput : class
        where TOutput : class, new()
    {
        protected readonly IImageProcessorService _imageProcessorService;

        protected string _imageSearchPattern = "*.jpg,*.jpeg,*.png,*.gif,*.webp,";

        protected MLContext _mlContext;
        protected OnnxScoringEstimator _pipeline;
        protected PredictionEngine<TInput, TOutput> _predictionEngine;
        protected ITransformer _predictionPipe;

        public string ModelPath { get; set; }

        protected bool _isModelLoaded = false;
        public bool IsModelLoaded
        {
            get => _isModelLoaded;
            private set
            {
                _isModelLoaded = value;
            }
        }

        protected BaseAIConsumer(IImageProcessorService imageProcessorService, string modelPath)
        {
            _imageProcessorService = imageProcessorService;

            ModelPath = modelPath;

            _mlContext = new MLContext();
        }

        /// <summary>
        /// Retrieves an array of input column names for the machine learning model.
        /// </summary>
        /// <returns>An array of strings representing the input column names.</returns>
        protected abstract string[] GetInputColumns();

        /// <summary>
        /// Retrieves an array of output column names for the machine learning model.
        /// </summary>
        /// <returns>An array of strings representing the output column names.</returns>
        protected abstract string[] GetOutputColumns();

        /// <summary>
        /// Retrieves a prediction pipeline for making predictions using the ONNX model.
        /// </summary>
        /// <returns>An instance of ITransformer representing the prediction pipeline.</returns>
        protected ITransformer GetPredictionPipeline<TData>() where TData : class
        {
            string[] inputColumns = GetInputColumns();
            string[] outputColumns = GetOutputColumns();

            int[] gpuIdsToTry = new int[] { 0, 1, 2, 3 };

            for (int i = 0; i < gpuIdsToTry.Length; i++)
            {
                // Try to load into GPUs 0 and 1; fall back to CPU if both GPUs failed.
                try
                {
                    _pipeline = _mlContext.Transforms.ApplyOnnxModel(outputColumnNames: outputColumns,
                            inputColumnNames: inputColumns, ModelPath, i, true);
                    break;
                }
                catch (EntryPointNotFoundException)
                {
                    if (i == gpuIdsToTry.Length - 1)
                    {
                        // Fall back to CPU if all GPUs failed.
                        _pipeline = _mlContext.Transforms.ApplyOnnxModel(outputColumnNames: outputColumns,
                            inputColumnNames: inputColumns, ModelPath);
                    }
                }
            }

            IDataView emptyDv = _mlContext.Data.LoadFromEnumerable<TData>(Array.Empty<TData>());

            return _pipeline.Fit(emptyDv);
        }

        /// <summary>
        /// Loads the machine learning model and initializes the prediction pipeline and engine.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when either the model path or the tags path is null, empty, or consists only of white spaces.</exception>
        protected virtual async Task LoadModel()
        {
            _predictionPipe = await Task.Run(() => GetPredictionPipeline<TInput>());
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<TInput, TOutput>(_predictionPipe);
        }

        /// <summary>
        /// Unloads the machine learning model and releases associated resources.
        /// </summary>
        /// <remarks>
        /// This method disposes of the prediction engine and releases any resources
        /// associated with it, setting the prediction engine and prediction pipeline to null.
        /// After calling this method, the model will no longer be loaded.
        /// </remarks>
        protected virtual void UnloadModel()
        {
            _predictionEngine?.Dispose();
            _predictionEngine = null;
            _predictionPipe = null;

            _isModelLoaded = false;
        }
    }
}
