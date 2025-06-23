using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

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
        public WDAutoTaggerService(IImageProcessorService imageProcessorService, ITagProcessorService tagProcessorService,
            string modelPath, string tagsPath) :
            base(imageProcessorService, tagProcessorService, modelPath, tagsPath)
        {
        }

        public override async Task<WDOutputData> GetPredictionAsync(string inputImagePath)
        {
            WDInputData inputData = await _imageProcessor.ProcessImageForTagPredictionAsync(inputImagePath);

            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns().FirstOrDefault(), inputData.Input)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
            {
                Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

                WDOutputData outputData = new WDOutputData()
                {
                    PredictionsSigmoid = tensorPrediction.ToArray()
                };
                return outputData;
            }
        }

        public override async Task<List<string>> GetOrderedByScoreListOfTagsAsync(string imagePath, bool weightedCaptions = false)
        {
            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            WDOutputData values = await GetPredictionAsync(imagePath);

            for (int i = 0; i < values.PredictionsSigmoid.Length; i++)
            {
                if (values.PredictionsSigmoid[i] > Threshold)
                {
                    predictionsDict.Add(_tags[i], values.PredictionsSigmoid[i]);
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

        public override async Task<WDOutputData> GetPredictionAsync(Stream imageStream)
        {
            WDInputData inputData = await _imageProcessor.ProcessImageForTagPredictionAsync(imageStream);
            List<NamedOnnxValue> inputValues = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor<float>(GetInputColumns().FirstOrDefault(), inputData.Input)
            };

            using (IDisposableReadOnlyCollection<DisposableNamedOnnxValue> prediction = await Task.Run(() => _session.Run(inputValues)))
            {
                Tensor<float> tensorPrediction = prediction[0].AsTensor<float>();

                WDOutputData outputData = new WDOutputData()
                {
                    PredictionsSigmoid = tensorPrediction.ToArray()
                };
                return outputData;
            }
        }

        public override async Task<string> InterrogateImageFromStream(Stream imageStream)
        {
            if (!_isModelLoaded)
            {
                await LoadModelAsync();
                _isModelLoaded = true;
            }

            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            WDOutputData values = await GetPredictionAsync(imageStream);

            for (int i = 0; i < values.PredictionsSigmoid.Length; i++)
            {
                if (values.PredictionsSigmoid[i] > _threshold)
                {
                    predictionsDict.Add(_tags[i], values.PredictionsSigmoid[i]);
                }
            }

            IOrderedEnumerable<KeyValuePair<string, float>> sortedDict = predictionsDict.OrderByDescending(x => x.Value);

            List<string> listOrdered = new List<string>();

            foreach (KeyValuePair<string, float> item in sortedDict)
            {
                listOrdered.Add(item.Key);
            }

            string commaSeparated = _tagProcessor.GetCommaSeparatedString(listOrdered);

            string redundantRemoved = _tagProcessor.ApplyRedundancyRemoval(commaSeparated);

            UnloadModel();

            return redundantRemoved;
        }
    }
}