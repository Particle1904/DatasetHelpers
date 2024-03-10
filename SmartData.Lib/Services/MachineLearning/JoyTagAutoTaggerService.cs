using Microsoft.ML.Data;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services.MachineLearning
{
    /// <summary>
    /// Service for generating tags for image files using a machine learning model and managing tag-related operations.
    /// </summary>
    public class JoyTagAutoTaggerService : BaseAutoTaggerService<JoyTagInputData, JoyTagOutputData>, INotifyProgress
    {
        /// <summary>
        /// Initializes a new instance of the JoyTagAutoTaggerService class.
        /// </summary>
        /// <param name="imageProcessorService">The service responsible for image processing.</param>
        /// <param name="tagProcessorService">The service responsible for processing tags.</param>
        /// <param name="modelPath">The path to the machine learning model.</param>
        /// <param name="tagsPath">The path to the directory where tag files are stored.</param>
        public JoyTagAutoTaggerService(IImageProcessorService imageProcessorService, ITagProcessorService tagProcessorService, string modelPath, string tagsPath) :
            base(imageProcessorService, tagProcessorService, modelPath, tagsPath)
        {
        }

        public override async Task<VBuffer<float>> GetPredictionAsync(string inputImagePath)
        {
            JoyTagInputData inputData = await _imageProcessorService.ProcessImageForJoyTagPredictionAsync(inputImagePath);

            JoyTagOutputData prediction = await Task.Run(() => _predictionEngine?.Predict(inputData));
            return prediction.PredictionsSigmoid;
        }

        public override async Task<List<string>> GetOrderedByScoreListOfTagsAsync(string imagePath, bool weightedCaptions = false)
        {
            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            VBuffer<float> predictions = await GetPredictionAsync(imagePath).ConfigureAwait(false);
            float[] values = predictions.GetValues().ToArray();

            // Normalize values by applying Sigmoid function
            float[] normalizedValues = new float[values.Length];
            for (int i = 0; i < normalizedValues.Length; i++)
            {
                normalizedValues[i] = Utilities.Sigmoid(values[i]);
            }

            for (int i = 0; i < normalizedValues.Length; i++)
            {
                if (normalizedValues[i] > Threshold)
                {
                    predictionsDict.Add(_tags[i], normalizedValues[i]);
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

        public override Task<VBuffer<float>> GetPredictionAsync(Stream imageStream)
        {
            throw new NotSupportedException("Tag predictions using Streams currently not supported by JoyTag!");
        }

        public override Task<string> InterrogateImageFromStream(Stream imageStream)
        {
            throw new NotSupportedException("Tag predictions using Streams currently not supported by JoyTag!");
        }
    }
}