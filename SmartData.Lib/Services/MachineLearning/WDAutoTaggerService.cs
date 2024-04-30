using Microsoft.ML.Data;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning
{
    /// <summary>
    /// Service for generating tags for image files using a machine learning model and managing tag-related operations.
    /// </summary>
    public class WDAutoTaggerService : BaseAutoTaggerService<WDInputData, WDOutputData>, INotifyProgress
    {
        /// <summary>
        /// Initializes a new instance of the WDAutoTaggerService class.
        /// </summary>
        /// <param name="imageProcessorService">The service responsible for image processing.</param>
        /// <param name="tagProcessorService">The service responsible for processing tags.</param>
        /// <param name="modelPath">The path to the machine learning model.</param>
        /// <param name="tagsPath">The path to the directory where tag files are stored.</param>
        public WDAutoTaggerService(IImageProcessorService imageProcessorService, ITagProcessorService tagProcessorService, string modelPath, string tagsPath) :
            base(imageProcessorService, tagProcessorService, modelPath, tagsPath)
        {
        }

        public override async Task<VBuffer<float>> GetPredictionAsync(string inputImagePath)
        {
            WDInputData inputData = await _imageProcessor.ProcessImageForTagPredictionAsync(inputImagePath);

            WDOutputData prediction = await Task.Run(() => _predictionEngine?.Predict(inputData));
            return prediction.PredictionsSigmoid;
        }

        public override async Task<List<string>> GetOrderedByScoreListOfTagsAsync(string imagePath, bool weightedCaptions = false)
        {
            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            VBuffer<float> predictions = await GetPredictionAsync(imagePath).ConfigureAwait(false);
            float[] values = predictions.GetValues().ToArray();

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > Threshold)
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

            return listOrdered;
        }

        public override async Task<VBuffer<float>> GetPredictionAsync(Stream imageStream)
        {
            WDInputData inputData = await _imageProcessor.ProcessImageForTagPredictionAsync(imageStream);

            WDOutputData prediction = await Task.Run(() => _predictionEngine?.Predict(inputData));
            return prediction.PredictionsSigmoid;
        }
    }
}