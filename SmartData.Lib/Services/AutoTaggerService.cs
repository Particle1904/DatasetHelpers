using Microsoft.ML.Data;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class AutoTaggerService : BaseAIConsumer<WDInputData, WDOutputData>, IAutoTaggerService
    {
        protected readonly ITagProcessorService _tagProcessorService;

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

        public AutoTaggerService(IImageProcessorService imageProcessorService, ITagProcessorService tagProcessorService, string modelPath, string tagsPath) : base(imageProcessorService, modelPath)
        {
            _tagProcessorService = tagProcessorService;
            _tagsPath = tagsPath;
        }

        protected override async Task LoadModel()
        {
            await base.LoadModel();

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

        protected override string[] GetInputColumns()
        {
            return new string[] { "input_1:0" };
        }
        protected override string[] GetOutputColumns()
        {
            return new string[] { "predictions_sigmoid" };
        }

        /// <summary>
        /// Asynchronously generates tags for images in the specified input path using a pre-trained model and saves the results to the specified output path.
        /// </summary>
        /// <param name="inputPath">The path to the folder containing the input images.</param>
        /// <param name="outputPath">The path to the folder where the output files will be saved.</param>
        /// <param name="weightedCaptions">Flag indicating whether to use weighted captions for tag generation.</param>
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
                await PostProcessTags(outputPath, weightedCaptions, file);
            }
        }

        /// <summary>
        /// Generates tags for the image files in the specified input folder, writes the results to text files in the specified output folder,
        /// and updates the progress object with the status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the text files will be written.</param>
        /// <param name="progress">The progress object to update with the status of the operation.</param>
        /// <param name="weightedCaptions">Flag indicating whether to use weighted captions for tag generation.</param>
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
                await PostProcessTags(outputPath, weightedCaptions, file);
                progress.UpdateProgress();
            }
        }

        public async Task<string> InterrogateImageFromStream(Stream imageStream)
        {
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            VBuffer<float> predictions = await GetPredictionAsync(imageStream).ConfigureAwait(false);
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

            foreach (KeyValuePair<string, float> item in sortedDict)
            {
                listOrdered.Add(item.Key);
            }

            foreach (string item in listOrdered)
            {
                item.Replace("_", " ");
            }

            string commaSeparated = _tagProcessorService.GetCommaSeparatedString(listOrdered);

            string redundantRemoved = _tagProcessorService.ApplyRedundancyRemoval(commaSeparated);

            return redundantRemoved;
        }

        /// <summary>
        /// Processes a list of tags from a file and generates a summarized version of the tags.
        /// </summary>
        /// <param name="outputPath">The output path where the summarized tags will be saved.</param>
        /// <param name="weightedCaptions">A boolean value indicating whether weighted captions are used.</param>
        /// <param name="file">The input file containing the tags to be processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task PostProcessTags(string outputPath, bool weightedCaptions, string file)
        {
            List<string> orderedPredictions = await GetOrderedByScoreListOfTagsAsync(file, weightedCaptions);
            string commaSeparated = _tagProcessorService.GetCommaSeparatedString(orderedPredictions);

            string redundantRemoved = _tagProcessorService.ApplyRedundancyRemoval(commaSeparated);

            string resultPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.txt");

            string tempFile = Path.Combine(outputPath, $"temp_{Path.GetFileName(file)}");
            File.Move(file, tempFile);

            string finalFile = tempFile.Replace("temp_", "");
            File.Move(tempFile, finalFile);

            await File.WriteAllTextAsync(resultPath, redundantRemoved);
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
        /// Retrieves predictions for the specified image file path using the prediction engine, which is a machine learning model that has been trained to make predictions. The method returns a <see cref="VBuffer{float}"/> object containing the predicted values.
        /// </summary>
        /// <param name="imagePath">The path of the image file to make predictions on.</param>
        /// <returns>A <see cref="VBuffer{float}"/> object containing the predicted values.</returns>
        private async Task<VBuffer<float>> GetPredictionAsync(string inputImagePath)
        {
            WDInputData inputData = await _imageProcessorService.ProcessImageForTagPredictionAsync(inputImagePath);

            WDOutputData prediction = await Task.Run(() => _predictionEngine.Predict(inputData));
            return prediction.PredictionsSigmoid;
        }

        /// <summary>
        /// Retrieves predictions for the specified image stream using the prediction engine, which is a machine learning model trained to make predictions. The method returns a <see cref="VBuffer{float}"/> object containing the predicted values.
        /// </summary>
        /// <param name="imageStream">The stream containing the image data to make predictions on.</param>
        /// <returns>A <see cref="VBuffer{float}"/> object containing the predicted values.</returns>
        private async Task<VBuffer<float>> GetPredictionAsync(Stream imageStream)
        {
            WDInputData inputData = await _imageProcessorService.ProcessImageForTagPredictionAsync(imageStream);

            WDOutputData prediction = await Task.Run(() => _predictionEngine.Predict(inputData));
            return prediction.PredictionsSigmoid;
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
    }
}