using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    /// <summary>
    /// View model for the Tag Editor, responsible for managing image tags and text editing.
    /// </summary>
    public partial class TagEditorViewModel : ViewModelBase
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IImageProcessorService _imageProcessor;
        private readonly IInputHooksService _inputHooks;
        private Random _random;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private List<string> _imageFiles;
        [ObservableProperty]
        private string _totalImageFiles;
        [ObservableProperty]
        private int _selectedItemIndex;
        [ObservableProperty]
        private Bitmap _selectedImage;
        [ObservableProperty]
        private string _selectedImageFilename;
        [ObservableProperty]
        private string _wordsToHighlight;
        [ObservableProperty]
        private string _wordsToFilter;
        [ObservableProperty]
        private bool _isExactFilter;
        [ObservableProperty]
        private bool _buttonEnabled;
        [ObservableProperty]
        private string _currentAndTotal;
        [ObservableProperty]
        private bool _editingTxt;
        [ObservableProperty]
        private string _currentImageTags;

        private bool _showBlurredImage;
        private MemoryStream _currentImageMemoryStream = null;

        /// <summary>
        /// Gets the current type of file being edited, either .txt or .caption.
        /// </summary>
        public string CurrentType
        {
            get
            {
                if (EditingTxt)
                {
                    return ".txt";
                }
                else
                {
                    return ".caption";
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the TagEditorViewModel class.
        /// </summary>
        /// <param name="fileManipulator">The file manipulation service for file operations.</param>
        /// <param name="imageProcessor">The image processing service for image-related operations.</param>
        /// <param name="inputHooks">The input hooks service for managing user input.</param>
        /// <param name="logger">The logger service for logging messages.</param>
        /// <param name="configs">The configuration service for application settings.</param>
        public TagEditorViewModel(IFileManipulatorService fileManipulator, IImageProcessorService imageProcessor,
                IInputHooksService inputHooks, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _imageProcessor = imageProcessor;
            _inputHooks = inputHooks;
            _random = new Random();

            InputFolderPath = _configs.Configurations.CombinedOutputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);

            ButtonEnabled = true;
            IsExactFilter = false;

            _editingTxt = true;

            SelectedItemIndex = 0;

            _inputHooks.ButtonF1 += (sender, args) => OnNavigationButtonDown("-1");
            _inputHooks.ButtonF2 += (sender, args) => OnNavigationButtonDown("1");
            _inputHooks.ButtonF3 += (sender, args) => OnNavigationButtonDown("-10");
            _inputHooks.ButtonF4 += (sender, args) => OnNavigationButtonDown("10");
            _inputHooks.ButtonF5 += (sender, args) => OnNavigationButtonDown("-100");
            _inputHooks.ButtonF6 += (sender, args) => OnNavigationButtonDown("100");
            _inputHooks.ButtonF8 += async (sender, args) => await BlurImageAsync();

            _inputHooks.MouseButton3 += async (sender, args) => await BlurImageAsync();
            _inputHooks.MouseButton4 += (sender, args) => OnNavigationButtonDown("-1");
            _inputHooks.MouseButton5 += (sender, args) => OnNavigationButtonDown("1");

            _inputHooks.AltLeftArrowCombo += (sender, args) => OnNavigationButtonDown("-1");
            _inputHooks.AltRightArrowCombo += (sender, args) => OnNavigationButtonDown("1");
        }

        /// <summary>
        /// Updates the current selected image tags based on the selected image.
        /// </summary>
        public void UpdateCurrentSelectedTags()
        {
            if (SelectedImage != null)
            {
                try
                {
                    CurrentImageTags = _fileManipulator.GetTextFromFile(ImageFiles[SelectedItemIndex], CurrentType);
                }
                catch
                {
                    Logger.LatestLogMessage = $".txt or .caption file for current image not found, just type in the editor and one will be created!";
                    CurrentImageTags = string.Empty;
                }
            }
        }

        /// <summary>
        /// Navigates to a specific item in the image list.
        /// </summary>
        /// <param name="parameter">The navigation parameter indicating the item index.</param>
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
                Logger.LatestLogMessage = "Couldn't load the image.";
            }
        }

        /// <summary>
        /// Navigates to a random item in the image list.
        /// </summary>
        [RelayCommand]
        private void GoToRandomItem()
        {
            if (ImageFiles?.Count != 0 && ImageFiles != null)
            {
                SelectedItemIndex = _random.Next(0, ImageFiles.Count);
            }
        }

        /// <summary>
        /// Switches between editing .txt and .caption files and updates the view accordingly.
        /// </summary>
        [RelayCommand]
        private void SwitchEditorType()
        {
            EditingTxt = !EditingTxt;
            OnPropertyChanged(nameof(CurrentType));
        }

        /// <summary>
        /// Toggles the display of a blurred image for the currently selected image asynchronously.
        /// </summary>
        [RelayCommand]
        private async Task BlurImageAsync()
        {
            _showBlurredImage = !_showBlurredImage;
            try
            {
                if (_showBlurredImage)
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        MemoryStream imageMemoryStream = await _imageProcessor.GetBlurredImageAsync(ImageFiles[SelectedItemIndex]);
                        imageMemoryStream.Seek(0, SeekOrigin.Begin);
                        _currentImageMemoryStream?.Dispose();
                        MemoryStream imageMemoryStreamCopy = new MemoryStream(imageMemoryStream.ToArray());
                        SelectedImage = new Bitmap(imageMemoryStream);
                        await imageMemoryStream.DisposeAsync();
                    });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        SelectedImage = new Bitmap(ImageFiles[SelectedItemIndex]);
                    });
                }
            }
            catch
            {
                Logger.LatestLogMessage = $"Something went wrong while loading blurred image!";
            }
        }

        /// <summary>
        /// Asynchronously filters and loads image files based on specified filter criteria.
        /// </summary>
        [RelayCommand]
        private async Task FilterFilesAsync()
        {
            try
            {
                ButtonEnabled = false;
                List<string> searchResult = await Task.Run(() => _fileManipulator.GetFilteredImageFiles(InputFolderPath, CurrentType, WordsToFilter, IsExactFilter));
                if (searchResult.Count > 0)
                {
                    SelectedItemIndex = 0;
                    ImageFiles = searchResult;
                    ImageFiles = ImageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();
                }
                else
                {
                    Logger.LatestLogMessage = "No images found!";
                }
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(FileNotFoundException))
                {
                    Logger.LatestLogMessage = "No image files were found in the directory.";
                }
            }
            finally
            {
                if (ImageFiles.Count != 0)
                {
                    SelectedImage = new Bitmap(ImageFiles[SelectedItemIndex]);
                }
                ButtonEnabled = true;
            }
        }

        /// <summary>
        /// Clears the applied filter and reloads all images from the original input folder.
        /// </summary>
        [RelayCommand]
        private void ClearFilter()
        {
            if (!string.IsNullOrEmpty(InputFolderPath))
            {
                LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Selects an input folder and loads images from it.
        /// </summary>
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

        /// <summary>
        /// Copies the current image tags to the clipboard asynchronously.
        /// </summary>
        [RelayCommand]
        private async Task CopyCurrentImageTagsToClipboard()
        {
            await CopyToClipboard(CurrentImageTags);
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
                Logger.LatestLogMessage = "No image files were found in the directory.";
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
        /// Handles changes in the EditingTxt property to update the selected image tags.
        /// </summary>
        partial void OnEditingTxtChanged(bool value)
        {
            UpdateCurrentSelectedTags();
        }

        /// <summary>
        /// Handles changes in the ImageFiles property to update the total image files count.
        /// </summary>
        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
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
        partial void OnSelectedImageChanged(Bitmap value)
        {
            try
            {
                UpdateCurrentSelectedTags();
            }
            catch (Exception exception)
            {
                Logger.LatestLogMessage = $".txt or .caption file for current image not found, just type in the editor and one will be created!{Environment.NewLine}{exception.StackTrace}";
                CurrentImageTags = string.Empty;
            }
            finally
            {
                CurrentAndTotal = $"Currently viewing: {SelectedItemIndex + 1}/{ImageFiles?.Count}.";
                SelectedImageFilename = $"Current file: {Path.GetFileName(ImageFiles[SelectedItemIndex])}.";
            }
        }

        /// <summary>
        /// Handles changes in the CurrentImageTags property to save the updated tags to the selected image's file.
        /// </summary>
        partial void OnCurrentImageTagsChanged(string value)
        {
            try
            {
                string txtFile = Path.ChangeExtension(ImageFiles[SelectedItemIndex], CurrentType);
                _fileManipulator.SaveTextToFile(txtFile, CurrentImageTags);
            }
            catch (NullReferenceException)
            {
                Logger.LatestLogMessage = "You need to select a folder with image files!";
            }
        }
    }
}
