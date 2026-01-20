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
    public partial class ManualCropViewModel : BaseViewModel
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IFileManagerService _fileManager;

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
        private Point _startingPosition;
        [ObservableProperty]
        private Point _endingPosition;

        public ManualCropViewModel(IImageProcessorService imageProcessor, IFileManagerService fileManager,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManager = fileManager;

            ButtonEnabled = true;
            IsUiEnabled = true;

            SelectedItemIndex = 0;

            StartingPosition = Point.Empty;
            EndingPosition = Point.Empty;

            InputFolderPath = _configs.Configurations.ManualCropConfigs.InputFolder;
            OutputFolderPath = _configs.Configurations.ManualCropConfigs.OutputFolder;
            ImageFiles = new List<string>();
            CurrentAndTotal = string.Empty;
            SelectedImageFilename = string.Empty;
            TotalImageFiles = string.Empty;
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
                Logger.SetLatestLogMessage("An unexpected error occurred while processing the image.", LogMessageColor.Error);
            }
        }

        [RelayCommand]
        private void CopyCurrentImage()
        {
            if (!string.IsNullOrEmpty(SelectedImageFilename) && !string.IsNullOrEmpty(OutputFolderPath))
            {
                string currentImage = ImageFiles[SelectedItemIndex];
                string outputPath = Path.Combine(OutputFolderPath, Path.GetFileName(currentImage));
                File.Copy(currentImage, outputPath);
            }
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
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x)))
                        .ToList();
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
                    var bitmap = new Bitmap(path);
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
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

        /// <summary>
        /// Handles the change event of the ending position of the crop region.
        /// </summary>
        /// <param name="value">The new ending position of the crop region.</param>
        /// <remarks>
        /// This method is triggered when the ending position of the crop region changes.
        /// It checks if an image is selected and if an output folder path is specified.
        /// If both conditions are met, it asynchronously crops the image based on the specified crop region
        /// (defined by the starting and ending positions) and saves the cropped image to the output folder.
        /// If no output folder is specified, it logs a message indicating that an output folder needs to be selected.
        /// If an error occurs during the cropping process, it throws an exception.
        /// </remarks>
        partial void OnEndingPositionChanged(Point value)
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

            Task.Run(async () =>
            {
                try
                {
                    if (_imageWasDownscaled)
                    {
                        // Create new variables so the program doesn't enter in an infinite loop calling OnEndingPositionChanged.
                        Point startingPosition = new Point((int)(StartingPosition.X / (ImageSize.X / (float)SelectedImage.Size.Width)),
                            (int)(StartingPosition.Y / (ImageSize.Y / (float)SelectedImage.Size.Height)));
                        Point endingPosition = new Point((int)(EndingPosition.X / (ImageSize.X / (float)SelectedImage.Size.Width)),
                            (int)(EndingPosition.Y / (ImageSize.Y / (float)SelectedImage.Size.Height)));

                        await _imageProcessor.CropImageAsync(ImageFiles[SelectedItemIndex], OutputFolderPath, startingPosition, endingPosition);
                    }
                    else
                    {
                        await _imageProcessor.CropImageAsync(ImageFiles[SelectedItemIndex], OutputFolderPath, StartingPosition, EndingPosition);
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    Logger.SetLatestLogMessage("An error occured while trying to crop the image. Be sure the crop area is bigger than 0 pixels in both Width and Height!",
                        LogMessageColor.Warning);
                }
            });
        }
    }
}
