using BERTTokenizers;

using Microsoft.ML.Data;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

using System.Reflection;

namespace SmartData.Lib.Services
{
    public class AutoCaptionService : BaseAIConsumer<BLIPInputData, BLIPOutputData>, IAutoCaptionService
    {
        private const int _sequenceLength = 512;
        private BertUnasedCustomVocabulary _tokenizer;

        public AutoCaptionService(IImageProcessorService imageProcessorService, string modelPath) : base(imageProcessorService, modelPath)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _tokenizer = new BertUnasedCustomVocabulary(Path.Combine(assemblyPath, "Vocabularies/base_uncased.txt"));
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "input_ids", "pixel_values" };
        }
        protected override string[] GetOutputColumns()
        {
            return new string[] { "output" };
        }

        /// <summary>
        /// Generates captions for the image files in the specified input folder, writes the results to caption files in the specified output folder,
        /// and updates the progress object with the status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the caption files will be written.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateCaptions(string inputPath, string outputPath)
        {
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            foreach (string file in files)
            {
                var prediction = await GetPredictionAsync(file);
            }
        }

        /// <summary>
        /// Generates captions for the image files in the specified input folder, writes the results to caption files in the specified output folder,
        /// and updates the progress object with the status of the operation.
        /// </summary>
        /// <param name="inputPath">The path to the input folder containing image files.</param>
        /// <param name="outputPath">The path to the output folder where the caption files will be written.</param>
        /// <param name="progress">The progress object to update with the status of the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task GenerateCaptions(string inputPath, string outputPath, Progress progress)
        {
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);
            progress.TotalFiles = files.Length;
            foreach (string file in files)
            {
                float[] prediction = await GetPredictionAsync(file);
                progress.UpdateProgress();
            }
        }

        /// <summary>
        /// Retrieves predictions for the specified image file path using the prediction engine, which is a machine learning model that has been trained to make predictions. The method returns a <see cref="VBuffer{float}"/> object containing the predicted values.
        /// </summary>
        /// <param name="imagePath">The path of the image file to make predictions on.</param>
        /// <returns>A <see cref="VBuffer{float}"/> object containing the predicted values.</returns>
        private async Task<float[]> GetPredictionAsync(string inputImagePath)
        {
            BLIPInputData inputData = await _imageProcessorService.ProcessImageForCaptionPredictionAsync(inputImagePath);

            string[] inputText = new string[] { "a photograph of a" };
            var tokenized = _tokenizer.Encode(_sequenceLength, inputText);
            var inputs = tokenized.Select(x => x.InputIds).Where(x => x != 0).ToArray();

            inputData.InputIds = new long[1, inputs.Length];

            for (int i = 0; i < 1; i++)
            {
                for (int j = 0; j < inputs.Length; j++)
                {
                    inputData.InputIds[i, j] = inputs[j];
                }
            }

            BLIPOutputData prediction = await Task.Run(() => _predictionEngine.Predict(inputData));
            return prediction.Output;
        }
    }
}