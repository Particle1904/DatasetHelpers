// Ignore Spelling: Metadata Lanczos

using HeyRed.ImageSharp.Heif.Formats.Avif;
using HeyRed.ImageSharp.Heif.Formats.Heif;

using ImageMagick;

using Microsoft.ML.OnnxRuntime.Tensors;

using Models;
using Models.MachineLearning;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Png.Chunks;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Models.MachineLearning.SAM2;
using SmartData.Lib.Services.Base;

namespace SmartData.Lib.Services
{
    public class ImageProcessorService : CancellableServiceBase, IImageProcessorService, INotifyProgress
    {
        private readonly string _imageSearchPattern = Utilities.GetSupportedImagesExtension;

        private readonly DecoderOptions _decoderOptions = new DecoderOptions()
        {
            Configuration = new Configuration(
                new JpegConfigurationModule(),
                new PngConfigurationModule(),
                new GifConfigurationModule(),
                new WebpConfigurationModule(),
                new AvifConfigurationModule(),
                new HeifConfigurationModule())
        };

        private readonly PngEncoder _pngEncoder = new PngEncoder()
        {
            ColorType = PngColorType.RgbWithAlpha,
            SkipMetadata = false
        };

        private LanczosResampler _lanczosResampler;
        private BicubicResampler _bicubicResampler;
        private CubicResampler _cubicResampler;

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

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        private static readonly float[] SAM2PixelMean = { 123.675f, 116.28f, 103.53f };
        private static readonly float[] SAM2PixelStd = { 58.395f, 57.12f, 57.375f };

        private static readonly float[] Florence2VisionEncoderPixelMean = { 0.485f, 0.456f, 0.406f };
        private static readonly float[] Florence2VisionEncoderPixelStd = { 0.229f, 0.224f, 0.225f };

        public ImageProcessorService() : base()
        {
            BlocksPerRow = _baseResolution / _divisor;
            _totalBlocks = BlocksPerRow * BlocksPerRow;
            _aspectRatioToBlocks = CalculateBuckets(_totalBlocks);
            _bicubicResampler = new BicubicResampler();
            _lanczosResampler = new LanczosResampler(_lanczosSamplerRadius);
            _cubicResampler = CubicResampler.MitchellNetravali;
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
            using (Image image = await Image.LoadAsync(_decoderOptions, filePath))
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

            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
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

                image.Mutate(context => context.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));

                double resizeFactor = Math.Min(targetWidth / (double)cropWidth, targetHeight / (double)cropHeight);
                int resizedWidth = (int)(cropWidth * resizeFactor);
                int resizedHeight = (int)(cropHeight * resizeFactor);

                ResizeOptions resizeOptions = new ResizeOptions
                {
                    Size = new Size(resizedWidth, resizedHeight),
                    Mode = ResizeMode.Stretch,
                    Position = AnchorPositionMode.Center,
                    PadColor = Color.White,
                    Sampler = _lanczosResampler,
                    Compand = true
                };

                image.Mutate(context => context.Resize(resizeOptions));

                if (ApplySharpen)
                {
                    image.Mutate(context => context.GaussianSharpen(_sharpenSigma));
                }

                image.Mutate(context => context.BackgroundColor(Color.White));
                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                await image.SaveAsPngAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".png"), _pngEncoder);
            }
        }

        /// <summary>
        /// Resizes all images in a given input directory and saves the resized images to an output directory.
        /// </summary>
        /// <param name="inputPath">The path to the directory containing the input images.</param>
        /// <param name="outputPath">The path to the directory where the resized images will be saved.</param>
        /// <param name="dimension">The target image dimension to resize the images to.</param>
        /// <remarks>
        /// This method uses multiple threads to resize the images in parallel. Each image is resized to a target aspect ratio based on a predetermined set of aspect ratio buckets. The resized images are saved as PNG files in the output directory.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Thrown when either the inputPath or outputPath is null.</exception>
        public async Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            SemaphoreSlim semaphore = new SemaphoreSlim(_semaphoreConcurrent);
            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await semaphore.WaitAsync();

                try
                {
                    await ResizeImageAsync(file, outputPath, dimension);
                }
                finally
                {
                    ProgressUpdated?.Invoke(this, EventArgs.Empty);
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Asynchronously processes an image for tag prediction by resizing it to 448x448 and converting it to a float array.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <returns>An <see cref="WDInputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by <paramref name="inputPath"/>.</exception>
        public async Task<WDInputData> ProcessImageForTagPredictionAsync(string inputPath)
        {
            WDInputData inputData = new WDInputData()
            {
                Input = new DenseTensor<float>(new int[] { 1, 448, 448, 3 })
            };

            using (Image<Bgr24> image = await Image.LoadAsync<Bgr24>(_decoderOptions, inputPath))
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

                image.Mutate(context => context.Resize(resizeOptions));

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

                            inputData.Input[0, y, x, 0] = pixel.R;
                            inputData.Input[0, y, x, 1] = pixel.G;
                            inputData.Input[0, y, x, 2] = pixel.B;
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
            inputData.Input = new DenseTensor<float>(new int[] { 1, 448, 448, 3 });

            using (Image<Bgr24> image = await Image.LoadAsync<Bgr24>(_decoderOptions, inputStream))
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

                image.Mutate(context => context.Resize(resizeOptions));

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

                            inputData.Input[0, y, x, 0] = pixel.R;
                            inputData.Input[0, y, x, 1] = pixel.G;
                            inputData.Input[0, y, x, 2] = pixel.B;
                        }
                    }
                });
            }
            return inputData;
        }

        /// <summary>
        /// Asynchronously processes an image for tag prediction, using the JoyTag model, by resizing it to 448x448 
        /// and converting it to a float array.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <returns>An <see cref="JoyTagInputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by <paramref name="inputPath"/>.</exception>
        public async Task<JoyTagInputData> ProcessImageForJoyTagPredictionAsync(string inputPath)
        {
            JoyTagInputData inputData = new JoyTagInputData()
            {
                Input = new DenseTensor<float>(new int[] { 1, 3, 448, 448 })
            };

            using (Image<Rgb24> image = await Image.LoadAsync<Rgb24>(_decoderOptions, inputPath))
            {
                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = _bicubicResampler,
                    Compand = true,
                    PadColor = new Rgb24(255, 255, 255),
                    Size = new Size(448, 448),
                };

                image.Mutate(context => context.Resize(resizeOptions));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            Rgb24 pixel = pixelRow[x];

                            inputData.Input[0, 0, y, x] = (pixel.R / 255.0f - 0.48145466f) / 0.26862954f;
                            inputData.Input[0, 1, y, x] = (pixel.G / 255.0f - 0.4578275f) / 0.26130258f;
                            inputData.Input[0, 2, y, x] = (pixel.B / 255.0f - 0.40821073f) / 0.27577711f;
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
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by <paramref name="inputPath"/>.</exception>
        public async Task<Yolov4InputData> ProcessImageForBoundingBoxPredictionAsync(string inputPath)
        {
            Yolov4InputData inputData = new Yolov4InputData()
            {
                Input = new DenseTensor<float>(new int[] { 1, 416, 416, 3 })
            };

            using (Image<Rgb24> image = await Image.LoadAsync<Rgb24>(_decoderOptions, inputPath))
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

                image.Mutate(context => context.Resize(resizeOptions));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Rgb24 pixel = ref pixelRow[x];

                            inputData.Input[0, y, x, 0] = pixel.R / 255f;
                            inputData.Input[0, y, x, 1] = pixel.G / 255f;
                            inputData.Input[0, y, x, 2] = pixel.B / 255f;
                        }
                    }
                });
            }
            return inputData;
        }

        /// <summary>
        /// Asynchronously processes an image for upscaling by converting it to a float array.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <returns>An <see cref="UpscalerInputData"/> object containing the processed image as a float array.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by <paramref name="inputPath"/>.</exception>
        public async Task<UpscalerInputData> ProcessImageForUpscalingAsync(string inputPath)
        {
            UpscalerInputData inputData = new UpscalerInputData();

            using (Image<Bgra32> image = await Image.LoadAsync<Bgra32>(_decoderOptions, inputPath))
            {
                // Adjust the image dimensions to be even.
                int height = image.Height;
                if (height % 2 != 0)
                {
                    height -= 1;
                }
                int width = image.Width;
                if (width % 2 != 0)
                {
                    width -= 1;
                }

                inputData.Input = new DenseTensor<float>(new[] { 1, 3, height, width });

                ResizeOptions resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
                    Compand = true,
                    PadColor = new Rgb24(0, 0, 0),
                    Size = new Size(width, height),
                };

                image.Mutate(context => context.Resize(resizeOptions));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Bgra32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Bgra32 pixel = ref pixelRow[x];

                            inputData.Input[0, 0, y, x] = pixel.B / 255f;
                            inputData.Input[0, 1, y, x] = pixel.G / 255f;
                            inputData.Input[0, 2, y, x] = pixel.R / 255f;
                        }
                    }
                });
            }

            return inputData;
        }

        /// <summary>
        /// Asynchronously processes an image and its corresponding mask for inpainting by converting them to float arrays.
        /// </summary>
        /// <param name="inputPath">The path of the image to be processed.</param>
        /// <param name="inputMaskPath">The path of the mask to be processed.</param>
        /// <returns>An <see cref="LaMaInputData"/> object containing the processed image and mask as float arrays.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputPath"/> or <paramref name="inputMaskPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by <paramref name="inputPath"/> or <paramref name="inputMaskPath"/>.</exception>
        /// <exception cref="ArgumentException">The dimensions of the mask do not match the dimensions of the image.</exception>
        public async Task<LaMaInputData> ProcessImageForInpaintAsync(string inputPath, string inputMaskPath)
        {
            LaMaInputData inputData = new LaMaInputData()
            {
                InputImage = new DenseTensor<float>(new[] { 1, 3, 512, 512 }),
                InputMask = new DenseTensor<float>(new[] { 1, 1, 512, 512 })
            };

            ResizeOptions resizeOptions = new ResizeOptions()
            {
                Mode = ResizeMode.BoxPad,
                Position = AnchorPositionMode.Center,
                Sampler = _cubicResampler,
                Compand = true,
                PadColor = new Rgb24(0, 0, 0),
                Size = new Size(512, 512),
            };

            // Process input image
            Point imageSize;
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
            {
                inputData.OriginalSize = new Point(image.Width, image.Height);

                imageSize = new Point(image.Width, image.Height);

                image.Mutate(context => context.Resize(resizeOptions));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Rgba32 pixel = ref pixelRow[x];

                            inputData.InputImage[0, 0, y, x] = pixel.R / 255f;
                            inputData.InputImage[0, 1, y, x] = pixel.G / 255f;
                            inputData.InputImage[0, 2, y, x] = pixel.B / 255f;
                        }
                    }
                });
            }

            // Process input mask
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputMaskPath))
            {
                if (image.Width != imageSize.X || image.Height != imageSize.Y)
                {
                    throw new ArgumentException("The mask and the image must have the same Width and Height!");
                }

                image.Mutate(image =>
                {
                    image.Resize(resizeOptions);
                    image.GaussianBlur(6.0f);
                });


                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            ref Rgba32 pixel = ref pixelRow[x];

                            float color = 0.0f;
                            if ((pixel.R + pixel.G + pixel.B) > 0)
                            {
                                color = 1.0f;
                            }

                            inputData.InputMask[0, 0, y, x] = color;
                        }
                    }
                });
            }

            return inputData;
        }

        /// <summary>
        /// Prepares an image and its mask for tile-based inpainting by splitting them into overlapping tiles.
        /// </summary>
        /// <param name="inputPath">The file path to the input image.</param>
        /// <param name="inputMaskPath">The file path to the input mask.</param>
        /// <param name="tileSize">The size of each square tile for processing. Defaults to 512.</param>
        /// <param name="overlap">The number of pixels that adjacent tiles will overlap. Defaults to 126.</param>
        /// <returns>An array of <see cref="TileData"/> containing the processed image and mask data for each tile, ready for inference.</returns>
        /// <exception cref="ArgumentException">Thrown when the input image and mask do not have the same dimensions.</exception>
        /// <remarks>
        /// This method splits both the input image and mask into tiles of the specified size with a given overlap. 
        /// Edge tiles that are smaller than the target size are padded using a mirror of their content.
        /// It then converts each pair of tiles into normalized tensors for the inpainting model.
        /// </remarks>
        public async Task<TileData[]> ProcessImageForTileInpaintAsync(string inputPath, string inputMaskPath, int tileSize = 512, int overlap = 126)
        {
            System.Drawing.Size imageSize = await GetImageSizeAsync(inputPath);
            System.Drawing.Size maskSize = await GetImageSizeAsync(inputMaskPath);
            if (!imageSize.Equals(maskSize))
            {
                throw new ArgumentException("Image and Mask must be same size!");
            }

            TileImage[] imageTiles = await ExtractTilesFromImage(inputPath, tileSize, overlap);
            TileImage[] maskTiles = await ExtractTilesFromImage(inputMaskPath, tileSize, overlap);

            if (imageTiles.Length != maskTiles.Length)
            {
                throw new ArgumentException("The number of Image Tiles and Mask Tiles isn't the same!");
            }

            List<TileData> tiles = new List<TileData>(imageTiles.Length);
            for (int i = 0; i < imageTiles.Length; i++)
            {
                LaMaInputData inputData = new LaMaInputData()
                {
                    InputImage = new DenseTensor<float>(new[] { 1, 3, tileSize, tileSize }),
                    InputMask = new DenseTensor<float>(new[] { 1, 1, tileSize, tileSize }),
                    OriginalSize = new Point(imageSize.Width, imageSize.Height)
                };

                using (Image<Rgba32> image = imageTiles[i].Image.CloneAs<Rgba32>())
                {
                    image.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                            for (int x = 0; x < pixelRow.Length; x++)
                            {
                                ref Rgba32 pixel = ref pixelRow[x];
                                inputData.InputImage[0, 0, y, x] = pixel.R / 255f;
                                inputData.InputImage[0, 1, y, x] = pixel.G / 255f;
                                inputData.InputImage[0, 2, y, x] = pixel.B / 255f;
                            }
                        }
                    });
                }

                using (Image<Rgba32> mask = maskTiles[i].Image.CloneAs<Rgba32>())
                {
                    mask.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < accessor.Height; y++)
                        {
                            Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                            for (int x = 0; x < pixelRow.Length; x++)
                            {
                                ref Rgba32 pixel = ref pixelRow[x];
                                float color;
                                if ((pixel.R + pixel.G + pixel.B) > 0)
                                {
                                    color = 1.0f;
                                }
                                else
                                {
                                    color = 0.0f;
                                }
                                inputData.InputMask[0, 0, y, x] = color;
                            }
                        }
                    });
                }

                TileData tile = new TileData(inputData, imageTiles[i].RowIndex, imageTiles[i].ColumnIndex, imageTiles[i].X, imageTiles[i].Y);
                tiles.Add(tile);
            }

            return tiles.ToArray();
        }

        /// <summary>
        /// Asynchronously processes an image for SAM2 encoding by resizing (with black padding to 1024×1024),
        /// then converting its pixels from the 0–255 range into a mean–std–normalized float tensor
        /// of shape [1, 3, 1024, 1024].
        /// </summary>
        /// <param name="inputPath">The file path of the image to be processed.</param>
        /// <returns>
        /// A <see cref="SAM2EncoderInputData"/> containing the image as a float tensor,
        /// normalized using the SAM2 model’s pixel mean and standard deviation.
        /// </returns>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown if the file specified by <paramref name="inputPath"/> does not exist.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an I/O error occurs while loading or reading the image file.
        /// </exception>
        public async Task<SAM2EncoderInputData> ProcessImageForSAM2EncodingAsync(string inputPath)
        {
            int sam2ImageInputSize = 1024;

            SAM2EncoderInputData inputData = new SAM2EncoderInputData()
            {
                InputImage = new DenseTensor<float>(new int[] { 1, 3, sam2ImageInputSize, sam2ImageInputSize })
            };

            using (Image<Rgb24> image = await Image.LoadAsync<Rgb24>(_decoderOptions, inputPath))
            {
                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
                    Compand = true,
                    PadColor = new Rgb24(0, 0, 0),
                    Size = new Size(sam2ImageInputSize, sam2ImageInputSize)
                };

                image.Mutate(context => context.Resize(resizeOptions));
                image.Mutate(context => context.GaussianSharpen(0.5f));

                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgb24> row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            ref Rgb24 px = ref row[x];

                            // Keep 0..255 range, then normalize
                            float r = (px.R - SAM2PixelMean[0]) / SAM2PixelStd[0];
                            float g = (px.G - SAM2PixelMean[1]) / SAM2PixelStd[1];
                            float b = (px.B - SAM2PixelMean[2]) / SAM2PixelStd[2];

                            inputData.InputImage[0, 0, y, x] = r;
                            inputData.InputImage[0, 1, y, x] = g;
                            inputData.InputImage[0, 2, y, x] = b;
                        }
                    }
                });
            }

            return inputData;
        }

        /// <summary>
        /// Saves the upscaled image data to the specified output path.
        /// </summary>
        /// <param name="outputPath">The path where the upscaled image will be saved.</param>
        /// <param name="outputData">The data containing the upscaled image.</param>
        public void SaveUpscaledImage(string outputPath, UpscalerOutputData outputData)
        {
            int width = outputData.Output.Dimensions[3];
            int height = outputData.Output.Dimensions[2];

            byte[] imageByte = new byte[3 * width * height];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int baseIndex = (row * width + col) * 3;
                    byte r = (byte)Math.Clamp(outputData.Output[0, 2, row, col] * 255, 0, 255);
                    byte g = (byte)Math.Clamp(outputData.Output[0, 1, row, col] * 255, 0, 255);
                    byte b = (byte)Math.Clamp(outputData.Output[0, 0, row, col] * 255, 0, 255);

                    imageByte[baseIndex] = r;
                    imageByte[baseIndex + 1] = g;
                    imageByte[baseIndex + 2] = b;
                }
            }

            ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(imageByte);
            using (Image<Rgb24> image = Image.LoadPixelData<Rgb24>(bytes, width, height))
            {
                image.SaveAsPng(outputPath);
            }
        }

        /// <summary>
        /// Saves the inpainted image data to the specified output path.
        /// </summary>
        /// <param name="outputPath">The path where the inpainted image will be saved.</param>
        /// <param name="inputData">The input data containing the original size of the image.</param>
        /// <param name="outputData">The output data containing the inpainted image.</param>
        public void SaveInpaintedImage(string outputPath, LaMaInputData inputData, LaMaOutputData outputData)
        {
            int width = outputData.OutputImage.Dimensions[3];
            int height = outputData.OutputImage.Dimensions[2];

            byte[] imageByte = new byte[3 * width * height];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int baseIndex = (row * width + col) * 3;
                    byte r = (byte)Math.Clamp(outputData.OutputImage[0, 2, row, col], 0, 255);
                    byte g = (byte)Math.Clamp(outputData.OutputImage[0, 1, row, col], 0, 255);
                    byte b = (byte)Math.Clamp(outputData.OutputImage[0, 0, row, col], 0, 255);

                    imageByte[baseIndex] = b;
                    imageByte[baseIndex + 1] = g;
                    imageByte[baseIndex + 2] = r;
                }
            }

            ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(imageByte);

            using (Image<Rgb24> image = Image.LoadPixelData<Rgb24>(bytes, width, height))
            {
                float originalAspectRatio = (float)inputData.OriginalSize.X / inputData.OriginalSize.Y;
                float currentAspectRatio = (float)width / height;

                int cropWidth = width;
                int cropHeight = height;

                if (currentAspectRatio > originalAspectRatio)
                {
                    cropWidth = (int)(height * originalAspectRatio);
                }
                else if (currentAspectRatio < originalAspectRatio)
                {
                    cropHeight = (int)(width / originalAspectRatio);
                }

                int cropX = (width - cropWidth) / 2;
                int cropY = (height - cropHeight) / 2;

                image.Mutate(context => context.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));
                image.Mutate(context => context.Resize(inputData.OriginalSize.X, inputData.OriginalSize.Y, KnownResamplers.Lanczos3));

                image.SaveAsPng(outputPath);
            }
        }

        /// <summary>
        /// Saves an inpainted image by seamlessly blending an array of processed tiles.
        /// </summary>
        /// <param name="outputPath">The file path where the final inpainted image will be saved.</param>
        /// <param name="outputData">An array of <see cref="LaMaOutputData"/> containing the processed output for each tile.</param>
        /// <param name="originalSize">The original dimensions of the complete image.</param>
        /// <param name="tileSize">The size of each tile that was processed. Defaults to 512.</param>
        /// <param name="overlap">The pixel overlap that was used to generate the tiles. Defaults to 126.</param>
        /// <remarks>
        /// This method reconstructs the full image from individual tiles. It uses a weighted-blending (feathering) algorithm 
        /// in the overlapping regions to eliminate visible seams and create a smooth, continuous result.
        /// </remarks>
        public void SaveInpaintedImage(string outputPath, LaMaOutputData[] outputData, System.Drawing.Size originalSize, int tileSize = 512, int overlap = 126)
        {
            if (outputData.Length == 0)
            {
                return;
            }

            int imageWidth = originalSize.Width;
            int imageHeight = originalSize.Height;

            System.Numerics.Vector3[] colorSum = new System.Numerics.Vector3[imageWidth * imageHeight];
            float[] weightSum = new float[imageWidth * imageHeight];

            float[] weightMap = new float[tileSize * tileSize];
            int feather = overlap / 2;
            for (int y = 0; y < tileSize; y++)
            {
                for (int x = 0; x < tileSize; x++)
                {
                    float weightX = 1.0f;
                    if (x < feather)
                    {
                        weightX = (float)x / feather;
                    }
                    else if (x >= tileSize - feather)
                    {
                        weightX = (float)(tileSize - 1 - x) / feather;
                    }

                    float weightY = 1.0f;
                    if (y < feather)
                    {
                        weightY = (float)y / feather;
                    }
                    else if (y >= tileSize - feather)
                    {
                        weightY = (float)(tileSize - 1 - y) / feather;
                    }

                    weightMap[y * tileSize + x] = Math.Min(weightX, weightY);
                }
            }

            foreach (LaMaOutputData tileData in outputData)
            {
                DenseTensor<float> outputTensor = tileData.OutputImage;
                int tileStartX = tileData.X;
                int tileStartY = tileData.Y;

                for (int y = 0; y < tileSize; y++)
                {
                    int globalY = tileStartY + y;
                    if (globalY >= imageHeight) continue;

                    for (int x = 0; x < tileSize; x++)
                    {
                        int globalX = tileStartX + x;
                        if (globalX >= imageWidth) continue;

                        float weight = weightMap[y * tileSize + x];
                        if (weight <= 0) continue;

                        float r = outputTensor[0, 0, y, x];
                        float g = outputTensor[0, 1, y, x];
                        float b = outputTensor[0, 2, y, x];

                        int globalIndex = globalY * imageWidth + globalX;

                        colorSum[globalIndex] += new System.Numerics.Vector3(r, g, b) * weight;
                        weightSum[globalIndex] += weight;
                    }
                }
            }

            using (Image<Rgba32> resultImage = new Image<Rgba32>(imageWidth, imageHeight))
            {
                resultImage.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            int index = y * imageWidth + x;
                            float totalWeight = weightSum[index];

                            if (totalWeight > 0)
                            {
                                System.Numerics.Vector3 finalColor = colorSum[index] / totalWeight;

                                pixelRow[x] = new Rgba32(
                                    (byte)Math.Clamp(finalColor.X, 0, 255),
                                    (byte)Math.Clamp(finalColor.Y, 0, 255),
                                    (byte)Math.Clamp(finalColor.Z, 0, 255),
                                    255);
                            }
                        }
                    }
                });

                resultImage.SaveAsPng(outputPath);
            }
        }

        /// <summary>
        /// Gets the upscaled image from UpscalerOutputData.
        /// </summary>
        /// <param name="outputData">The output data containing the upscaled image data.</param>
        /// <returns>The upscaled image.</returns>
        public Image GetUpscaledImage(UpscalerOutputData outputData)
        {
            int width = outputData.Output.Dimensions[3];
            int height = outputData.Output.Dimensions[2];

            byte[] imageByte = new byte[3 * width * height];
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int baseIndex = (row * width + col) * 3;
                    byte r = (byte)Math.Clamp(outputData.Output[0, 2, row, col] * 255, 0, 255);
                    byte g = (byte)Math.Clamp(outputData.Output[0, 1, row, col] * 255, 0, 255);
                    byte b = (byte)Math.Clamp(outputData.Output[0, 0, row, col] * 255, 0, 255);

                    imageByte[baseIndex] = r;
                    imageByte[baseIndex + 1] = g;
                    imageByte[baseIndex + 2] = b;
                }
            }

            ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(imageByte);
            using (Image<Rgb24> image = Image.LoadPixelData<Rgb24>(bytes, width, height))
            {
                return image;
            }
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
        public async Task<MemoryStream> GetBlurredImageAsync(string inputPath)
        {
            using (Image image = await Image.LoadAsync(_decoderOptions, inputPath))
            {
                image.Mutate(context => context.GaussianBlur(_blurRadius));

                MemoryStream blurredImageStream = new MemoryStream();
                image.Mutate(context => context.BackgroundColor(Color.White));
                image.SaveAsJpeg(blurredImageStream, new JpegEncoder());

                return blurredImageStream;
            }
        }

        /// <summary>
        /// Crops the specified region from an image and saves the cropped portion to the output path.
        /// </summary>
        /// <param name="inputPath">The path of the input image file.</param>
        /// <param name="outputPath">The path where the cropped image will be saved.</param>
        /// <param name="startingPosition">The starting point of the crop region.</param>
        /// <param name="endingPosition">The ending point of the crop region.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// The method loads the input image, crops the specified region defined by the starting and ending positions,
        /// and saves the cropped image to the output path. The crop region is defined by the rectangle formed
        /// by the starting and ending positions.
        /// </remarks>
        public async Task CropImageAsync(string inputPath, string outputPath, System.Drawing.Point startingPosition, System.Drawing.Point endingPosition)
        {
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
            {
                int x = Math.Min(startingPosition.X, endingPosition.X);
                int y = Math.Min(startingPosition.Y, endingPosition.Y);
                int width = Math.Abs(startingPosition.X - endingPosition.X);
                int height = Math.Abs(startingPosition.Y - endingPosition.Y);

                Rectangle cropArea = new Rectangle()
                {
                    X = Math.Clamp(x, 0, image.Width),
                    Y = Math.Clamp(y, 0, image.Height),
                    Width = Math.Clamp(width, 0, image.Width - x),
                    Height = Math.Clamp(height, 0, image.Height - y)
                };

                image.Mutate(context => context.Crop(cropArea));

                image.Mutate(context => context.BackgroundColor(Color.White));
                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                await image.SaveAsPngAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".png"), _pngEncoder);
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
            using (Image image = await Image.LoadAsync(_decoderOptions, imageStream))
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
        /// Creates an image mask of the specified width and height, filled with a black background, 
        /// and returns it as a PNG image in a memory stream.
        /// </summary>
        /// <param name="width">The width of the image mask to be created.</param>
        /// <param name="height">The height of the image mask to be created.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the PNG image of the created black image mask.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a new image of the specified dimensions using the ImageSharp library, 
        /// fills it with a black background, and then saves it as a PNG image into a memory stream.
        /// </para>
        /// <para>
        /// The resulting memory stream containing the PNG image is returned to the caller.
        /// </para>
        /// </remarks>
        public MemoryStream CreateImageMask(int width, int height)
        {
            using (Image<Rgba32> image = new Image<Rgba32>(width, height))
            {
                image.Mutate(context => context.BackgroundColor(Color.Black));

                MemoryStream imageMaskStream = new MemoryStream();
                image.SaveAsPng(imageMaskStream, _pngEncoder);

                return imageMaskStream;
            }
        }

        /// <summary>
        /// Draws a circle on an existing image mask and returns the updated image as a PNG image in a memory stream.
        /// </summary>
        /// <param name="maskStream">The memory stream containing the existing image mask.</param>
        /// <param name="position">The center position of the circle to be drawn.</param>
        /// <param name="radius">The radius of the circle to be drawn.</param>
        /// <param name="color">The color of the circle to be drawn.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the PNG image of the updated image mask with the drawn circle.</returns>
        /// <remarks>
        /// <para>
        /// This method takes an existing image mask from a memory stream, draws a circle at the specified position with the 
        /// given radius and color, and saves the updated image as a PNG image into a new memory stream.
        /// </para>
        /// <para>
        /// The resulting memory stream containing the PNG image is returned to the caller.
        /// </para>
        /// </remarks>
        public MemoryStream DrawCircleOnMask(MemoryStream maskStream, Point position, float radius, Color color)
        {
            maskStream.Seek(0, SeekOrigin.Begin);
            using (Image image = Image.Load(maskStream))
            {
                SolidBrush brush = new SolidBrush(color);
                SixLabors.ImageSharp.Drawing.EllipsePolygon circle = new SixLabors.ImageSharp.Drawing.EllipsePolygon(position.X,
                    position.Y, radius);
                image.Mutate(context => context.Fill(brush, circle));

                MemoryStream imageMaskStream = new MemoryStream();
                image.SaveAsPng(imageMaskStream, _pngEncoder);

                return imageMaskStream;
            }
        }

        /// <summary>
        /// Converts an image at the specified input path to a Base64-encoded string in JPEG format.
        /// </summary>
        /// <param name="inputPath">The file path of the input image.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the Base64-encoded string of the JPEG image.</returns>
        /// <remarks>
        /// This method performs the following steps:
        /// <list type="number">
        /// <item>Loads the input image from the specified file path.</item>
        /// <item>Encodes the image into JPEG format with a quality setting of 100.</item>
        /// <item>Converts the encoded JPEG image into a Base64 string.</item>
        /// </list>
        /// Supported image formats depend on the <c>SixLabors.ImageSharp</c> library.
        /// </remarks>
        /// <exception cref="FileNotFoundException">Thrown if the specified input file does not exist.</exception>
        /// <exception cref="ImageFormatException">Thrown if the input file is not a valid image format.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the application lacks permissions to access the specified file.</exception>
        /// <example>
        /// <code>
        /// string inputPath = "path/to/image.png";
        /// string base64Image = await GetBase64ImageAsync(inputPath);
        /// Console.WriteLine(base64Image);
        /// </code>
        /// </example>
        public async Task<string> GetBase64ImageAsync(string inputPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    PngEncoder pngEncoder = new PngEncoder();

                    await image.SaveAsPngAsync(memoryStream, pngEncoder);
                    byte[] jpegBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(jpegBytes);
                }
            }
        }

        /// <summary>
        /// Asynchronously selects the highest‐confidence mask from the SAM2 decoder output, processes it
        /// (resizing, cropping, thresholding, dilation, and blurring), and saves the resulting binary PNG mask
        /// to the specified output path.
        /// </summary>
        /// <param name="SAM2masks">
        /// The <see cref="SAM2DecoderOutputData"/> containing raw mask logits, IoU predictions, and the original
        /// image resolution. Must have a non‐null <c>Masks</c> tensor and valid <c>OriginalResolution</c>.
        /// </param>
        /// <param name="outputPath">
        /// The file path where the final binary mask PNG will be written. The directory must exist and the path
        /// must be writable.
        /// </param>
        /// <param name="dilationSizeInPixels">
        /// The radius, in pixels, by which to dilate the binary mask before applying median and Gaussian blurs.
        /// Defaults to <c>2</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous image‐processing and file‐save operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="SAM2masks"/> is null, its <c>Masks</c> tensor is null, or if
        /// <paramref name="outputPath"/> is null or empty.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an I/O error occurs while writing the output PNG file to <paramref name="outputPath"/>.
        /// </exception>
        public async Task SaveSAM2MaskAsync(SAM2DecoderOutputData SAM2masks, string outputPath, int dilationSizeInPixels = 2)
        {
            const int maskResolution = 256;
            const int encoderInputSize = 1024;

            int originalWidth = SAM2masks.OriginalResolution.Width;
            int originalHeight = SAM2masks.OriginalResolution.Height;

            DenseTensor<float> maskTensor = SAM2masks.Masks!;

            // Select the best mask based on IoU
            int bestIndex = 0;
            float bestIoU = SAM2masks.IouPredictions[0, 0];
            for (int i = 1; i < SAM2masks.IouPredictions.Length; i++)
            {
                if (SAM2masks.IouPredictions[0, i] > bestIoU)
                {
                    bestIoU = SAM2masks.IouPredictions[0, i];
                    bestIndex = i;
                }
            }

            using (Image<L16> floatMask = new Image<L16>(maskResolution, maskResolution))
            {
                // Fill the low-res float mask
                for (int y = 0; y < maskResolution; y++)
                {
                    for (int x = 0; x < maskResolution; x++)
                    {
                        float logit = maskTensor[0, bestIndex, y, x];
                        float probability = Utilities.Sigmoid(logit);
                        ushort value = (ushort)(probability * ushort.MaxValue);
                        floatMask[x, y] = new L16(value);
                    }
                }

                // Resize to 1024x1024
                ResizeOptions upscaleResizeOptions = new ResizeOptions
                {
                    Size = new Size(encoderInputSize, encoderInputSize),
                    Mode = ResizeMode.Stretch,
                    Sampler = _lanczosResampler
                };

                using (Image<L16> upscaledMask = floatMask.Clone(context => context.Resize(upscaleResizeOptions)))
                {
                    // Crop padding (BoxPad logic)
                    float originalAspect = (float)originalWidth / originalHeight;
                    int paddedWidth, paddedHeight, offsetX, offsetY;

                    if (originalAspect > 1)
                    {
                        paddedWidth = encoderInputSize;
                        paddedHeight = (int)(encoderInputSize / originalAspect);
                        offsetX = 0;
                        offsetY = (encoderInputSize - paddedHeight) / 2;
                    }
                    else
                    {
                        paddedWidth = (int)(encoderInputSize * originalAspect);
                        paddedHeight = encoderInputSize;
                        offsetX = (encoderInputSize - paddedWidth) / 2;
                        offsetY = 0;
                    }

                    using (Image<L16> croppedMask = upscaledMask.Clone(context => context.Crop(new Rectangle(offsetX, offsetY, paddedWidth, paddedHeight))))
                    {
                        // Resize to original image size
                        using (Image<L16> finalFloatMask = croppedMask.Clone(context => context.Resize(originalWidth, originalHeight, _lanczosResampler)))
                        {
                            // Threshold to binary mask
                            using (Image<L8> binaryMask = new Image<L8>(originalWidth, originalHeight))
                            {
                                for (int y = 0; y < finalFloatMask.Height; y++)
                                {
                                    for (int x = 0; x < finalFloatMask.Width; x++)
                                    {
                                        float probability = finalFloatMask[x, y].PackedValue / (float)ushort.MaxValue;
                                        byte value = probability > 0.5f ? (byte)255 : (byte)0;
                                        binaryMask[x, y] = new L8(value);
                                    }
                                }

                                // Blur the mask a bit for a better inpaint result
                                Image<L8> dilatedMask = DilateMask(binaryMask, dilationSizeInPixels);
                                dilatedMask.Mutate(context => context.MedianBlur(3, true));
                                dilatedMask.Mutate(context => context.GaussianBlur(5.0f));
                                await dilatedMask.SaveAsPngAsync(outputPath, _pngEncoder);
                                dilatedMask.Dispose();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously selects the highest‐confidence mask from the SAM2 decoder output, processes it
        /// (resizing, cropping, thresholding), and returns the resulting binary mask as an <see cref="Image{L8}"/>.
        /// </summary>
        /// <param name="SAM2masks">
        /// The <see cref="SAM2DecoderOutputData"/> containing raw mask logits, IoU predictions, and the original
        /// image resolution. Must have a non‐null <c>Masks</c> tensor and valid <c>OriginalResolution</c>.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation that returns a binary <see cref="Image{L8}"/> mask
        /// corresponding to the highest-confidence prediction.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="SAM2masks"/> is null or its <c>Masks</c> tensor is null.
        /// </exception>
        public Image<L8> CreateSAM2Mask(SAM2DecoderOutputData SAM2masks)
        {
            const int maskResolution = 256;
            const int encoderInputSize = 1024;

            int originalWidth = SAM2masks.OriginalResolution.Width;
            int originalHeight = SAM2masks.OriginalResolution.Height;

            DenseTensor<float> maskTensor = SAM2masks.Masks!;

            // Select the best mask based on IoU
            int bestIndex = 0;
            float bestIoU = SAM2masks.IouPredictions[0, 0];
            for (int i = 1; i < SAM2masks.IouPredictions.Length; i++)
            {
                if (SAM2masks.IouPredictions[0, i] > bestIoU)
                {
                    bestIoU = SAM2masks.IouPredictions[0, i];
                    bestIndex = i;
                }
            }

            // Build the low-res float mask
            using Image<L16> floatMask = new Image<L16>(maskResolution, maskResolution);
            for (int y = 0; y < maskResolution; y++)
            {
                for (int x = 0; x < maskResolution; x++)
                {
                    float logit = maskTensor[0, bestIndex, y, x];
                    float probability = Utilities.Sigmoid(logit);
                    ushort value = (ushort)(probability * ushort.MaxValue);
                    floatMask[x, y] = new L16(value);
                }
            }

            // Resize to 1024x1024
            ResizeOptions upscaleResizeOptions = new ResizeOptions
            {
                Size = new Size(encoderInputSize, encoderInputSize),
                Mode = ResizeMode.Stretch,
                Sampler = _lanczosResampler
            };

            using Image<L16> upscaledMask = floatMask.Clone(context => context.Resize(upscaleResizeOptions));

            // Crop padding (BoxPad logic)
            float originalAspect = (float)originalWidth / originalHeight;
            int paddedWidth, paddedHeight, offsetX, offsetY;

            if (originalAspect > 1)
            {
                paddedWidth = encoderInputSize;
                paddedHeight = (int)(encoderInputSize / originalAspect);
                offsetX = 0;
                offsetY = (encoderInputSize - paddedHeight) / 2;
            }
            else
            {
                paddedWidth = (int)(encoderInputSize * originalAspect);
                paddedHeight = encoderInputSize;
                offsetX = (encoderInputSize - paddedWidth) / 2;
                offsetY = 0;
            }

            using Image<L16> croppedMask = upscaledMask.Clone(context => context.Crop(new Rectangle(offsetX, offsetY, paddedWidth, paddedHeight)));

            // Resize to original image size
            using Image<L16> finalFloatMask = croppedMask.Clone(context => context.Resize(originalWidth, originalHeight, _lanczosResampler));

            // Threshold to binary mask
            Image<L8> binaryMask = new Image<L8>(originalWidth, originalHeight);
            for (int y = 0; y < finalFloatMask.Height; y++)
            {
                for (int x = 0; x < finalFloatMask.Width; x++)
                {
                    float probability = finalFloatMask[x, y].PackedValue / (float)ushort.MaxValue;
                    byte value = probability > 0.5f ? (byte)255 : (byte)0;
                    binaryMask[x, y] = new L8(value);
                }
            }

            return binaryMask;
        }

        /// <summary>
        /// Asynchronously combines a list of binary masks into a single union mask, applies dilation and blurring,
        /// and saves the resulting mask as a PNG file to the specified output path.
        /// </summary>
        /// <param name="masks">
        /// A list of binary <see cref="Image{L8}"/> masks to be combined. Each mask must have identical dimensions.
        /// Non-zero pixels in any input mask will be set to 255 in the final combined mask.
        /// </param>
        /// <param name="outputPath">
        /// The file path where the final combined binary mask PNG will be saved. The directory must exist and the path
        /// must be writable.
        /// </param>
        /// <param name="dilationSizeInPixels">
        /// The radius, in pixels, by which to dilate the combined mask before applying median and Gaussian blurs.
        /// Defaults to <c>2</c>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous mask-combination and file-save operation.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <paramref name="masks"/> is null or empty.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if any mask in <paramref name="masks"/> does not match the dimensions of the first mask.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown if an I/O error occurs while writing the output PNG file to <paramref name="outputPath"/>.
        /// </exception>
        public async Task CombineListOfMasksAsync(List<Image<L8>> masks, string outputPath, int dilationSizeInPixels = 2)
        {
            if (masks == null || masks.Count == 0)
            {
                throw new ArgumentException("Mask list is empty.", nameof(masks));
            }

            int width = masks[0].Width;
            int height = masks[0].Height;

            using (Image<L8> combinedMask = new Image<L8>(width, height))
            {
                foreach (Image<L8> mask in masks)
                {
                    if (mask.Width != width || mask.Height != height)
                    {
                        throw new InvalidOperationException("All masks must have the same dimensions.");
                    }

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (mask[x, y].PackedValue > 0)
                            {
                                combinedMask[x, y] = new L8(255);
                            }
                        }
                    }
                }

                using (Image<L8> dilatedMask = DilateMask(combinedMask, dilationSizeInPixels))
                {
                    dilatedMask.Mutate(context => context.MedianBlur(3, true));
                    dilatedMask.Mutate(context => context.GaussianBlur(5.0f));
                    await dilatedMask.SaveAsPngAsync(outputPath, _pngEncoder);
                }
            }
        }

        /// <summary>
        /// Asynchronously resizes an image to fit within the specified dimensions, 
        /// maintaining aspect ratio and applying optional sharpening.
        /// </summary>
        /// <param name="inputPath">The file path of the source image to be resized.</param>
        /// <param name="outputPath">The directory where the resized image will be saved.</param>
        /// <param name="dimension">The target dimension to resize the longest side of the image to.</param>
        /// <returns>A task representing the asynchronous image resizing operation.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An error occurred while reading or writing image files.</exception>
        private async Task ResizeImageAsync(string inputPath, string outputPath, SupportedDimensions dimension)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputPath);

            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
            {
                double aspectRatio = image.Width / (double)image.Height;

                int canvasWidth, canvasHeight;
                int computedWidth, computedHeight;

                if (image.Width >= image.Height)
                {
                    computedWidth = (int)dimension;
                    computedHeight = (int)Math.Round(computedWidth / aspectRatio);
                    canvasWidth = computedWidth;
                    canvasHeight = RoundToNearestMultiple(computedHeight, 16);
                }
                else
                {
                    computedHeight = (int)dimension;
                    computedWidth = (int)Math.Round(computedHeight * aspectRatio);
                    canvasHeight = computedHeight;
                    canvasWidth = RoundToNearestMultiple(computedWidth, 16);
                }

                ResizeOptions resizeOptions = new ResizeOptions
                {
                    Size = new Size(canvasWidth, canvasHeight),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                    Sampler = _lanczosResampler,
                    Compand = true
                };

                image.Mutate(context => context.Resize(resizeOptions));

                if (ApplySharpen && (image.Width >= MinimumResolutionForSigma || image.Height >= MinimumResolutionForSigma))
                {
                    image.Mutate(context => context.GaussianSharpen(_sharpenSigma));
                }

                string outputFilePath = Path.ChangeExtension(Path.Combine(outputPath, fileName), ".png");
                await image.SaveAsPngAsync(outputFilePath, _pngEncoder);

            }
        }

        /// <summary>
        /// Rounds the provided value to the nearest multiple of the specified number.
        /// </summary>
        /// <param name="value">The value to round.</param>
        /// <param name="multiple">The multiple to round to.</param>
        /// <returns>The rounded value.</returns>
        private int RoundToNearestMultiple(int value, int multiple)
        {
            if (multiple == 0)
            {
                return value;
            }

            int remainder = value % multiple;
            if (remainder == 0)
            {
                return value;
            }

            int lowerMultiple = value - remainder;
            int upperMultiple = lowerMultiple + multiple;

            if (value - lowerMultiple < upperMultiple - value)
            {
                return lowerMultiple;
            }
            else
            {
                return upperMultiple;
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
        /// Extracts overlapping tiles from an image, using mirror padding for edge tiles.
        /// </summary>
        /// <param name="inputPath">The file path to the input image.</param>
        /// <param name="tileSize">The target size of each square tile. Defaults to 512.</param>
        /// <param name="overlap">The number of pixels that adjacent tiles should overlap. Defaults to 126.</param>
        /// <returns>An array of <see cref="TileImage"/> objects, each representing a tile ready for processing.</returns>
        /// <remarks>
        /// This method divides the source image into a grid of overlapping tiles. If a tile at an edge or corner is smaller 
        /// than the target <paramref name="tileSize"/>, it is padded by mirroring its own content to fill the remaining space.
        /// </remarks>
        private async Task<TileImage[]> ExtractTilesFromImage(string inputPath, int tileSize = 512, int overlap = 126)
        {
            List<TileImage> imageTiles = new List<TileImage>();
            using (Image<Rgba32> sourceImage = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
            {
                int step = tileSize - overlap;
                int imageWidth = sourceImage.Width;
                int imageHeight = sourceImage.Height;

                int numTilesX = (imageWidth <= tileSize) ? 1 : (int)Math.Ceiling((double)(imageWidth - tileSize) / step) + 1;
                int numTilesY = (imageHeight <= tileSize) ? 1 : (int)Math.Ceiling((double)(imageHeight - tileSize) / step) + 1;

                for (int yIdx = 0; yIdx < numTilesY; yIdx++)
                {
                    for (int xIdx = 0; xIdx < numTilesX; xIdx++)
                    {
                        int cropX = xIdx * step;
                        int cropY = yIdx * step;

                        if (xIdx == numTilesX - 1 && imageWidth > tileSize)
                        {
                            cropX = imageWidth - tileSize;
                        }
                        if (yIdx == numTilesY - 1 && imageHeight > tileSize)
                        {
                            cropY = imageHeight - tileSize;
                        }

                        Rectangle sourceCropArea = new Rectangle(cropX, cropY, Math.Min(tileSize, imageWidth - cropX), Math.Min(tileSize, imageHeight - cropY));

                        Image<Rgba32> tile = new Image<Rgba32>(sourceCropArea.Width, sourceCropArea.Height);
                        tile.Mutate(context => context.DrawImage(sourceImage, new Point(0, 0), sourceCropArea, 1f));

                        if (tile.Width < tileSize || tile.Height < tileSize)
                        {
                            Image<Rgba32> paddedImage = new Image<Rgba32>(tileSize, tileSize);
                            paddedImage.Mutate(context => context.DrawImage(tile, new Point(0, 0), 1f));

                            if (tile.Width < tileSize)
                            {
                                int padWidth = tileSize - tile.Width;
                                Rectangle mirrorRegion = new Rectangle(tile.Width - padWidth, 0, padWidth, tile.Height);
                                using (Image<Rgba32> mirrorX = tile.Clone(context => context.Crop(mirrorRegion).Flip(FlipMode.Horizontal)))
                                {
                                    paddedImage.Mutate(context => context.DrawImage(mirrorX, new Point(tile.Width, 0), 1f));
                                }
                            }

                            if (tile.Height < tileSize)
                            {
                                int padHeight = tileSize - tile.Height;
                                Rectangle regionToMirrorY = new Rectangle(0, tile.Height - padHeight, tileSize, padHeight);
                                using (Image<Rgba32> mirrorY = paddedImage.Clone(context => context.Crop(regionToMirrorY).Flip(FlipMode.Vertical)))
                                {
                                    paddedImage.Mutate(context => context.DrawImage(mirrorY, new Point(0, tile.Height), 1f));
                                }
                            }

                            tile.Dispose();
                            imageTiles.Add(new TileImage(paddedImage, yIdx, xIdx, sourceCropArea.X, sourceCropArea.Y));
                        }
                        else
                        {
                            imageTiles.Add(new TileImage(tile, yIdx, xIdx, sourceCropArea.X, sourceCropArea.Y));
                        }
                    }
                }
            }

            return imageTiles.ToArray();
        }

        /// <summary>
        /// Dilates the given binary mask by the specified radius (in pixels).
        /// That is, any pixel within <paramref name="radius"/> of a foreground pixel
        /// becomes foreground.
        /// </summary>
        private Image<L8> DilateMask(Image<L8> mask, int radius)
        {
            int w = mask.Width, h = mask.Height;

            byte[] src = new byte[w * h];
            mask.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < h; y++)
                {
                    Span<L8> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        src[y * w + x] = row[x].PackedValue;
                    }
                }
            });

            Image<L8> dilated = new Image<L8>(w, h);
            dilated.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < h; y++)
                {
                    Span<L8> row = accessor.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        byte maxVal = 0;

                        for (int dy = -radius; dy <= radius; dy++)
                            for (int dx = -radius; dx <= radius; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                                    maxVal = Math.Max(maxVal, src[ny * w + nx]);
                            }
                        row[x] = new L8(maxVal);
                    }
                }
            });

            return dilated;
        }

        #region Magick.NET
        private async Task<MagickImage> ConvertToMagickImageAsync(Image<Rgba32> image)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await image.SaveAsPngAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return new MagickImage(memoryStream);
            }
        }

        private async Task<Image<Rgba32>> ConvertToImageSharpAsync(MagickImage magickImage)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await magickImage.WriteAsync(memoryStream, MagickFormat.Bmp);
                memoryStream.Seek(0, SeekOrigin.Begin);
                return await Image.LoadAsync<Rgba32>(memoryStream);
            }
        }

        private void ApplyAutoCorrections(MagickImage magickImage, bool autoLevel, bool autoGamma)
        {
            if (autoLevel)
            {
                magickImage.AutoLevel();
            }
            if (autoGamma)
            {
                magickImage.AutoGamma();
            }
        }
        #endregion
    }
}
