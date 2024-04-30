using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Services.Base;

using System.Diagnostics;

namespace SmartData.Lib.Services.MachineLearning
{
    public class ContentAwareCropService : BaseAIConsumer<Yolov4InputData, Yolov4OutputData>, IContentAwareCropService, INotifyProgress
    {

        private float _scoreThreshold = 0.5f;
        /// <summary>
        /// Gets or sets the threshold value for this object. The threshold value determines the cutoff point for certain calculations.
        /// </summary>
        /// <value>
        /// A <see cref="float"/> value between 0.0 and 1.0, inclusive.
        /// </value>
        /// <remarks>
        /// The <see cref="ScoreThreshold"/> value must be a floating-point value between 0.0 and 1.0. Values outside this range will be clamped to the nearest valid value. 
        /// </remarks>
        public float ScoreThreshold
        {
            get => _scoreThreshold;
            set
            {
                _scoreThreshold = Math.Clamp(value, 0.0f, 1.0f);
            }
        }

        private float _iouThreshold = 0.35f;
        /// <summary>
        /// Gets or sets the threshold value for this object. The threshold value determines the cutoff point for certain calculations.
        /// </summary>
        /// <value>
        /// A <see cref="float"/> value between 0.0 and 1.0, inclusive.
        /// </value>
        /// <remarks>
        /// The <see cref="ScoreThreshold"/> value must be a floating-point value between 0.0 and 1.0. Values outside this range will be clamped to the nearest valid value. 
        /// </remarks>
        public float IouThreshold
        {
            get => _iouThreshold;
            set
            {
                _iouThreshold = Math.Clamp(value, 0.0f, 1.0f);
            }
        }

        private float _expansionPercentage = 0.1f;
        /// <summary>
        /// Gets or sets the expansion percentage value for this object. The threshold value determines how much the Bounding Box should be expanded (in %).
        /// </summary>
        /// <value>
        /// A <see cref="float"/> value between 0.1 and 1.0, inclusive.
        /// </value>
        /// <remarks>
        /// The <see cref="ScoreThreshold"/> value must be a floating-point value between 0.1 and 1.0. Values outside this range will be clamped to the nearest valid value. 
        /// </remarks>
        public float ExpansionPercentage
        {
            get => _expansionPercentage;
            set
            {
                _expansionPercentage = Math.Clamp(value, 1.0f, 2.0f);
            }
        }

        private readonly string[] _classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane",
                "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench",
                "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella",
                "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat",
                "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife",
                "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut",
                "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse",
                "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock",
                "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        private readonly int[] _shapes = new int[] { 52, 26, 13 };

        private readonly float[][][] _anchors = new float[][][]
            {
                new float[][] { new float[] { 12, 16 }, new float[] { 19, 36 }, new float[] { 40, 28 } },
                new float[][] { new float[] { 36, 75 }, new float[] { 76, 55 }, new float[] { 72, 146 } },
                new float[][] { new float[] { 142, 110 }, new float[] { 192, 243 }, new float[] { 459, 401 } }
            };

        private readonly float[] _xyScale = new float[] { 1.2f, 1.1f, 1.05f };
        private readonly float[] _strides = new float[] { 8, 16, 32 };

        private int _lanczosRadius = 3;
        public int LanczosRadius
        {
            get => _lanczosRadius;
            set
            {
                _lanczosRadius = Math.Clamp(value, 1, 25);
            }
        }

        public bool ApplySharpen { get; set; }

        private double _sharpenSigma;
        public double SharpenSigma
        {
            get => _sharpenSigma;
            set
            {
                if (Math.Round(value, 2) != _sharpenSigma)
                {
                    _sharpenSigma = Math.Clamp(Math.Round(value, 2), 0.5d, 5d);
                }
            }
        }

        private int _minimumResolutionForSigma;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public int MinimumResolutionForSigma
        {
            get => _minimumResolutionForSigma;
            set
            {
                _minimumResolutionForSigma = Math.Clamp(value, 256, ushort.MaxValue);
            }
        }

        public ContentAwareCropService(IImageProcessorService imageProcessorService, string modelPath) : base(imageProcessorService, modelPath)
        {
        }

        protected override string[] GetInputColumns()
        {
            return new string[] { "input_1:0" };
        }
        protected override string[] GetOutputColumns()
        {
            return new string[] { "Identity:0", "Identity_1:0", "Identity_2:0" };
        }

        /// <summary>
        /// Processes cropped images by performing the following steps:
        /// - Loads the model if it's not already loaded.
        /// - Retrieves files from the specified input path that match the image search pattern.
        /// - Updates the progress with the total number of files.
        /// - For each file, retrieves the bounding box predictions using YOLOv4.
        /// - Retrieves the size of the image.
        /// - Obtains the person bounding box results based on the predictions and image size.
        /// - Constructs the result path for the cropped image.
        /// - Crops the image and saves it using the image processor service.
        /// - raise events to signal the progress status of the operation.
        /// </summary>
        /// <param name="inputPath">The input path containing the images to process.</param>
        /// <param name="outputPath">The output path to store the cropped images.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ProcessCroppedImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension)
        {
            if (!_isModelLoaded)
            {
                await LoadModel();
            }

            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);
            List<string> filesList = new List<string>();
            try
            {
                filesList = files.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
            }
            catch (Exception)
            {
                filesList = files.ToList();
            }
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            _imageProcessorService.LanczosSamplerRadius = LanczosRadius;
            _imageProcessorService.ApplySharpen = ApplySharpen;
            _imageProcessorService.SharpenSigma = (float)SharpenSigma;
            _imageProcessorService.MinimumResolutionForSigma = MinimumResolutionForSigma;
            foreach (string file in filesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ProcessingRoutine(outputPath, dimension, file);
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Performs the processing routine for a given file, including bounding box prediction, image size retrieval, cropping, and saving the result to the specified output path.
        /// </summary>
        /// <param name="outputPath">The path where the processed image will be saved.</param>
        /// <param name="dimension">The supported dimensions for cropping the image.</param>
        /// <param name="file">The input file to be processed.</param>
        /// <returns>A task representing the asynchronous processing operation.</returns>
        private async Task ProcessingRoutine(string outputPath, SupportedDimensions dimension, string file)
        {
            Yolov4OutputData boundingBoxPrediction = await GetPredictionAsync(file);
            System.Drawing.Size imageSize = await _imageProcessorService.GetImageSizeAsync(file);
            var results = GetPersonBoundingBox(boundingBoxPrediction, imageSize.Width, imageSize.Height);
            await _imageProcessorService.CropImageAsync(file, outputPath, results, _expansionPercentage, dimension);
        }

        /// <summary>
        /// Gets the bounding box prediction for an input image using YOLOv4.
        /// </summary>
        /// <param name="inputImagePath">The path to the input image.</param>
        /// <returns>A task representing the asynchronous operation that yields the YOLOv4 bounding box prediction.</returns>
        private async Task<Yolov4OutputData> GetPredictionAsync(string inputImagePath)
        {
            Yolov4InputData inputData = await _imageProcessorService.ProcessImageForBoundingBoxPredictionAsync(inputImagePath);

            Yolov4OutputData prediction = await Task.Run(() => _predictionEngine.Predict(inputData));
            return prediction;
        }

        /// <summary>
        /// Retrieves the bounding boxes for detected persons based on the YOLOv4 predictions.
        /// </summary>
        /// <param name="predictions">The YOLOv4 output predictions.</param>
        /// <param name="imageWidth">The width of the image.</param>
        /// <param name="imageHeight">The height of the image.</param>
        /// <returns>A list of detected persons with their corresponding bounding boxes.</returns>
        private List<DetectedPerson> GetPersonBoundingBox(Yolov4OutputData predictions, int imageWidth, int imageHeight)
        {
            List<float[]> detectedObjects = new List<float[]>();
            int classesCount = _classesNames.Length;

            float[][] predictionResults = new[] { predictions.Identity0, predictions.Identity1, predictions.Identity2 };

            for (int i = 0; i < predictionResults.Length; i++)
            {
                var prediction = predictionResults[i];
                var outputSize = _shapes[i];

                for (int boxY = 0; boxY < outputSize; boxY++)
                {
                    for (int boxX = 0; boxX < outputSize; boxX++)
                    {
                        for (int a = 0; a < _anchors.Length; a++)
                        {
                            var offset = boxY * outputSize * (classesCount + 5) * _anchors.Length + boxX * (classesCount + 5) * _anchors.Length + a * (classesCount + 5);
                            var predictionBoundingBox = prediction.Skip(offset).Take(classesCount + 5).ToArray();

                            var boundingBoxXYWH = predictionBoundingBox.Take(4).ToArray();
                            var predictionConfidence = predictionBoundingBox[4];
                            var predictionProbabilities = predictionBoundingBox.Skip(5).ToArray();

                            var deltaX = boundingBoxXYWH[0];
                            var deltaY = boundingBoxXYWH[1];
                            var deltaWidth = boundingBoxXYWH[2];
                            var deltaHeight = boundingBoxXYWH[3];

                            float centerX = (Utilities.Sigmoid(deltaX) * _xyScale[i] - 0.5f * (_xyScale[i] - 1) + boxX) * _strides[i];
                            float centerY = (Utilities.Sigmoid(deltaY) * _xyScale[i] - 0.5f * (_xyScale[i] - 1) + boxY) * _strides[i];
                            float width = (float)Math.Exp(deltaWidth) * _anchors[i][a][0];
                            float height = (float)Math.Exp(deltaHeight) * _anchors[i][a][1];

                            float x1 = centerX - width * 0.5f;
                            float y1 = centerY - height * 0.5f;
                            float x2 = centerX + width * 0.5f;
                            float y2 = centerY + height * 0.5f;

                            float inputSize = 416f;
                            float resizeRatio = Math.Min(inputSize / imageWidth, inputSize / imageHeight);
                            float widthOffset = (inputSize - resizeRatio * imageWidth) / 2f;
                            float heightOffset = (inputSize - resizeRatio * imageHeight) / 2f;

                            var originalX1 = 1f * (x1 - widthOffset) / resizeRatio; // left
                            var originalX2 = 1f * (x2 - widthOffset) / resizeRatio; // right
                            var originalY1 = 1f * (y1 - heightOffset) / resizeRatio; // top
                            var originalY2 = 1f * (y2 - heightOffset) / resizeRatio; // bottom

                            originalX1 = Math.Max(originalX1, 0);
                            originalY1 = Math.Max(originalY1, 0);
                            originalX2 = Math.Min(originalX2, imageWidth - 1);
                            originalY2 = Math.Min(originalY2, imageHeight - 1);
                            if (originalX1 > originalX2 || originalY1 > originalY2)
                            {
                                continue;
                            }

                            var predictionScores = predictionProbabilities.Select(p => p * predictionConfidence).ToList();

                            float maxScore = predictionScores.Max();
                            if (maxScore > _scoreThreshold)
                            {
                                detectedObjects.Add(new float[] { originalX1, originalY1, originalX2, originalY2, maxScore, predictionScores.IndexOf(maxScore) });
                            }
                        }
                    }
                }
            }

            List<float[]> detectedPersonObjects = detectedObjects.Where(x => x[5] == 0)
                .OrderByDescending(x => x[4]).ToList();

            List<DetectedPerson> detectedPeople = ApplyNonMaximumSuppresion(detectedPersonObjects);
            return detectedPeople;
        }

        /// <summary>
        /// Applies the non-maximum suppression algorithm to a list of detected objects to filter out overlapping bounding boxes.
        /// </summary>
        /// <param name="detectedObjects">The list of detected objects with their bounding boxes and confidence scores.</param>
        /// <returns>A list of non-overlapping detected persons after applying non-maximum suppression.</returns>
        private List<DetectedPerson> ApplyNonMaximumSuppresion(List<float[]> detectedObjects)
        {
            List<DetectedPerson> results = new List<DetectedPerson>();

            for (int i = 0; i < detectedObjects.Count; i++)
            {
                var boundingBox1 = detectedObjects[i];
                if (boundingBox1 == null)
                {
                    continue;
                }

                var confidence = boundingBox1[4];

                results.Add(new DetectedPerson(boundingBox1.Take(4).ToArray(), confidence));

                var iou = detectedObjects.Select(boundingBox2 => boundingBox2 == null ? float.NaN : CalculateIoU(boundingBox1, boundingBox2)).ToList();
                for (int j = 0; j < iou.Count; j++)
                {
                    if (!float.IsNaN(iou[j]) && iou[j] > _iouThreshold)
                    {
                        detectedObjects[j] = null;
                    }
                }
            }
            detectedObjects.RemoveAll(x => x == null);

            return results;
        }

        /// <summary>
        /// Calculates the intersection over union (IoU) between two bounding boxes.
        /// </summary>
        /// <param name="boundingBox1">The coordinates of the first bounding box: [x1, y1, x2, y2].</param>
        /// <param name="boundingBox2">The coordinates of the second bounding box: [x1, y1, x2, y2].</param>
        /// <returns>The IoU value between the two bounding boxes.</returns>
        private static float CalculateIoU(float[] boundingBox1, float[] boundingBox2)
        {
            var area1 = CalculateBoundingBoxArea(boundingBox1);
            var area2 = CalculateBoundingBoxArea(boundingBox2);

            Debug.Assert(area1 >= 0);
            Debug.Assert(area2 >= 0);

            var intersectionWidth = Math.Max(0, Math.Min(boundingBox1[2], boundingBox2[2]) - Math.Max(boundingBox1[0], boundingBox2[0]));
            var intersectionHeight = Math.Max(0, Math.Min(boundingBox1[3], boundingBox2[3]) - Math.Max(boundingBox1[1], boundingBox2[1]));
            var intersectionArea = intersectionWidth * intersectionHeight;

            return intersectionArea / (area1 + area2 - intersectionArea);
        }

        /// <summary>
        /// Calculates the area of a bounding box.
        /// </summary>
        /// <param name="boundingBox">The coordinates of the bounding box: [x1, y1, x2, y2].</param>
        /// <returns>The area of the bounding box.</returns>
        private static float CalculateBoundingBoxArea(float[] boundinbBox)
        {
            return (boundinbBox[2] - boundinbBox[0]) * (boundinbBox[3] - boundinbBox[1]);
        }
    }
}
