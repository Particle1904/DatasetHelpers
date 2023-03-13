using DatasetHelpers.Models;

using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;

using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace SmartData.Lib.Services
{
    public class AutoTaggerService
    {
        private string _modelPath;

        private MLContext _mlContext;
        private OnnxScoringEstimator _pipeline;
        private ITransformer _predictionPipe;
        private PredictionEngine<InputData, OutputData> _predictionEngine;

        private string[] _tags;
        private float _tagThreshold = 0.2f;

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

        public AutoTaggerService(string modelPath, string csvPath)
        {
            _modelPath = modelPath;
            _mlContext = new MLContext();
            _predictionPipe = GetPredictionPipeline();
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<InputData, OutputData>(_predictionPipe);

            LoadTags(csvPath);
            for (int i = 0; i < _tags.Length; i++)
            {
                _tags[i] = _tags[i].Replace("_", " ");
            }
        }

        public ITransformer GetPredictionPipeline()
        {
            var inputColumns = new string[] { "input_1:0" };
            var outputColumns = new string[] { "predictions_sigmoid" };

            _pipeline = _mlContext.Transforms.ApplyOnnxModel(outputColumnNames: outputColumns, inputColumnNames: inputColumns, _modelPath);

            var emptyDv = _mlContext.Data.LoadFromEnumerable(new InputData[] { });

            return _pipeline.Fit(emptyDv);
        }

        public List<string> GetOrderedByScoreListOfTags(string imagePath)
        {
            Dictionary<string, float> predictionsDict = new Dictionary<string, float>();

            var predictions = GetPrediction(imagePath);
            var values = predictions.GetValues();

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > _tagThreshold)
                {
                    predictionsDict.Add(_tags[i], values[i]);
                }
            }

            var sortedDict = predictionsDict.OrderByDescending(x => x.Value);

            List<string> listOrdered = new List<string>();

            foreach (KeyValuePair<string, float> item in sortedDict)
            {
                listOrdered.Add(item.Key);
            }

            foreach (string item in listOrdered)
            {
                item.Replace("_", " ");
            }

            return listOrdered;
        }

        private VBuffer<float> GetPrediction(string imagePath)
        {
            Console.WriteLine($"Making predictions using a threshold of {_tagThreshold}");
            InputData inputData = new InputData();
            inputData.Input_1 = new float[448 * 448 * 3];
            int index = 0;

            using (Image<Bgr24> image = Image.Load<Bgr24>(imagePath))
            {
                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = new LanczosResampler(3),
                    Compand = true,
                    PadColor = new Bgr24(255, 255, 255),
                    Size = new Size(448, 448),
                };

                image.Mutate(image => image.Resize(resizeOptions));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Bgr24> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Bgr24 pixel = ref pixelRow[x];
                            byte temp = pixel.R;
                            pixel.R = pixel.B;
                            pixel.B = temp;

                            inputData.Input_1[index++] = pixel.R;
                            inputData.Input_1[index++] = pixel.G;
                            inputData.Input_1[index++] = pixel.B;
                        }
                    }
                });
            }

            var prediction = _predictionEngine.Predict(inputData);
            return prediction.PredictionsSigmoid;
        }

        private void LoadTags(string csvPath)
        {
            if (File.Exists(csvPath))
            {
                _tags = File.ReadAllLines(csvPath);
            }
        }

        public void LoadConfigs(Config configs)
        {
            _tagThreshold = configs.TaggerThreshold;
        }
    }
}