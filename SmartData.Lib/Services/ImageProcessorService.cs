using SixLabors.ImageSharp.Processing.Processors.Transforms;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class ImageProcessorService : IImageProcessorService
    {
        private const ushort _divisor = 64;
        private int _baseResolution = 512;
        private int _lanczosSamplerRadius = 3;

        public int BlocksPerRow { get; private set; }
        private int _totalBlocks;
        Dictionary<double, int> _aspectRatioToBlocks;

        private int _processedImages;

        public ImageProcessorService()
        {
            int BlocksPerRow = _baseResolution / _divisor;
            _totalBlocks = BlocksPerRow * BlocksPerRow;
            _aspectRatioToBlocks = CalculateBuckets(_totalBlocks);
        }

        /// <summary>
        /// Resizes all images in a given input directory and saves the resized images to an output directory.
        /// </summary>
        /// <param name="inputPath">The path to the directory containing the input images.</param>
        /// <param name="outputPath">The path to the directory where the resized images will be saved.</param>
        /// <remarks>
        /// This method uses multiple threads to resize the images in parallel. Each image is resized to a target aspect ratio based on a predetermined set of aspect ratio buckets. The resized images are saved as PNG files in the output directory.
        /// </remarks>
        public void ResizeImages(string inputPath, string outputPath)
        {
            string[] files = Directory.GetFiles(inputPath);

            CountdownEvent countdown = new CountdownEvent(files.Length);

            foreach (var file in files)
            {
                ResizeParams parameters = new ResizeParams()
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    FilePath = file,
                    CountdownEvent = countdown
                };

                ThreadPool.QueueUserWorkItem(ResizeImage, parameters);
            }

            countdown.Wait();
        }

        /// <summary>
        /// Resizes a single image to a target aspect ratio and saves it as a PNG file.
        /// </summary>
        /// <param name="state">The state object passed to the thread pool.</param>
        /// <remarks>
        /// This method resizes an image to a target aspect ratio based on a predetermined set of aspect ratio buckets. The target aspect ratio is calculated based on the number of blocks assigned to each aspect ratio bucket. The resized image is saved as a PNG file in the output directory.
        /// </remarks>
        private void ResizeImage(object state)
        {
            ResizeParams parameters = (ResizeParams)state;

            string file = parameters.FilePath;
            string fileName = Path.GetFileName(parameters.FilePath);
            string inputPath = parameters.InputPath;
            string outputPath = parameters.OutputPath;

            using (Image image = Image.Load(file))
            {
                double aspectRatio = Math.Round(image.Width / (double)image.Height, 2);

                int bucket = FindAspectRatioBucket(aspectRatio);

                int blocks = _aspectRatioToBlocks.Values.Sum();

                double bucketFraction = 0.0;
                if (_aspectRatioToBlocks.ContainsKey(bucket))
                {
                    bucketFraction = _aspectRatioToBlocks[bucket] / (double)blocks;
                }
                else
                {
                    bucketFraction = 1.0 / (double)blocks;
                }

                double targetAspectRatio = Math.Sqrt(bucketFraction);

                int targetWidth;
                int targetHeight;

                if (image.Width > image.Height)
                {
                    targetWidth = Math.Min(image.Width, 512);
                    targetHeight = (int)Math.Round(targetWidth / aspectRatio);
                }
                else
                {
                    targetHeight = Math.Min(image.Height, 512);
                    targetWidth = (int)Math.Round(targetHeight * aspectRatio);
                }

                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = new LanczosResampler(_lanczosSamplerRadius),
                    Compand = true,
                    PadColor = Color.White,
                    Size = new Size(targetWidth, targetHeight)
                };

                image.Mutate(image => image.Resize(resizeOptions));
                image.SaveAsPng(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".png"));
            }

            parameters.CountdownEvent.Signal();
        }

        /// <summary>
        /// Calculates the number of blocks assigned to each aspect ratio bucket.
        /// </summary>
        /// <param name="totalBlocks">The total number of blocks to be assigned across all aspect ratio buckets.</param>
        /// <returns>A dictionary containing the number of blocks assigned to each aspect ratio bucket.</returns>
        /// <remarks>
        /// This method calculates the number of blocks assigned to each aspect ratio bucket based on a predetermined set of aspect ratios. The number of blocks assigned to each bucket is proportional to the area of images with that aspect ratio in the input directory.
        /// </remarks>
        private Dictionary<double, int> CalculateBuckets(int totalBlocks)
        {
            Dictionary<double, int> aspectRatioToBlocks = new Dictionary<double, int>();

            aspectRatioToBlocks[1.0f] = totalBlocks;
            aspectRatioToBlocks[4.0f / 3.0f] = CalculateBlocksForAspectRatio(totalBlocks, 4.0f, 3.0f);
            aspectRatioToBlocks[3.0f / 2.0f] = CalculateBlocksForAspectRatio(totalBlocks, 3.0f, 2.0f);
            aspectRatioToBlocks[1.0f / 1.5f] = CalculateBlocksForAspectRatio(totalBlocks, 1.0f, 1.5f);
            aspectRatioToBlocks[1.0f / 1.85f] = CalculateBlocksForAspectRatio(totalBlocks, 1.0f, 1.85f);
            aspectRatioToBlocks[2.39f / 1.0f] = CalculateBlocksForAspectRatio(totalBlocks, 2.39f, 1.0f);
            aspectRatioToBlocks[4.0f / 5.0f] = CalculateBlocksForAspectRatio(totalBlocks, 4.0f, 5.0f);
            aspectRatioToBlocks[3.0f / 4.0f] = CalculateBlocksForAspectRatio(totalBlocks, 3.0f, 4.0f);
            aspectRatioToBlocks[2.0f / 3.0f] = CalculateBlocksForAspectRatio(totalBlocks, 2.0f, 3.0f);

            return aspectRatioToBlocks;
        }

        /// <summary>
        /// Calculates the number of blocks required to represent an image with a target aspect ratio.
        /// </summary>
        /// <param name="totalBlocks">The total number of blocks to be assigned across all aspect ratio buckets.</param>
        /// <param name="width">The width of the target aspect ratio.</param>
        /// <param name="height">The height of the target aspect ratio.</param>
        /// <returns>The number of blocks required to represent an image with the target aspect ratio.</returns>
        /// <remarks>
        /// This method calculates the number of blocks required to represent an image with the target aspect ratio. The target aspect ratio is used to calculate the target width and height of the image, and the number of blocks required to represent the image is calculated based on the target width and height.
        /// </remarks>
        private int CalculateBlocksForAspectRatio(int totalBlocks, float width, float height)
        {
            double aspectRatio = width / (double)height;
            double targetArea = totalBlocks * _baseResolution * _baseResolution;

            int targetWidth = (int)Math.Min(_baseResolution, Math.Sqrt(targetArea * aspectRatio));
            int targetHeight = (int)Math.Min(_baseResolution, Math.Sqrt(targetArea / aspectRatio));

            int blocks = (int)Math.Ceiling(targetWidth * targetHeight / (double)_baseResolution);
            return blocks;
        }

        /// <summary>
        /// Finds the aspect ratio bucket for the given aspect ratio.
        /// </summary>
        /// <param name="aspectRatio">The aspect ratio to find the bucket for.</param>
        /// <returns>The bucket number that corresponds to the given aspect ratio. If no bucket is found, returns the total number of blocks.</returns>
        private int FindAspectRatioBucket(double aspectRatio)
        {
            foreach (var aspectRatioRange in _aspectRatioToBlocks.Keys.OrderByDescending(k => k))
            {
                if (aspectRatio >= aspectRatioRange)
                {
                    return _aspectRatioToBlocks[aspectRatioRange];
                }
            }

            return _totalBlocks;
        }
    }
}
