using Enums;

using Microsoft.ML.OnnxRuntime;

namespace SmartData.Lib.Services.Base
{
    public abstract class BaseAIConsumer<TInput, TOutput> : CancellableServiceBase
        where TInput : class
        where TOutput : class, new()
    {
        protected InferenceSession _session;

        private readonly List<OnnxRuntimeProvider> _executionProviders = new List<OnnxRuntimeProvider>()
        {
            OnnxRuntimeProvider.CUDA,
            OnnxRuntimeProvider.ROCm,
            OnnxRuntimeProvider.DirectML
        };

        private readonly SemaphoreSlim _loadModelSemaphore = new SemaphoreSlim(1, 1);

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

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseAIConsumer{TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="modelPath">Path to the machine learning model.</param>
        protected BaseAIConsumer(string modelPath)
        {
            ModelPath = modelPath;
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
        /// Loads the machine learning model and initializes the prediction session.
        /// </summary>
        protected virtual async Task LoadModelAsync()
        {
            await _loadModelSemaphore.WaitAsync();
            try
            {
                if (IsModelLoaded)
                {
                    return;
                }

                ResetState();

                SessionOptions sessionOptions = new SessionOptions()
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    IntraOpNumThreads = 1,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    EnableMemoryPattern = false
                };

                foreach (OnnxRuntimeProvider provider in _executionProviders)
                {
                    if (TryAppendProvider(sessionOptions, provider))
                    {
                        break;
                    }
                }

                sessionOptions.ApplyConfiguration();
                _session = await Task.Run(() => new InferenceSession(ModelPath, sessionOptions));
                IsModelLoaded = true;
            }
            catch (Exception exception)
            {
                ResetState();

                throw new InvalidOperationException($"Failed to load model from {ModelPath}.", exception);
            }
            finally
            {
                _loadModelSemaphore.Release();
            }
        }

        /// <summary>
        /// Attempts to append the specified execution provider to the session options.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private bool TryAppendProvider(SessionOptions options, OnnxRuntimeProvider provider)
        {
            try
            {
                switch (provider)
                {
                    case OnnxRuntimeProvider.CUDA:
                        options.AppendExecutionProvider_CUDA();
                        break;
                    case OnnxRuntimeProvider.ROCm:
                        options.AppendExecutionProvider_ROCm();
                        break;
                    case OnnxRuntimeProvider.DirectML:
                        options.AppendExecutionProvider_DML();
                        break;
                }
                return true;
            }
            catch (Exception)
            {
                // The specified provider is not available.
                return false;
            }
        }

        /// <summary>
        /// Unloads the machine learning model and releases associated resources.
        /// </summary>
        /// <remarks>
        /// This method disposes of the session and releases any resources
        /// associated with it, setting the session to null.
        /// After calling this method, the model will no longer be loaded.
        /// </remarks>
        protected virtual void UnloadModel()
        {
            if (_loadModelSemaphore.Wait(1000))
            {
                try
                {
                    ResetState();
                }
                finally
                {
                    _loadModelSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Resets the state of the AI consumer, disposing of the session and setting the model loaded flag to false.
        /// </summary>
        private void ResetState()
        {
            _session?.Dispose();
            _session = null;
            IsModelLoaded = false;
        }
    }
}
