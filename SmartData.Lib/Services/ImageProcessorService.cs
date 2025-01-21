// Ignore Spelling: Metadata Lanczos

using HeyRed.ImageSharp.Heif.Formats.Avif;
using HeyRed.ImageSharp.Heif.Formats.Heif;

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

        private readonly JpegEncoder _jpegEncoder = new JpegEncoder()
        {
            ColorType = JpegEncodingColor.Rgb,
            Interleaved = true,
            Quality = 100,
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

                image.Mutate(image => image.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));

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

                image.Mutate(image => image.Resize(resizeOptions));

                if (ApplySharpen)
                {
                    image.Mutate(image => image.GaussianSharpen(_sharpenSigma));
                }

                image.Mutate(image => image.BackgroundColor(Color.White));
                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                await image.SaveAsJpegAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".jpeg"), _jpegEncoder);
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
            foreach (var file in files)
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

                image.Mutate(image => image.Resize(resizeOptions));

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

                image.Mutate(image => image.Resize(resizeOptions));

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

                image.Mutate(image => image.Resize(resizeOptions));

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
        /// <param name="inputImagePath">The path of the image to be processed.</param>
        /// <param name="inputMaskPath">The path of the mask to be processed.</param>
        /// <returns>An <see cref="LaMaInputData"/> object containing the processed image and mask as float arrays.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="inputImagePath"/> or <paramref name="inputMaskPath"/> does not exist.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurred while opening the file specified by <paramref name="inputImagePath"/> or <paramref name="inputMaskPath"/>.</exception>
        /// <exception cref="ArgumentException">The dimensions of the mask do not match the dimensions of the image.</exception>
        public async Task<LaMaInputData> ProcessImageForInpaintAsync(string inputImagePath, string inputMaskPath)
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
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputImagePath))
            {
                inputData.OriginalSize = new Point(image.Width, image.Height);

                imageSize = new Point(image.Width, image.Height);

                image.Mutate(image => image.Resize(resizeOptions));

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

                image.Mutate(image => image.Resize(resizeOptions));

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
        /// Processes the input image and mask for tile-based inpainting, and returns an array of <see cref="TileData"/>.
        /// </summary>
        /// <param name="inputImagePath">The file path to the input image.</param>
        /// <param name="inputMaskPath">The file path to the input mask.</param>
        /// <param name="tileSize">The size of each tile in pixels. Default is 512.</param>
        /// <returns>An array of <see cref="TileData"/> containing the processed image and mask data for each tile.</returns>
        /// <exception cref="ArgumentException">Thrown when the input image and mask do not have the same size or the number of tiles do not match.</exception>
        /// <remarks>
        /// This method splits the input image and mask into tiles of the specified size, processes each tile to extract the pixel data,
        /// and creates an array of <see cref="TileData"/> containing the processed data for each tile. The tiles are then returned as an array.
        /// </remarks>
        public async Task<TileData[]> ProcessImageForTileInpaintAsync(string inputImagePath, string inputMaskPath, int tileSize = 512)
        {
            System.Drawing.Size imageSize = await GetImageSizeAsync(inputImagePath);
            System.Drawing.Size maskSize = await GetImageSizeAsync(inputMaskPath);
            if (!imageSize.Equals(maskSize))
            {
                throw new ArgumentException("Image and Mask must be same size!");
            }

            TileImage[] imageTiles = await ExtractTilesFromImage(inputImagePath, tileSize);
            TileImage[] maskTiles = await ExtractTilesFromImage(inputMaskPath, tileSize);
            if (imageTiles.Length != maskTiles.Length)
            {
                throw new ArgumentException("The number of Image Tiles and Mask Tiles isn't the same! The number of tiles must be the same!");
            }

            List<TileData> tiles = new List<TileData>(imageTiles.Length);
            for (int i = 0; i < imageTiles.Length; i++)
            {
                LaMaInputData inputData = new LaMaInputData()
                {
                    InputImage = new DenseTensor<float>(new[] { 1, 3, 512, 512 }),
                    InputMask = new DenseTensor<float>(new[] { 1, 1, 512, 512 }),
                    OriginalSize = new Point(imageSize.Width, imageSize.Height)
                };

                // Process image data
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

                // Process mask data
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

                TileData tile = new TileData(inputData, imageTiles[i].RowIndex, imageTiles[i].ColumnIndex);
                tiles.Add(tile);
            }

            return tiles.ToArray();
        }

        /// <summary>
        /// Extracts tiles from the input image with the specified tile size.
        /// </summary>
        /// <param name="inputImagePath">The file path to the input image.</param>
        /// <param name="tileSize">The size of each tile in pixels.</param>
        /// <returns>An array of <see cref="TileImage"/> containing the extracted image tiles.</returns>
        /// <remarks>
        /// This method splits the input image into tiles of the specified size and resizes each tile to the specified tile size.
        /// Each tile is then returned as a <see cref="TileImage"/> object containing the image data and tile indices.
        /// </remarks>
        private async Task<TileImage[]> ExtractTilesFromImage(string inputImagePath, int tileSize)
        {
            ResizeOptions resizeOptions = new ResizeOptions()
            {
                Mode = ResizeMode.BoxPad,
                Position = AnchorPositionMode.TopLeft,
                Compand = true,
                Size = new Size(tileSize, tileSize),
            };

            int rows;
            int columns;
            List<TileImage> imageTiles = new List<TileImage>();
            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputImagePath))
            {
                rows = (int)Math.Ceiling(image.Height / (float)tileSize);
                columns = (int)Math.Ceiling(image.Width / (float)tileSize);

                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        int cropX = x * tileSize;
                        int cropY = y * tileSize;
                        int cropWidth;
                        if (x == columns - 1)
                        {
                            cropWidth = image.Width - cropX;
                        }
                        else
                        {
                            cropWidth = tileSize;
                        }
                        int cropHeight;
                        if (y == rows - 1)
                        {
                            cropHeight = image.Height - cropY;
                        }
                        else
                        {
                            cropHeight = tileSize;
                        }

                        Image<Rgba32> cloneImage = image.Clone();
                        Rectangle cropArea = new Rectangle(cropX, cropY, cropWidth, cropHeight);
                        cloneImage.Mutate(image => image.Crop(cropArea));

                        if (cropWidth < tileSize || cropHeight < tileSize)
                        {
                            Image<Rgba32> paddedImage = new Image<Rgba32>(tileSize, tileSize);
                            paddedImage.Mutate(ctx => ctx.DrawImage(cloneImage, new Point(0, 0), 1f));

                            // Stretch the last column to the right
                            if (cropWidth < tileSize)
                            {
                                for (int stretchX = cropWidth; stretchX < tileSize; stretchX++)
                                {
                                    for (int stretchY = 0; stretchY < cropHeight; stretchY++)
                                    {
                                        paddedImage[stretchX, stretchY] = paddedImage[cropWidth - 1, stretchY];
                                    }
                                }
                            }

                            // Stretch the last row to the bottom
                            if (cropHeight < tileSize)
                            {
                                for (int stretchY = cropHeight; stretchY < tileSize; stretchY++)
                                {
                                    for (int stretchX = 0; stretchX < tileSize; stretchX++)
                                    {
                                        paddedImage[stretchX, stretchY] = paddedImage[stretchX, cropHeight - 1];
                                    }
                                }
                            }

                            imageTiles.Add(new TileImage(paddedImage, x, y));
                        }
                        else
                        {
                            cloneImage.Mutate(img => img.Resize(tileSize, tileSize));
                            imageTiles.Add(new TileImage(cloneImage, x, y));
                        }
                    }
                }
            }

            return imageTiles.ToArray();
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

                image.Mutate(image => image.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));
                image.Mutate(image => image.Resize(inputData.OriginalSize.X, inputData.OriginalSize.Y, KnownResamplers.Lanczos3));

                image.SaveAsPng(outputPath);
            }
        }

        /// <summary>
        /// Saves the inpainted image to the specified output path by combining the processed image tiles.
        /// </summary>
        /// <param name="outputPath">The file path where the resulting inpainted image will be saved.</param>
        /// <param name="inputData">An array of <see cref="TileData"/> representing the input data of the image tiles.</param>
        /// <param name="outputData">An array of <see cref="LaMaOutputData"/> containing the output data of the inpainted image tiles.</param>
        /// <param name="tileSize">The size of each tile in pixels. Default is 512.</param>
        /// <remarks>
        /// This method creates a new image by assembling the inpainted tiles from the <paramref name="outputData"/> array.
        /// Each tile's pixel data is processed and placed in the appropriate position in the resulting image based on the row and column indices.
        /// The assembled image is then saved as a PNG file at the specified <paramref name="outputPath"/>.
        /// </remarks>
        public void SaveInpaintedImage(string outputPath, TileData[] inputData, LaMaOutputData[] outputData, int tileSize = 512)
        {
            using (Image<Rgba32> resultImage = new Image<Rgba32>(inputData[0].LaMaInputData.OriginalSize.X,
                inputData[0].LaMaInputData.OriginalSize.Y))
            {
                for (int i = 0; i < outputData.Length; i++)
                {
                    int width = outputData[i].OutputImage.Dimensions[2];
                    int height = outputData[i].OutputImage.Dimensions[3];

                    byte[] imageByte = new byte[3 * width * height];
                    for (int row = 0; row < height; row++)
                    {
                        for (int col = 0; col < width; col++)
                        {
                            int baseIndex = (row * width + col) * 3;
                            byte r = (byte)Math.Clamp(outputData[i].OutputImage[0, 2, row, col], 0, 255);
                            byte g = (byte)Math.Clamp(outputData[i].OutputImage[0, 1, row, col], 0, 255);
                            byte b = (byte)Math.Clamp(outputData[i].OutputImage[0, 0, row, col], 0, 255);

                            imageByte[baseIndex] = b;
                            imageByte[baseIndex + 1] = g;
                            imageByte[baseIndex + 2] = r;
                        }
                    }

                    ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(imageByte);

                    Image<Rgb24> tile = Image.LoadPixelData<Rgb24>(bytes, width, height);
                    Point location = new Point(tileSize * outputData[i].RowIndex, tileSize * outputData[i].ColumnIndex);
                    resultImage.Mutate(image => image.DrawImage(tile, location, 1));
                }

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
                image.Mutate(image => image.GaussianBlur(_blurRadius));

                MemoryStream blurredImageStream = new MemoryStream();
                image.Mutate(image => image.BackgroundColor(Color.White));
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

                image.Mutate(image => image.Crop(cropArea));

                image.Mutate(image => image.BackgroundColor(Color.White));
                string fileName = Path.GetFileNameWithoutExtension(inputPath);
                await image.SaveAsJpegAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".jpeg"), _jpegEncoder);
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
                image.Mutate(image => image.BackgroundColor(Color.Black));

                MemoryStream imageMaskStream = new MemoryStream();
                image.SaveAsJpeg(imageMaskStream, _jpegEncoder);

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
                image.Mutate(image => image.Fill(brush, circle));

                MemoryStream imageMaskStream = new MemoryStream();
                image.SaveAsJpeg(imageMaskStream, _jpegEncoder);

                return imageMaskStream;
            }
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

            using (Image<Rgba32> image = await Image.LoadAsync<Rgba32>(_decoderOptions, inputPath))
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

                image.Mutate(image => image.Resize(resizeOptions));

                if (ApplySharpen && (originalWidth >= MinimumResolutionForSigma || originalHeight >= MinimumResolutionForSigma))
                {
                    image.Mutate(image => image.GaussianSharpen(_sharpenSigma));
                }

                image.Mutate(image => image.BackgroundColor(Color.White));
                await image.SaveAsJpegAsync(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".jpeg"), _jpegEncoder);
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
