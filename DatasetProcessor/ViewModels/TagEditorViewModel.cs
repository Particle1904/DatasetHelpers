using Avalonia.Media.Imaging;

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

        private bool _showBlurredImage;
        private MemoryStream _currentImageMemoryStream = null;
        [ObservableProperty]
        private bool _editingTxt;

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

        [ObservableProperty]
        private string _currentImageTags;

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
        }

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

        partial void OnEditingTxtChanged(bool value)
        {
            UpdateCurrentSelectedTags();
        }

        partial void OnImageFilesChanged(List<string> value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Count.ToString()}.";
        }

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

        partial void OnCurrentImageTagsChanged(string value)
        {
            string txtFile = Path.ChangeExtension(ImageFiles[SelectedItemIndex], CurrentType);
            _fileManipulator.SaveTextToFile(txtFile, CurrentImageTags);
        }

        [RelayCommand]
        public async Task SelectInputFolder()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                LoadImagesFromInputFolder();
            }
        }

        [RelayCommand]
        public void GoToItem(string parameter)
        {
            int.TryParse(parameter, out int parameterInt);

            if (ImageFiles?.Count != 0)
            {
                SelectedItemIndex += parameterInt;
            }
        }

        [RelayCommand]
        public void GoToRandomItem()
        {
            if (ImageFiles?.Count != 0 && ImageFiles != null)
            {
                SelectedItemIndex = _random.Next(0, ImageFiles.Count);
            }
        }

        [RelayCommand]
        public void SwitchEditorType()
        {
            EditingTxt = !EditingTxt;
            OnPropertyChanged(nameof(CurrentType));
        }

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
    }
}
