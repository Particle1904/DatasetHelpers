using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class ImageProcessorService : IImageProcessorService
    {
        private readonly string _imageSearchPattern = "*.jpg,*.jpeg,*.png,*.gif,*.webp,";

        private LanczosResampler _lanczosResampler;

        private const ushort _semaphoreConcurrent = 6;

        private const float _blurRadius = 22f;

        private const ushort _divisor = 64;
        private readonly int _baseResolution = 512;

        private int _lanczosSamplerRadius = 3;
        public int LanczosSamplerRadius
        {
            get => _lanczosSamplerRadius;
            set
            {
                _lanczosSamplerRadius = Math.Clamp(value, 1, 25);
                if (_lanczosSamplerRadius != _lanczosResampler.Radius)
                {
                    _lanczosResampler = new LanczosResampler(_lanczosSamplerRadius);
                }
            }
        }
        private float _sharpenSigma = 1.0f;
        public float SharpenSigma
        {
            get => _sharpenSigma;
            set
            {
                _sharpenSigma = Math.Clamp(value, 0.5f, 5.0f);
            }
        }

        private int _minimumResolutionForSigma;
        public int MinimumResolutionForSigma
        {
            get => _minimumResolutionForSigma;
            set
            {
                _minimumResolutionForSigma = Math.Clamp(value, 256, ushort.MaxValue);
            }
        }

        public bool ApplySharpen { get; set; } = false;

        public int BlocksPerRow { get; private set; }
        private readonly int _totalBlocks;
        private readonly Dictionary<double, int> _aspectRatioToBlocks;

        public ImageProcessorService()
        {
            BlocksPerRow = _baseResolution / _divisor;
            _totalBlocks = BlocksPerRow * BlocksPerRow;
            _aspectRatioToBlocks = CalculateBuckets(_totalBlocks);
            _lanczosResampler = new LanczosResampler(_lanczosSamplerRadius);
            MinimumResolutionForSigma = 512;
        }

        /// <summary>
        /// Asynchronously retrieves the size of an image file.
        /// </summary>
        /// <param name="filePath">The path to the image file.</param>
        /// <returns>A <see cref="System.Drawing.Size"/> representing the width and height of the image.</returns>
        public async Task<System.Drawing.Size> GetImageSizeAsync(string filePath)
        {
            System.Drawing.Size size = new System.Drawing.Size();
            using (Image image = await Image.LoadAsync(filePath))
            {
                size.Width = image.Width;
                size.Height = image.Height;
            }

            return size;
        }

        /// <summary>
        /// Crops and resizes an image based on the bounding box of a detected person, and saves the resulting image to the specified output path.
        /// If no person is detected in the image, the original image is copied to the output path.
        /// </summary>
        /// <param name="inputPath">The path of the input image.</param>
        /// <param name="outputPath">The output path where the cropped and resized image will be saved.</param>
        /// <param name="results">The list of detected persons containing the bounding box information.</param>
        /// <param name="expansionPercentage">The scale factor to apply to the bounding box to include more context in the cropped image.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CropImageAsync(string inputPath, string outputPath, List<DetectedPerson> results, float expansionPercentage, SupportedDimensions dimension)
        {
            if (results == null || results.Count == 0)
            {
                await ResizeImageAsync(inputPath, outputPath, dimension);
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            using (Image<Rgb24> image = await Image.LoadAsync<Rgb24>(inputPath))
            {
                DetectedPerson? detectedPerson = results.FirstOrDefault();
                float[] boundingBox = detectedPerson!.BoundingBox;

                int targetWidth = (int)dimension;
                int targetHeight = (int)dimension;

                int centerX = (int)((boundingBox[0] + boundingBox[2]) / 2);
                int centerY = (int)((boundingBox[1] + boundingBox[3]) / 2);
                int width = (int)(boundingBox[2] - boundingBox[0]);
                int height = (int)(boundingBox[3] - boundingBox[1]);

                int scaledWidth = (int)(width * expansionPercentage);
                int scaledHeight = (int)(height * expansionPercentage);

                int scaledX1 = Math.Max(0, centerX - scaledWidth / 2);
                int scaledY1 = Math.Max(0, centerY - scaledHeight / 2);
                int scaledX2 = Math.Min(image.Width, centerX + scaledWidth / 2);
                int scaledY2 = Math.Min(image.Height, centerY + scaledHeight / 2);

                int cropWidth = scaledX2 - scaledX1;
                int cropHeight = scaledY2 - scaledY1;

                double originalAspectRatio = width / (double)height;
                double targetAspectRatio = targetWidth / (double)targetHeight;

                if (originalAspectRatio < targetAspectRatio)
                {
                    cropHeight = (int)(cropWidth / originalAspectRatio);
                }
                else
                {
                    cropWidth = (int)(cropHeight * originalAspectRatio);
                }

                int cropX = Math.Max(0, centerX - cropWidth / 2);
                int cropY = Math.Max(0, centerY - cropHeight / 2);

                cropWidth = Math.Min(cropWidth, image.Width - cropX);
                cropHeight = Math.Min(cropHeight, image.Height - cropY);

                Image<Rgb24> croppedImage = image.Clone();
                croppedImage.Mutate(x => x.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));

                double resizeFactor = Math.Min(targetWidth / (double)cropWidth, targetHeight / (double)cropHeight);
                int resizedWidth = (int)(cropWidth * resizeFactor);
                int resizedHeight = (int)(cropHeight * resizeFactor);

                Image<Rgb24> resizedImage = croppedImage.Clone();
                resizedImage.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(resizedWidth, resizedHeight),
                    Mode = ResizeMode.Stretch,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
                    Compand = true
                }));

                if (ApplySharpen)
                {
                    resizedImage.Mutate(image => image.GaussianSharpen(_sharpenSigma));
                }

                JpegEncoder encoder = new JpegEncoder()
                {
                    ColorType = JpegEncodingColor.Rgb,
                    Interleaved = true,
                    Quality = 100,
                    SkipMetadata = false
                };

                await resizedImage.SaveAsJpegAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".jpeg"), encoder);
            }
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
        public async Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            SemaphoreSlim semaphore = new SemaphoreSlim(_semaphoreConcurrent);

            foreach (var file in files)
            {
                await semaphore.WaitAsync();

                try
                {
                    await ResizeImageAsync(file, outputPath, dimension);
                }
                finally
                {
                    semaphore.Release();
                }
            }
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
        public async Task ResizeImagesAsync(string inputPath, string outputPath, Progress progress, SupportedDimensions dimension)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            progress.TotalFiles = files.Length;

            SemaphoreSlim semaphore = new SemaphoreSlim(_semaphoreConcurrent);
            foreach (var file in files)
            {
                await semaphore.WaitAsync();

                try
                {
                    await ResizeImageAsync(file, outputPath, dimension);
                }
                finally
                {
                    progress.UpdateProgress();
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Asynchronously processes an image for tag prediction by resizing it to 448x448 and converting it to a float array.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <returns>An <see cref="WDInputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by inputPath does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by inputPath.</exception>
        public async Task<WDInputData> ProcessImageForTagPredictionAsync(string inputPath)
        {
            WDInputData inputData = new WDInputData();
            inputData.Input1 = new float[448 * 448 * 3];

            int index = 0;
            using (Image<Bgr24> image = await Image.LoadAsync<Bgr24>(inputPath))
            {
                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
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

                            inputData.Input1[index++] = pixel.R;
                            inputData.Input1[index++] = pixel.G;
                            inputData.Input1[index++] = pixel.B;
                        }
                    }
                });
            }
            return inputData;
        }

        /// <summary>
        /// Asynchronously processes an image for tag prediction by resizing it to 448x448 and converting it to a float array.
        /// </summary>
        /// <param name="inputStream">The stream containing the image data to be processed.</param>
        /// <returns>An <see cref="WDInputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the stream containing the image data is not found.</exception>
        /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs while opening the stream containing the image data.</exception>
        public async Task<WDInputData> ProcessImageForTagPredictionAsync(Stream inputStream)
        {
            WDInputData inputData = new WDInputData();
            inputData.Input1 = new float[448 * 448 * 3];

            int index = 0;
            using (Image<Bgr24> image = await Image.LoadAsync<Bgr24>(inputStream))
            {
                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
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

                            inputData.Input1[index++] = pixel.R;
                            inputData.Input1[index++] = pixel.G;
                            inputData.Input1[index++] = pixel.B;
                        }
                    }
                });
            }
            return inputData;
        }

        /// <summary>
        /// Asynchronously processes an image for bounding box prediction by resizing it to 416x416 and converting it to a float array.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <returns>A <see cref="Yolov4InputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by inputPath does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by inputPath.</exception>
        public async Task<Yolov4InputData> ProcessImageForBoundingBoxPredictionAsync(string inputPath)
        {
            Yolov4InputData inputData = new Yolov4InputData();
            inputData.Input1 = new float[416 * 416 * 3];

            int index = 0;
            using (Image<Rgb24> image = await Image.LoadAsync<Rgb24>(inputPath))
            {
                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
                    Compand = true,
                    PadColor = new Rgb24(0, 0, 0),
                    Size = new Size(416, 416),
                };

                image.Mutate(image => image.Resize(resizeOptions));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Rgb24 pixel = ref pixelRow[x];

                            float r = pixel.R * 1f / 255f;
                            float g = pixel.G * 1f / 255f;
                            float b = pixel.B * 1f / 255f;

                            inputData.Input1[index++] = r;
                            inputData.Input1[index++] = g;
                            inputData.Input1[index++] = b;
                        }
                    }
                });
            }
            return inputData;
        }

        /// <summary>
        /// Applies a Gaussian blur filter to the specified image bytes and returns the resulting blurred image as a byte array.
        /// </summary>
        /// <param name="imageBytes">The byte array representing the image to be blurred.</param>
        /// <returns>A byte array representing the blurred image.</returns>
        /// <remarks>
        /// <para>
        /// This method loads the image bytes into an ImageSharp image and applies a Gaussian blur filter with the specified blur radius to it.
        /// </para>
        /// <para>
        /// The blurred image is then saved as a JPEG image into a new memory stream, which is converted to a byte array and returned as the result.
        /// </para>
        /// </remarks>
        public async Task<MemoryStream> GetBlurredImageAsync(string imagePath)
        {
            using (Image image = await Image.LoadAsync(imagePath))
            {
                image.Mutate(x => x.GaussianBlur(_blurRadius));

                MemoryStream blurredImageStream = new MemoryStream();
                image.SaveAsJpeg(blurredImageStream, new JpegEncoder());

                return blurredImageStream;
            }
        }

        /// <summary>
        /// Asynchronously reads the metadata of an image from the provided stream.
        /// </summary>
        /// <param name="imageStream">The stream containing the image data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of strings representing the image metadata.</returns>
        public async Task<List<string>> ReadImageMetadataAsync(Stream imageStream)
        {
            List<string> metadata = new List<string>(3);
            using (Image image = await Image.LoadAsync(imageStream))
            {
                PngMetadata pngMetadata = image.Metadata.GetPngMetadata();
                if (pngMetadata != null)
                {
                    PngTextData metadataText = pngMetadata.TextData.FirstOrDefault();

                    if (metadataText.Value.Contains($"Negative prompt: "))
                    {
                        string[] split1 = metadataText.Value.Split($"Negative prompt: ");
                        string[] split2 = split1[1].Split("Steps: ");

                        metadata.Add(split1[0]);
                        metadata.Add(split2[0]);
                        metadata.Add($"Steps: {split2[1]}");
                    }
                    else
                    {
                        string[] split = metadataText.Value.Split($"Steps: ");

                        metadata.Add(split[0]);
                        metadata.Add(string.Empty);
                        metadata.Add($"Steps: {split[1]}");
                    }

                    for (int i = 0; i < metadata.Count; i++)
                    {
                        if (metadata[i].EndsWith(", "))
                        {
                            string formatted = metadata[i].Remove(metadata[i].Length - 2, 2);
                            metadata[i] = formatted;
                        }
                        else if (metadata[i].EndsWith(','))
                        {
                            string formatted = metadata[i].Remove(metadata[i].Length - 1, 1);
                            metadata[i] = formatted;
                        }

                        metadata[i] = metadata[i].TrimEnd();
                    }
                }
            }
            return metadata;
        }

        /// <summary>
        /// Resizes an image to a target aspect ratio and saves it as a JPEG file in the output directory.
        /// </summary>
        /// <param name="outputPath">The full path of the directory to save the resized image file in.</param>
        /// <param name="inputPath">The full path of the image file to resize.</param>
        /// <param name="dimension">The maximum dimension (width or height) of the resized image. Defaults to 512.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of resizing and saving the image file.</returns>
        /// <remarks>
        /// <para>
        /// This method resizes the input image file to a target aspect ratio based on a predetermined set of aspect ratio buckets. The target aspect ratio is calculated based on the number of blocks assigned to each aspect ratio bucket.
        /// </para>
        /// <para>
        /// The resized image is saved as a JPEG file in the output directory with the same name as the original file, but with the extension changed to ".jpeg".
        /// </para>
        /// <para>
        /// The maximum dimension (width or height) of the resized image can be specified using the optional dimension parameter. If both width and height of the image are less than the specified dimension, then the image is not resized. The default value of dimension is 512.
        /// </para>
        /// <para>
        /// This method uses a Lanczos resampling algorithm for high-quality image resizing. The JPEG encoding quality is set to 100 and metadata is not skipped.
        /// </para>
        /// </remarks>
        private async Task ResizeImageAsync(string inputPath, string outputPath, SupportedDimensions dimension)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            using (Image image = await Image.LoadAsync(inputPath))
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;

                double aspectRatio = Math.Round(image.Width / (double)image.Height, 2);

                int bucket = FindAspectRatioBucket(aspectRatio);

                int blocks = _aspectRatioToBlocks.Values.Sum();

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
                    Mode = ResizeMode.Stretch,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
                    Compand = true,
                    PadColor = Color.White,
                    Size = new Size(targetWidth, targetHeight)
                };

                image.Mutate(image =>
                {
                    image.BackgroundColor(Color.White);
                    image.Resize(resizeOptions);
                });

                if (ApplySharpen && (originalWidth >= MinimumResolutionForSigma || originalHeight >= MinimumResolutionForSigma))
                {
                    image.Mutate(image => image.GaussianSharpen(_sharpenSigma));
                }

                JpegEncoder encoder = new JpegEncoder()
                {
                    ColorType = JpegEncodingColor.Rgb,
                    Interleaved = true,
                    Quality = 100,
                    SkipMetadata = false
                };

                await image.SaveAsJpegAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".jpeg"), encoder);
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
