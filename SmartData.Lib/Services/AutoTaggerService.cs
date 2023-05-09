using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

using System.Text;

namespace SmartData.Lib.Services
{
    public class AutoTaggerService : IAutoTaggerService
    {
        private string _imageSearchPattern = "*.jpg,*.jpeg,*.png,*.gif,*.webp,";

        private readonly IImageProcessorService _imageProcessorService;

        private MLContext _mlContext;
        private OnnxScoringEstimator _pipeline;
        private ITransformer _predictionPipe;
        private PredictionEngine<InputData, OutputData> _predictionEngine;

        private string[] _tags;

        private float _threshold = 0.2f;
        /// <summary>
        /// Gets or sets the threshold value for this object. The threshold value determines the cutoff point for certain calculations.
        /// </summary>
        /// <value>
        /// A <see cref="System.Single"/> value between 0.0 and 1.0, inclusive.
        /// </value>
        /// <remarks>
        /// The <see cref="Threshold"/> value must be a floating-point value between 0.0 and 1.0. Values outside this range will be clamped to the nearest valid value. 
        /// </remarks>
        public float Threshold
        {
            get => _threshold;
            set
            {
                _threshold = Math.Clamp(value, 0.0f, 1.0f);
            }
        }

        private string _modelPath;
        public string ModelPath
        {
            get
            {
                return _modelPath;
            }
            set
            {
                _modelPath = value;
            }
        }

        private string _tagsPath;
        public string TagsPath
        {
            get
            {
                return _tagsPath;
            }
            set
            {
                _tagsPath = value;
            }
        }

        private bool _isModelLoaded = false;
        public bool IsModelLoaded
        {
            get => _isModelLoaded;
            private set
            {
                _isModelLoaded = value;
            }
        }

        public AutoTaggerService(IImageProcessorService imageProcessorService, string modelPath, string tagsPath)
        {
            _imageProcessorService = imageProcessorService;

            _modelPath = modelPath;
            _tagsPath = tagsPath;

            _mlContext = new MLContext();
        }

        /// <summary>
        /// Asynchronously generates tags for images in the specified input path using a pre-trained model and saves the results to the specified output path.
        /// </summary>
        /// <param name="inputPath">The path to the folder containing the input images.</param>
        /// <param name="outputPath">The path to the folder where the output files will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateTags(string inputPath, string outputPath, bool weightedCaptions = false)
        {
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            foreach (string file in files)
            {
                List<string> orderedPredictions = await GetOrderedByScoreListOfTagsAsync(file, weightedCaptions);
                string commaSeparated = GetCommaSeparatedString(orderedPredictions);

                string resultPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.txt");

                string tempFile = Path.Combine(outputPath, $"temp_{Path.GetFileName(file)}");
                File.Move(file, tempFile);

                string finalFile = tempFile.Replace("temp_", "");
                File.Move(tempFile, finalFile);

                await File.WriteAllTextAsync(resultPath, commaSeparated);
            }
        }

        /// <summary>
        /// Generates tags for the image files in the specified input folder, writes the results to text files in the specified output folder,
        /// and updates the progress object with the status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the text files will be written.</param>
        /// <param name="progress">The progress object to update with the status of the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateTags(string inputPath, string outputPath, Progress progress, bool weightedCaptions = false)
        {
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (string file in files)
            {
                List<string> orderedPredictions = await GetOrderedByScoreListOfTagsAsync(file, weightedCaptions);
                string commaSeparated = GetCommaSeparatedString(orderedPredictions);

                string resultPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.txt");

                string tempFile = Path.Combine(outputPath, $"temp_{Path.GetFileName(file)}");
                File.Move(file, tempFile);

                string finalFile = tempFile.Replace("temp_", "");
                File.Move(tempFile, finalFile);

                await File.WriteAllTextAsync(resultPath, commaSeparated);
                progress.UpdateProgress();
            }
        }

        /// <summary>
        /// Returns a list of tags ordered by their score for a given image path.
        /// </summary>
        /// <param name="imagePath">The file path of the image to be analyzed.</param>
        /// <returns>A list of tags ordered by their score in descending order.</returns>
        private async Task<List<string>> GetOrderedByScoreListOfTagsAsync(string imagePath, bool weightedCaptions = false)
        {

            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            VBuffer<float> predictions = await GetPredictionAsync(imagePath).ConfigureAwait(false);
            float[] values = predictions.GetValues().ToArray();

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > _threshold)
                {
                    predictionsDict.Add(_tags[i], values[i]);
                }
            }

            IOrderedEnumerable<KeyValuePair<string, float>> sortedDict = predictionsDict.OrderByDescending(x => x.Value);

            List<string> listOrdered = new List<string>();
            if (weightedCaptions)
            {
                foreach (KeyValuePair<string, float> item in sortedDict)
                {
                    listOrdered.Add($"({item.Key}:{item.Value.ToString("F2")})");
                }
            }
            else
            {
                foreach (KeyValuePair<string, float> item in sortedDict)
                {
                    listOrdered.Add(item.Key);
                }
            }

            foreach (string item in listOrdered)
            {
                item.Replace("_", " ");
            }

            return listOrdered;
        }

        /// <summary>
        /// Loads the machine learning model and initializes the prediction pipeline and engine.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when either the model path or the tags path is null, empty, or consists only of white spaces.</exception>
        private async Task LoadModel()
        {
            _predictionPipe = await Task.Run(() => GetPredictionPipeline());
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<InputData, OutputData>(_predictionPipe);

            LoadTags(TagsPath);
            if (_tags?.Length > 0)
            {
                for (int i = 0; i < _tags.Length; i++)
                {
                    _tags[i] = _tags[i].Replace("_", " ");
                }
            }

            _isModelLoaded = true;
        }

        /// <summary>
        /// Retrieves predictions for the specified image file path using the prediction engine, which is a machine learning model that has been trained to make predictions. The method returns a <see cref="VBuffer{float}"/> object containing the predicted values.
        /// </summary>
        /// <param name="imagePath">The path of the image file to make predictions on.</param>
        /// <returns>A <see cref="VBuffer{float}"/> object containing the predicted values.</returns>
        private async Task<VBuffer<float>> GetPredictionAsync(string inputImagePath)
        {
            InputData inputData = await _imageProcessorService.ProcessImageForTagPrediction(inputImagePath);

            OutputData prediction = await Task.Run(() => _predictionEngine.Predict(inputData));
            return prediction.PredictionsSigmoid;
        }

        /// <summary>
        /// Retrieves a prediction pipeline for making predictions using the ONNX model.
        /// </summary>
        /// <returns>An instance of ITransformer representing the prediction pipeline.</returns>
        private ITransformer GetPredictionPipeline()
        {
            string[] inputColumns = new string[] { "input_1:0" };
            string[] outputColumns = new string[] { "predictions_sigmoid" };

            _pipeline = _mlContext.Transforms.ApplyOnnxModel(outputColumnNames: outputColumns, inputColumnNames: inputColumns, _modelPath);

            IDataView emptyDv = _mlContext.Data.LoadFromEnumerable(new InputData[] { });

            return _pipeline.Fit(emptyDv);
        }

        /// <summary>
        /// Loads tags from a CSV file and assigns them to the '_tags' field.
        /// </summary>
        /// <param name="csvPath">The path to the CSV file containing the tags.</param>
        private void LoadTags(string csvPath)
        {
            if (File.Exists(csvPath))
            {
                _tags = File.ReadAllLines(csvPath);
            }
        }

        /// <summary>
        /// Constructs a comma-separated string from the elements in the specified list.
        /// </summary>
        /// <param name="predictedTags">The list of tags to construct a string from.</param>
        /// <returns>A string that contains the elements of the specified list separated by commas.</returns>
        private string GetCommaSeparatedString(List<string> predictedTags)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string tag in predictedTags)
            {
                if (tag != predictedTags.LastOrDefault())
                {
                    stringBuilder.Append($"{tag}, ");
                }
                else
                {
                    stringBuilder.Append(tag);
                }
            }

            return stringBuilder.ToString();
        }
    }
}