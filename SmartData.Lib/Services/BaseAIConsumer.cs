﻿using Microsoft.ML;
using Microsoft.ML.Transforms.Onnx;

using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services
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

        protected string _modelPath;
        public string ModelPath
        {
            get => _modelPath;
            set
            {
                _modelPath = value;
            }
        }

        protected bool _isModelLoaded = false;
        public bool IsModelLoaded
        {
            get => _isModelLoaded;
            private set
            {
                _isModelLoaded = value;
            }
        }

        public BaseAIConsumer(IImageProcessorService imageProcessorService, string modelPath)
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

            _pipeline = _mlContext.Transforms.ApplyOnnxModel(outputColumnNames: outputColumns, inputColumnNames: inputColumns, _modelPath);

            IDataView emptyDv = _mlContext.Data.LoadFromEnumerable<TData>(new TData[] { });

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
    }
}