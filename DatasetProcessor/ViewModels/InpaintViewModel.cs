using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace DatasetProcessor.ViewModels
{
    public partial class InpaintViewModel : BaseViewModel
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IInpaintService _inpaint;
        private readonly IFileManagerService _fileManager;
        private readonly IModelManagerService _modelManager;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _inpaintingProgress;
        [ObservableProperty]
        private List<string> _imageFiles;
        [ObservableProperty]
        private string _totalImageFiles;
        [ObservableProperty]
        private int _selectedItemIndex;
        [ObservableProperty]
        private Bitmap? _selectedImage;
        [ObservableProperty]
        private Bitmap? _selectedImageMask;
        [ObservableProperty]
        private string _selectedImageFilename;
        [ObservableProperty]
        private string _currentAndTotal;
        [ObservableProperty]
        private Point _imageSize;
        private Size _originalImageSize;
        private double _scaleFactor;
        private bool _imageWasDownscaled;

        private WriteableBitmap? _writeableMaskBitmap;
        private byte[]? _pixelBuffer;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;
        [ObservableProperty]
        private bool _inpaintCurrentButtonEnabled;

        private Image<Rgba32> _rawMaskImage;

        [ObservableProperty]
        private Point _circlePosition;

        [ObservableProperty]
        private double _circleRadius;

        [ObservableProperty]
        private double _brushHardness = 1.0f;
        public string BrushHardnessString
        {
            get => $"{Math.Round(BrushHardness * 100, 2)}%";
        }
        public double CircleWidthHeight
        {
            get => CircleRadius * 2.0d;
        }
        [ObservableProperty]
        private double _maskOpacity;
        public string MaskOpacityString
        {
            get => $"{Math.Round(MaskOpacity * 100, 2)}%";
        }

        [ObservableProperty]
        Color _drawingColor;

        public InpaintViewModel(IImageProcessorService imageProcessor, IInpaintService inpaintService, IModelManagerService modelManager,
            IFileManagerService fileManager, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManager = fileManager;
            _inpaint = inpaintService;
            _modelManager = modelManager;

            (_inpaint as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                InpaintingProgress = ResetProgress(InpaintingProgress);
                InpaintingProgress.TotalFiles = args;
            };
            (_inpaint as INotifyProgress).ProgressUpdated += (sender, args) => InpaintingProgress.UpdateProgress();

            SelectedItemIndex = 0;

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;
            ImageFiles = new List<string>();
            CurrentAndTotal = string.Empty;
            SelectedImageFilename = string.Empty;
            TotalImageFiles = string.Empty;

            CircleRadius = 15.0f;
            MaskOpacity = 0.75f;

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
            IsUiEnabled = true;
            InpaintCurrentButtonEnabled = true;
        }

        /// <summary>
        /// Saves the current image mask to a file with a "_mask" suffix in the same directory as the current image file.
        /// </summary>
        public void SaveCurrentImageMask()
        {
            if (_rawMaskImage == null)
            {
                return;
            }
            try
            {
                string maskPath = GetCurrentFileMaskFilename();
                string directory = Path.GetDirectoryName(maskPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _rawMaskImage.SaveAsJpeg(maskPath);
            }
            catch (Exception ex)
            {
                Logger.SetLatestLogMessage($"An error occurred while saving the mask image: {ex.Message}", LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Selects an input folder for image files to be processed.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                LoadImagesFromInputFolder();
            }
        }

        [RelayCommand]
        private async Task SelectOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        /// <summary>
        /// Navigates to a specific item in the image list.
        /// </summary>
        /// <param name="parameter">The navigation parameter indicating the item index.</param>
        [RelayCommand]
        private void GoToItem(string parameter)
        {
            if (!int.TryParse(parameter, out int parameterInt))
            {
                return;
            }

            if (ImageFiles?.Count == 0)
            {
                return;
            }

            try
            {
                SelectedItemIndex += parameterInt;
            }
            catch
            {
                Logger.SetLatestLogMessage("An error occurred while loading the image.", LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Inpaints the currently selected image using the LaMa model.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task InpaintCurrentImageAsync()
        {
            IsUiEnabled = false;
            InpaintCurrentButtonEnabled = false;

            try
            {
                Logger.SetLatestLogMessage("Inpaiting current selected image...", LogMessageColor.Informational);
                await DownloadModelFiles(_modelManager, AvailableModels.LaMa);

                string imageFilename = Path.GetFileNameWithoutExtension(ImageFiles[SelectedItemIndex]);
                await _inpaint.InpaintImageTilesAsync(ImageFiles[SelectedItemIndex], GetCurrentFileMaskFilename(),
                    Path.Combine(OutputFolderPath, $"{imageFilename}.png"));
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                    LogMessageColor.Error);
                await Logger.SaveExceptionStackTraceAsync(exception);
            }
            finally
            {
                IsUiEnabled = true;
                InpaintCurrentButtonEnabled = true;
                Logger.SetLatestLogMessage("Finished inpaiting current selected image! Check output folder for the result.", LogMessageColor.Informational);
            }
        }

        /// <summary>
        /// Inpaints all images in the input folder using the LaMa model.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task InpaintingImagesAsync()
        {
            IsUiEnabled = false;
            InpaintCurrentButtonEnabled = false;

            _timer.Restart();
            DispatcherTimer uiTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            uiTimer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            uiTimer.Start();

            InpaintingProgress = ResetProgress(InpaintingProgress);
            TaskStatus = ProcessingStatus.Running;

            try
            {
                IsCancelEnabled = true;
                await DownloadModelFiles(_modelManager, AvailableModels.LaMa);
                await _inpaint.InpaintImagesAsync(InputFolderPath, OutputFolderPath);
            }
            catch (OperationCanceledException)
            {
                IsCancelEnabled = false;
                Logger.SetLatestLogMessage($"Cancelled the current operation!", LogMessageColor.Informational);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                await Logger.SaveExceptionStackTraceAsync(exception);
            }
            finally
            {
                IsCancelEnabled = false;
                IsUiEnabled = true;
                InpaintCurrentButtonEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }

            _timer.Stop();
        }

        /// <summary>
        /// Cancels the current inpainting task if it is running.
        /// </summary>
        [RelayCommand]
        private void CancelTask()
        {
            (_inpaint as ICancellableService)?.CancelCurrentTask();
        }

        /// <summary>
        /// Loads image files from the specified input folder and prepares the view model for editing.
        /// </summary>
        private void LoadImagesFromInputFolder()
        {
            try
            {
                ImageFiles = _fileManager.GetImageFiles(InputFolderPath)
                    .Where(x => !x.Contains("_mask")).ToList();
                if (ImageFiles.Count != 0)
                {
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                    SelectedItemIndex = 0;
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = new Bitmap(ImageFiles[SelectedItemIndex]);
                }
            }
        }

        /// <summary>
        /// Handles changes in the SelectedItemIndex property to ensure it stays within the valid range.
        /// </summary>
        partial void OnSelectedItemIndexChanged(int value)
        {
            if (ImageFiles?.Count > 0)
            {
                SelectedItemIndex = Math.Clamp(value, 0, ImageFiles.Count - 1);
                string path = ImageFiles[SelectedItemIndex];
                Task.Run(() =>
                {
                    Bitmap bitmap = new Bitmap(path);
                    Dispatcher.UIThread.Post(() => SelectedImage = bitmap);
                });
            }
            else
            {
                SelectedItemIndex = 0;
            }
        }

        /// <summary>
        /// Handles changes in the SelectedImage property to update the current selected image tags.
        /// </summary>
        partial void OnSelectedImageChanged(Bitmap? value)
        {
            CurrentAndTotal = $"Currently viewing: {SelectedItemIndex + 1}/{ImageFiles?.Count}.";
            SelectedImageFilename = $"Current file: {Path.GetFileName(ImageFiles?[SelectedItemIndex])}.";
            if (value == null)
            {
                return;
            }

            _originalImageSize = new Size(value.PixelSize.Width, value.PixelSize.Height);

            double widthScale = 768.0f / value.Size.Width;
            double heightScale = 768.0f / value.Size.Height;
            _scaleFactor = Math.Min(widthScale, heightScale);

            if (_scaleFactor < 1.0f)
            {
                ImageSize = new Point((int)(value.Size.Width * _scaleFactor), (int)(value.Size.Height * _scaleFactor));
                _imageWasDownscaled = true;
            }
            else
            {
                ImageSize = new Point((int)value.Size.Width, (int)value.Size.Height);
                _imageWasDownscaled = false;
                _scaleFactor = 1.0f;
            }

            _rawMaskImage?.Dispose();
            _writeableMaskBitmap?.Dispose();
            _writeableMaskBitmap = null;
            _pixelBuffer = null;

            string maskPath = GetCurrentFileMaskFilename();
            if (File.Exists(maskPath))
            {
                _rawMaskImage = Image.Load<Rgba32>(maskPath);
                if (_rawMaskImage.Width != _originalImageSize.Width || _rawMaskImage.Height != _originalImageSize.Height)
                {
                    _rawMaskImage.Mutate(x => x.Resize(_originalImageSize.Width, _originalImageSize.Height));
                }
            }
            else
            {
                _rawMaskImage = new Image<Rgba32>(_originalImageSize.Width, _originalImageSize.Height);
                _rawMaskImage.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.Black));
            }

            UpdateMaskDisplay();
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

        /// <summary>
        /// Handles changes in the CirclePosition property.
        /// </summary>
        partial void OnCirclePositionChanged(Point value)
        {
            if (SelectedImage == null || _rawMaskImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(OutputFolderPath))
            {
                Logger.SetLatestLogMessage("You need to first select a folder for the output files! Image wont be saved.",
                    LogMessageColor.Warning);
                return;
            }

            try
            {
                SixLabors.ImageSharp.Color color;
                if (DrawingColor.Equals(Color.White))
                {
                    color = SixLabors.ImageSharp.Color.White;
                }
                else if (DrawingColor.Equals(Color.Black))
                {
                    color = SixLabors.ImageSharp.Color.Black;
                }
                else
                {
                    return;
                }

                float x;
                float y;
                float radius;

                if (_imageWasDownscaled)
                {
                    float scaleX = (float)ImageSize.X / (float)_originalImageSize.Width;
                    float scaleY = (float)ImageSize.Y / (float)_originalImageSize.Height;

                    x = value.X / scaleX;
                    y = value.Y / scaleY;
                    radius = (float)(CircleRadius / _scaleFactor);
                }
                else
                {
                    x = value.X;
                    y = value.Y;
                    radius = (float)CircleRadius;
                }

                _imageProcessor.DrawCircleOnInMemoryMask(_rawMaskImage, new SixLabors.ImageSharp.Point((int)x, (int)y), radius, color, (float)BrushHardness);

                UpdateMaskDisplay();
            }
            catch (ArgumentOutOfRangeException)
            {
                Logger.SetLatestLogMessage("An error occured while trying to crop the image. Be sure the crop area is bigger than 0 pixels in both Width and Height!",
                    LogMessageColor.Warning);
            }
        }

        /// <summary>
        /// Handles changes in the CircleRadius property.
        /// </summary>
        partial void OnCircleRadiusChanged(double value)
        {
            CircleRadius = Math.Round(value, 2);
            OnPropertyChanged(nameof(CircleWidthHeight));
        }

        /// <summary>
        /// Handles changes to the brush hardness by rounding the value and updating the property.
        /// </summary>
        /// <param name="value">The new brush hardness value.</param>
        partial void OnBrushHardnessChanged(double value)
        {
            BrushHardness = Math.Round(value, 2);
            OnPropertyChanged(nameof(BrushHardness));
            OnPropertyChanged(nameof(BrushHardnessString));
        }

        /// <summary>
        /// Handles changes in the MaskOpacity property.
        /// </summary>
        /// <param name="value"></param>
        partial void OnMaskOpacityChanged(double value)
        {
            MaskOpacity = Math.Round(value, 2);
            OnPropertyChanged(nameof(MaskOpacityString));
        }

        /// <summary>
        /// Generates the output filename for the current image file with a "_mask" suffix and a ".jpeg" extension.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> representing the full path of the output file, which is the same directory as the current image file
        /// and has the same name with a "_mask" suffix and a ".jpeg" extension.
        /// </returns>
        /// <remarks>
        /// This method retrieves the directory and the filename (without extension) of the currently selected image file in the <see cref="ImageFiles"/> collection. 
        /// It then combines them to create a new filename with a "_mask.jpeg" suffix and returns the full path.
        /// </remarks>
        private string GetCurrentFileMaskFilename()
        {
            string outputFolder = Path.GetDirectoryName(ImageFiles[SelectedItemIndex]);
            string filename = Path.GetFileNameWithoutExtension(ImageFiles[SelectedItemIndex]);

            string masksPath = Path.Combine(outputFolder, "masks");

            if (!Directory.Exists(masksPath))
            {
                Directory.CreateDirectory(masksPath);
            }
            return Path.Combine(masksPath, Path.Combine($"{filename}_mask.jpeg"));
        }

        private void UpdateMaskDisplay()
        {
            if (_rawMaskImage == null)
            {
                return;
            }

            int width = _rawMaskImage.Width;
            int height = _rawMaskImage.Height;

            if (_writeableMaskBitmap == null || _writeableMaskBitmap.PixelSize.Width != width || _writeableMaskBitmap.PixelSize.Height != height)
            {
                _writeableMaskBitmap?.Dispose();
                _writeableMaskBitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);

                SelectedImageMask = _writeableMaskBitmap;

                int requiredBytes = width * height * 4;
                if (_pixelBuffer == null || _pixelBuffer.Length != requiredBytes)
                {
                    _pixelBuffer = new byte[requiredBytes];
                }
            }

            Span<Rgba32> targetSpan = MemoryMarshal.Cast<byte, Rgba32>(new Span<byte>(_pixelBuffer));
            _rawMaskImage.CopyPixelDataTo(targetSpan);

            using (var frameBuffer = _writeableMaskBitmap.Lock())
            {
                Marshal.Copy(_pixelBuffer!, 0, frameBuffer.Address, _pixelBuffer!.Length);
            }
            OnPropertyChanged(nameof(SelectedImageMask));
        }
    }
}
