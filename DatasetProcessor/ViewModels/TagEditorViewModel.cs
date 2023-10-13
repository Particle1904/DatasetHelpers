using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Interfaces;

using System;
using System.IO;

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
        private string[] _imageFiles;
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
        private bool _editingTxt;

        public string CurrentType
        {
            get
            {
                if (_editingTxt)
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

        partial void OnImageFilesChanged(string[] value)
        {
            TotalImageFiles = $"Total files found: {ImageFiles.Length.ToString()}";
        }

        partial void OnSelectedItemIndexChanged(int value)
        {
            if (ImageFiles?.Length > 0)
            {
                SelectedItemIndex = Math.Clamp(value, 0, ImageFiles.Length - 1);
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
                //SelectedImage = newValue;
                SelectedImageFilename = Path.GetFileName(ImageFiles[SelectedItemIndex]);
            }
        }

        partial void OnTotalImageFilesChanged(string value)
        {
            TotalImageFiles = $"Total files found: {value}";
        }

        partial void OnCurrentAndTotalChanged(string value)
        {
            CurrentAndTotal = $"Current viewing: {SelectedItemIndex + 1}/{ImageFiles?.Length}.";
        }

        partial void OnCurrentImageTagsChanged(string value)
        {
            OnPropertyChanged(nameof(CurrentAndTotal));
            string txtFile = Path.ChangeExtension(ImageFiles[SelectedItemIndex], CurrentType);
            _fileManipulator.SaveTextToFile(txtFile, CurrentImageTags);
        }

        [RelayCommand]
        public void GoToItem(int parameter)
        {

        }
    }
}
