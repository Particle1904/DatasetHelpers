using Microsoft.ML.OnnxRuntime;

namespace SmartData.Lib.Services.Base
{
    public abstract class BaseAIConsumer<TInput, TOutput> : CancellableServiceBase
        where TInput : class
        where TOutput : class, new()
    {
        protected string _imageSearchPattern = "*.jpg,*.jpeg,*.png,*.gif,*.webp,";

        protected InferenceSession _session;

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

        protected bool _useGPU = true;

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
        protected virtual async Task LoadModel()
        {
            if (_session != null)
            {
                _session = null;
            }

            int[] gpuIdsToTry = { 0, 1 };

            SessionOptions sessionOptions = new SessionOptions();

            if (_useGPU)
            {
                sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                sessionOptions.EnableMemoryPattern = false;
                //sessionOptions.LogVerbosityLevel = 1;
                //sessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_VERBOSE;

                try
                {
                    sessionOptions.AppendExecutionProvider_DML(0);
                }
                catch (Exception) { /* DML Failed */ }

                try
                {
                    sessionOptions.AppendExecutionProvider_CUDA(0);
                }
                catch (Exception) { /* CUDA Failed */ }

                try
                {
                    sessionOptions.AppendExecutionProvider_ROCm(0);
                }
                catch (Exception) { /* ROCm Failed */ }
            }

            sessionOptions.ApplyConfiguration();
            _session = await Task.Run(() => new InferenceSession(ModelPath, sessionOptions));
            IsModelLoaded = true;
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
            _session?.Dispose();
            _isModelLoaded = false;
        }
    }
}
