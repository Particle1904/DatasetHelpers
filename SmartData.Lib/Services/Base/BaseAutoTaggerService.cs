using Interfaces.MachineLearning;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

namespace SmartData.Lib.Services.Base
{
    public abstract class BaseAutoTaggerService<TInput, TOutput> : BaseAIConsumer<TInput, TOutput>, IAutoTaggerService, IUnloadModel,
            INotifyProgress
        where TInput : class
        where TOutput : class, new()
    {
        protected readonly IImageProcessorService _imageProcessor;
        protected readonly ITagProcessorService _tagProcessor;

        protected string[] _tags;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        protected float _threshold = 0.2f;
        /// <summary>
        /// Gets or sets the threshold value for this object. The threshold value determines the cutoff point for certain calculations.
        /// </summary>
        /// <value>
        /// A floating-point value between 0.0 and 1.0, inclusive.
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

        /// <summary>
        /// Path to the directory where tag files are stored.
        /// </summary>
        public string TagsPath { get; set; }

        /// <summary>
        /// Initializes a new instance of the AutoTaggerService class.
        /// </summary>
        /// <param name="imageProcessor">The service responsible for image processing.</param>
        /// <param name="tagProcessor">The service responsible for processing tags.</param>
        /// <param name="modelPath">The path to the machine learning model.</param>
        /// <param name="tagsPath">The path to the directory where tag files are stored.</param>
        public BaseAutoTaggerService(IImageProcessorService imageProcessor, ITagProcessorService tagProcessor, string modelPath,
            string tagsPath) : base(modelPath)
        {
            _imageProcessor = imageProcessor;
            _tagProcessor = tagProcessor;
            TagsPath = tagsPath;
        }

        protected override async Task LoadModelAsync()
        {
            await base.LoadModelAsync();

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
        /// Generates tags for the image files in the specified input folder, writes the results to text files in the specified output folder,
        /// and raise events to signal the progress status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the text files will be written.</param>
        /// <param name="weightedCaptions">Flag indicating whether to use weighted captions for tag generation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateTags(string inputPath, string outputPath, bool weightedCaptions = false)
        {
            if (!_isModelLoaded)
            {
                await LoadModelAsync();
                _isModelLoaded = true;
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await PostProcessTags(outputPath, weightedCaptions, file);
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Generates tags for the image files in the specified input folder, appends the results to text files in the specified output folder,
        /// and raise events to signal the progress status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the text files will be written.</param>
        /// <param name="weightedCaptions">Flag indicating whether to use weighted captions for tag generation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateTagsAndAppendToFile(string inputPath, string outputPath, bool weightedCaptions = false)
        {
            if (!_isModelLoaded)
            {
                await LoadModelAsync();
                _isModelLoaded = true;
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await PostProcessTagsAndAppendToFile(outputPath, weightedCaptions, file);
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Generates tags for the image files in the specified input folder, appends the results to text files
        /// in the specified output folder, and raise events to signal the progress status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the text files will be written.</param>
        /// <param name="appendToFile">Flag indicating whether to append tags to existing tag files (if available).</param>
        /// <param name="weightedCaptions">Flag indicating whether to use weighted captions for tag generation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateTagsAndKeepRedundant(string inputPath, string outputPath, bool appendToFile, bool weightedCaptions = false)
        {
            if (!_isModelLoaded)
            {
                await LoadModelAsync();
                _isModelLoaded = true;
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await GenerateTagsWithRedundant(outputPath, appendToFile, weightedCaptions, file);
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Generates tags for an image file, appends the results to a text file, and renames the image file.
        /// </summary>
        /// <param name="outputPath">The path to the folder where the text files will be written.</param>
        /// <param name="appendToFile">Flag indicating whether to append tags to an existing tag file (if available).</param>
        /// <param name="weightedCaptions">Flag indicating whether to use weighted captions for tag generation.</param>
        /// <param name="file">The path to the image file to process.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task GenerateTagsWithRedundant(string outputPath, bool appendToFile, bool weightedCaptions, string file)
        {
            List<string> orderedPredictions = await GetOrderedByScoreListOfTagsAsync(file, weightedCaptions);
            string commaSeparated = _tagProcessor.GetCommaSeparatedString(orderedPredictions);

            string txtFile = Path.ChangeExtension(file, ".txt");

            if (File.Exists(txtFile) && appendToFile)
            {
                string existingCaption = await File.ReadAllTextAsync(txtFile);
                commaSeparated = $"{existingCaption.Replace("_", " ")}, {commaSeparated}";
            }

            string resultPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.txt");

            string tempFile = Path.Combine(outputPath, $"temp_{Path.GetFileName(file)}");
            File.Move(file, tempFile);

            string finalFile = tempFile.Replace("temp_", "");
            File.Move(tempFile, finalFile);

            await File.WriteAllTextAsync(resultPath, commaSeparated);
        }

        /// <summary>
        /// Processes a list of tags from a file and generates a summarized version of the tags.
        /// </summary>
        /// <param name="outputPath">The output path where the summarized tags will be saved.</param>
        /// <param name="weightedCaptions">A boolean value indicating whether weighted captions are used.</param>
        /// <param name="file">The input file containing the tags to be processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task PostProcessTags(string outputPath, bool weightedCaptions, string file)
        {
            List<string> orderedPredictions = await GetOrderedByScoreListOfTagsAsync(file, weightedCaptions);
            string commaSeparated = _tagProcessor.GetCommaSeparatedString(orderedPredictions);

            string redundantRemoved = _tagProcessor.ApplyRedundancyRemoval(commaSeparated);

            string resultPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.txt");

            string tempFile = Path.Combine(outputPath, $"temp_{Path.GetFileName(file)}");
            File.Move(file, tempFile);

            string finalFile = tempFile.Replace("temp_", "");
            File.Move(tempFile, finalFile);

            await File.WriteAllTextAsync(resultPath, redundantRemoved);
        }

        /// <summary>
        /// Generates tags for an image file and appends the results to an existing tag file or creates a new one.
        /// </summary>
        /// <param name="outputPath">The path to the directory where the combined result will be saved.</param>
        /// <param name="weightedCaptions">A boolean value indicating whether weighted captions are used.</param>
        /// <param name="file">The input file containing the existing caption and tags to be processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task PostProcessTagsAndAppendToFile(string outputPath, bool weightedCaptions, string file)
        {
            string txtFile = Path.ChangeExtension(file, ".txt");

            if (File.Exists(txtFile))
            {
                string existingCaption = await File.ReadAllTextAsync(txtFile);

                List<string> orderedPredictions = await GetOrderedByScoreListOfTagsAsync(file, weightedCaptions);
                string commaSeparated = _tagProcessor.GetCommaSeparatedString(orderedPredictions);

                string existingPlusGenerated = $"{existingCaption.Replace("_", " ")}, {commaSeparated}";

                string redundantRemoved = _tagProcessor.ApplyRedundancyRemoval(existingPlusGenerated);

                string resultPath = Path.Combine(outputPath, $"{Path.GetFileNameWithoutExtension(file)}.txt");

                string tempFile = Path.Combine(outputPath, $"temp_{Path.GetFileName(file)}");
                File.Move(file, tempFile);

                string finalFile = tempFile.Replace("temp_", "");
                File.Move(tempFile, finalFile);

                await File.WriteAllTextAsync(resultPath, redundantRemoved);
            }
            else
            {
                await PostProcessTags(outputPath, weightedCaptions, file);
            }
        }

        /// <summary>
        /// Returns a list of tags ordered by their score for a given image path.
        /// </summary>
        /// <param name="imagePath">The file path of the image to be analyzed.</param>
        /// <returns>A list of tags ordered by their score in descending order.</returns>
        public abstract Task<List<string>> GetOrderedByScoreListOfTagsAsync(string imagePath, bool weightedCaptions = false);

        /// <summary>
        /// Retrieves predictions for the specified image file path using the prediction engine, which is a machine learning model that has been trained to make predictions. The method returns a <see cref="VBuffer{float}"/> object containing the predicted values.
        /// </summary>
        /// <param name="imagePath">The path of the image file to make predictions on.</param>
        /// <returns>A <see cref="VBuffer{float}"/> object containing the predicted values.</returns>
        public abstract Task<TOutput> GetPredictionAsync(string inputImagePath);

        /// <summary>
        /// Retrieves predictions for the specified image stream using the prediction engine, which is a machine learning model trained to make predictions. The method returns a <see cref="VBuffer{float}"/> object containing the predicted values.
        /// </summary>
        /// <param name="imageStream">The stream containing the image data to make predictions on.</param>
        /// <returns>A <see cref="VBuffer{float}"/> object containing the predicted values.</returns>
        public abstract Task<TOutput> GetPredictionAsync(Stream imageStream);

        /// <summary>
        /// Interrogates an image from a stream and returns a string representation of the predicted tags.
        /// </summary>
        /// <param name="imageStream">The stream containing the image data.</param>
        /// <returns>A string representation of the predicted tags.</returns>
        public abstract Task<string> InterrogateImageFromStream(Stream imageStream);

        /// <summary>
        /// Loads tags from a CSV file and assigns them to the '_tags' field.
        /// </summary>
        /// <param name="csvPath">The path to the CSV file containing the tags.</param>
        protected void LoadTags(string csvPath)
        {
            if (File.Exists(csvPath))
            {
                _tags = File.ReadAllLines(csvPath);
            }
        }

        public void UnloadAIModel()
        {
            UnloadModel();
        }
    }
}