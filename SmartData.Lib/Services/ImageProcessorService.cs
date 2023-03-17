using SixLabors.ImageSharp.Processing.Processors.Transforms;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class ImageProcessorService : IImageProcessorService
    {
        private string _imageSearchPattern = "*.jpg,*.jpeg,*.png,*.gif,*.webp,";

        private const ushort _divisor = 64;
        private int _baseResolution = 512;
        private int _lanczosSamplerRadius = 3;

        public int BlocksPerRow { get; private set; }
        private int _totalBlocks;
        Dictionary<double, int> _aspectRatioToBlocks;

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
        /// <param name="dimension">The target dimensions to resize the images to. Defaults to 512x512 resolution.</param>
        /// <remarks>
        /// This method uses multiple threads to resize the images in parallel. Each image is resized to a target aspect ratio based on a predetermined set of aspect ratio buckets. The resized images are saved as PNG files in the output directory.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension = SupportedDimensions.Resolution512x512)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            var tasks = new List<Task>();

            foreach (var file in files)
            {
                tasks.Add(ResizeImageAsync(outputPath, file, dimension));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Resizes all images in a given input directory and saves the resized images to an output directory.
        /// </summary>
        /// <param name="inputPath">The path to the directory containing the input images.</param>
        /// <param name="outputPath">The path to the directory where the resized images will be saved.</param>
        /// <param name="progress">An object used to report progress during the image resizing process.</param>
        /// <param name="dimension">The target image dimension to resize the images to.</param>
        /// <remarks>
        /// This method uses multiple threads to resize the images in parallel. Each image is resized to a target aspect ratio based on a predetermined set of aspect ratio buckets. The resized images are saved as PNG files in the output directory.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Thrown when either the inputPath, outputPath, or progress parameter is null.</exception>
        public async Task ResizeImagesAsync(string inputPath, string outputPath, Progress progress, SupportedDimensions dimension = SupportedDimensions.Resolution512x512)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            progress.TotalFiles = files.Length;

            var tasks = new List<Task>();
            foreach (var file in files)
            {
                tasks.Add(ResizeImageAsync(outputPath, file, dimension)
                     .ContinueWith(task =>
                     {
                         progress.UpdateProgress();
                     }));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Asynchronously processes an image for tag prediction by resizing it to 448x448 and converting it to a float array.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <returns>An <see cref="InputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by inputPath does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by inputPath.</exception>
        public async Task<InputData> ProcessImageForTagPrediction(string inputPath)
        {
            InputData inputData = new InputData();
            inputData.Input_1 = new float[448 * 448 * 3];

            int index = 0;
            using (Image<Bgr24> image = await Image.LoadAsync<Bgr24>(inputPath))
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
            return inputData;
        }

        /// <summary>
        /// Resizes an image to a target aspect ratio and saves it as a PNG file in the output directory.
        /// </summary>
        /// <param name="outputPath">The full path of the directory to save the resized image file in.</param>
        /// <param name="inpuPath">The full path of the image file to resize.</param>
        /// <param name="dimension">The maximum dimension (width or height) of the resized image. Defaults to 512.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of resizing and saving the image file.</returns>
        /// <remarks>
        /// <para>
        /// This method resizes the input image file to a target aspect ratio based on a predetermined set of aspect ratio buckets. The target aspect ratio is calculated based on the number of blocks assigned to each aspect ratio bucket.
        /// </para>
        /// <para>
        /// The resized image is saved as a PNG file in the output directory with the same name as the original file, but with the extension changed to ".png".
        /// </para>
        /// <para>
        /// The maximum dimension (width or height) of the resized image can be specified using the optional `dimension` parameter. The default value is 512.
        /// </para>
        /// </remarks>
        private async Task ResizeImageAsync(string outputPath, string inpuPath, SupportedDimensions dimension = SupportedDimensions.Resolution512x512)
        {
            string fileName = Path.GetFileName(inpuPath);

            using (Image image = await Image.LoadAsync(inpuPath))
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

                if (image.Width < (int)dimension && image.Height < (int)dimension)
                {
                    if (image.Width > image.Height)
                    {
                        targetWidth = (int)dimension;
                        targetHeight = (int)Math.Round(targetWidth / aspectRatio);
                    }
                    else
                    {
                        targetHeight = (int)dimension;
                        targetWidth = (int)Math.Round(targetHeight * aspectRatio);
                    }
                }
                else
                {
                    if (image.Width > image.Height)
                    {
                        targetWidth = Math.Min(image.Width, (int)dimension);
                        targetHeight = (int)Math.Round(targetWidth / aspectRatio);
                    }
                    else
                    {
                        targetHeight = Math.Min(image.Height, (int)dimension);
                        targetWidth = (int)Math.Round(targetHeight * aspectRatio);
                    }
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

                image.Mutate(image =>
                {
                    image.BackgroundColor(Color.White);
                    image.Resize(resizeOptions);
                });
                await image.SaveAsPngAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".png"));
            }
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
