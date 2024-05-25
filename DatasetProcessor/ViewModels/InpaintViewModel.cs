using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class InpaintViewModel : BaseViewModel
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IFileManipulatorService _fileManipulator;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
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
        private MemoryStream _selectedImageMaskBitmap;
        [ObservableProperty]
        private string _selectedImageFilename;
        [ObservableProperty]
        private string _currentAndTotal;
        [ObservableProperty]
        private Point _imageSize;
        private bool _imageWasDownscaled;

        [ObservableProperty]
        private bool _buttonEnabled;
        [ObservableProperty]
        private bool _isUiEnabled;

        [ObservableProperty]
        private Point _circlePosition;

        [ObservableProperty]
        float _circleRadius;
        public float CircleWidthHeight
        {
            get => CircleRadius * 2.0f;
        }

        [ObservableProperty]
        Color _drawingColor;

        public InpaintViewModel(IImageProcessorService imageProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManipulator = fileManipulator;

            ButtonEnabled = true;
            IsUiEnabled = true;

            SelectedItemIndex = 0;

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;
            ImageFiles = new List<string>();
            CurrentAndTotal = string.Empty;
            SelectedImageFilename = string.Empty;
            TotalImageFiles = string.Empty;

            CircleRadius = 15.0f;
        }

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

        [RelayCommand]
        private void GoToItem(string parameter)
        {
            try
            {
                int.TryParse(parameter, out int parameterInt);

                if (ImageFiles?.Count != 0)
                {
                    SelectedItemIndex += parameterInt;
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("Couldn't load the image.", LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Loads image files from the specified input folder and prepares the view model for editing.
        /// </summary>
        private void LoadImagesFromInputFolder()
        {
            try
            {
                ImageFiles = _fileManipulator.GetImageFiles(InputFolderPath);
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
                SelectedImage = new Bitmap((ImageFiles[SelectedItemIndex]));
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

            double widthScale = 1024.0 / value.Size.Width;
            double heightScale = 512.0 / value.Size.Height;
            double scaleFactor = Math.Min(widthScale, heightScale);

            if (scaleFactor < 1.0)
            {
                ImageSize = new Point((int)(value.Size.Width * scaleFactor), (int)(value.Size.Height * scaleFactor));
                _imageWasDownscaled = true;
            }
            else
            {
                ImageSize = new Point((int)value.Size.Width, (int)value.Size.Height);
                _imageWasDownscaled = false;
            }

            _selectedImageMaskBitmap = _imageProcessor.CreateImageMask((int)SelectedImage.Size.Width,
                (int)SelectedImage.Size.Height);
            _selectedImageMaskBitmap.Seek(0, SeekOrigin.Begin);
            SelectedImageMask = new Bitmap(_selectedImageMaskBitmap);
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

        partial void OnCirclePositionChanged(Point value)
        {
            if (SelectedImage == null)
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

                if (_imageWasDownscaled)
                {
                    // Create new variables so the program doesn't enter in an infinite loop calling OnEndingPositionChanged.
                    Point position = new Point((int)(value.X / (ImageSize.X / (float)SelectedImage.Size.Width)),
                        (int)(value.Y / (ImageSize.Y / (float)SelectedImage.Size.Height)));

                    _selectedImageMaskBitmap = _imageProcessor.DrawCircleOnMask(_selectedImageMaskBitmap,
                        new SixLabors.ImageSharp.Point(position.X, position.Y), CircleRadius, color);
                }
                else
                {
                    _selectedImageMaskBitmap = _imageProcessor.DrawCircleOnMask(_selectedImageMaskBitmap,
                        new SixLabors.ImageSharp.Point(value.X, value.Y), CircleRadius, color);
                }

                _selectedImageMaskBitmap.Seek(0, SeekOrigin.Begin);
                SelectedImageMask = new Bitmap(_selectedImageMaskBitmap);
            }
            catch (ArgumentOutOfRangeException)
            {
                Logger.SetLatestLogMessage("An error occured while trying to crop the image. Be sure the crop area is bigger than 0 pixels in both Width and Height!",
                    LogMessageColor.Warning);
            }
        }

        /// <summary>
        /// Handles a button down event and navigates to the item with the specified index.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <param name="index">The index of the item to navigate to.</param>
        private void OnNavigationButtonDown(string index)
        {
            if (IsActive)
            {
                Dispatcher.UIThread.InvokeAsync(() => GoToItem(index));
            }
        }
    }
}
