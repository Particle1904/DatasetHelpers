using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

using SmartData.Lib.Interfaces.MachineLearning;

namespace SmartData.Lib.Services.MachineLearning
{
    public class CLIPTokenizerService : ICLIPTokenizerService
    {
        private InferenceSession _session;

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

        public CLIPTokenizerService(string modelPath)
        {
            ModelPath = modelPath;
        }

        /// <summary>
        /// Counts the tokens in the input text after tokenization using the loaded model.
        /// </summary>
        /// <param name="inputText">The input text to tokenize and count tokens.</param>
        /// <returns>The total number of tokens in the input text.</returns>
        public int CountTokens(string inputText)
        {
            if (!_isModelLoaded)
            {
                LoadModel();
            }

            if (string.IsNullOrEmpty(inputText))
            {
                return 0;
            }

            DenseTensor<string> inputTensor = new DenseTensor<string>(new string[] { inputText }, new int[] { 1 });
            List<NamedOnnxValue> inputString = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor<string>("string_input", inputTensor) };
            IDisposableReadOnlyCollection<DisposableNamedOnnxValue> tokens = _session.Run(inputString);
            List<long> inputIds = (tokens.ToList().First().Value as IEnumerable<long>).ToList();
            // Remove beginning (49406) of stream and ending (49407) of stream tokens
            // since they don't matter for counting the prompt total tokens.
            inputIds.Remove(49406);
            inputIds.Remove(49407);
            return inputIds.Count();
        }

        /// <summary>
        /// Loads the onnx extension model with custom operation for CLIP tokenization and initializes the ONNX inference session.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when either the model path is null, empty, or consists only of white spaces.</exception>
        private void LoadModel()
        {
            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.RegisterOrtExtensions();
            _session = new InferenceSession(ModelPath, sessionOptions);
        }
    }
}