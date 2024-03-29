﻿using Avalonia.Media.Imaging;
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

        public ManualCropViewModel(IImageProcessorService imageProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManipulator = fileManipulator;

            ButtonEnabled = true;
            IsUiEnabled = true;

            SelectedItemIndex = 0;

            StartingPosition = Point.Empty;
            EndingPosition = Point.Empty;

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;
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
            if (value != null)
            {
                if (value.Size.Width > 1024 || value.Size.Height > 1024)
                {
                    // If the image is too big, downscale it to fit on screen. Temporary solution
                    ImageSize = new Point((int)value.Size.Width / 2, (int)value.Size.Height / 2);
                    _imageWasDownscaled = true;
                }
                else
                {
                    ImageSize = new Point((int)value.Size.Width, (int)value.Size.Height);
                    _imageWasDownscaled = false;
                }
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
                        // Upscale the area by 2. TODO: Should probably look for a better solution.
                        Point startingPosition = new Point(StartingPosition.X * 2, StartingPosition.Y * 2);
                        Point endingPosition = new Point(EndingPosition.X * 2, EndingPosition.Y * 2);
                        await _imageProcessor.CropImageAsync(ImageFiles[SelectedItemIndex], OutputFolderPath, startingPosition, endingPosition);
                    }
                    else
                    {
                        await _imageProcessor.CropImageAsync(ImageFiles[SelectedItemIndex], OutputFolderPath, StartingPosition, EndingPosition);
                    }
                }
                catch (Exception exception)
                {
                    if (exception.GetType() == typeof(ArgumentOutOfRangeException))
                    {
                        Logger.SetLatestLogMessage("An error occured while trying to crop the image. Be sure the crop area is bigger than 0 pixels in both Width and Height!",
                            LogMessageColor.Warning);
                    }
                }
            });
        }
    }
}
