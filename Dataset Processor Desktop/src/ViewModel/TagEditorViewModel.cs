using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagEditorViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly ILoggerService _loggerService;

        private string _inputFolderPath;
        public string InputFolderPath
        {
            get => _inputFolderPath;
            set
            {
                _inputFolderPath = value;
                OnPropertyChanged(nameof(InputFolderPath));
            }
        }

        private List<string> _imageFiles;
        public List<string> ImageFiles
        {
            get => _imageFiles;
            set
            {
                _imageFiles = value;
                OnPropertyChanged(nameof(ImageFiles));
            }
        }

        private int _selectedItemIndex;
        public int SelectedItemIndex
        {
            get => _selectedItemIndex;
            set
            {
                _selectedItemIndex = value;
                OnPropertyChanged(nameof(SelectedItemIndex));
                if (_imageFiles.Count > 0)
                {
                    SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                }
            }
        }

        private ImageSource _selectedImage;
        public ImageSource SelectedImage
        {
            get => _selectedImage;
            set
            {
                try
                {
                    CurrentImageTags = _fileManipulatorService.GetTagsForImage(_imageFiles[_selectedItemIndex]);
                }
                catch (Exception exception)
                {
                    _loggerService.LatestLogMessage = $".txt file for current image not found, just type in the editor and one will be created! Error: {exception.StackTrace}";
                }
                finally
                {
                    _selectedImage = value;
                    OnPropertyChanged(nameof(SelectedImage));
                }
            }
        }

        public RelayCommand PreviousItemCommand { get; private set; }
        public RelayCommand NextItemCommand { get; private set; }
        public RelayCommand SelectInputFolderCommand { get; private set; }

        private string _currentImageTags;
        public string CurrentImageTags
        {
            get => _currentImageTags;
            set
            {
                _currentImageTags = value;
                OnPropertyChanged(nameof(CurrentImageTags));

                string txtFile = Path.ChangeExtension(_imageFiles[_selectedItemIndex], ".txt");
                _fileManipulatorService.SaveTagsForImage(txtFile, _currentImageTags);
            }
        }

        public TagEditorViewModel(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, ILoggerService loggerService)
        {
            _folderPickerService = folderPickerService;
            _fileManipulatorService = fileManipulatorService;
            _loggerService = loggerService;

            _inputFolderPath = Path.Combine(AppContext.BaseDirectory, "combined-images-output");
            _fileManipulatorService.CreateFolderIfNotExist(_inputFolderPath);

            ImageFiles = _fileManipulatorService.GetImageFiles(InputFolderPath);

            PreviousItemCommand = new RelayCommand(GoToPreviousItem);
            NextItemCommand = new RelayCommand(GoToNextItem);
            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());

            SelectedItemIndex = 0;
            _showBlurredImage = false;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                try
                {
                    ImageFiles = _fileManipulatorService.GetImageFiles(InputFolderPath);
                }
                catch
                {
                    _loggerService.LatestLogMessage = "No image files were found in the directory.";
                }
                finally
                {
                    if (_imageFiles.Count > 0)
                    {
                        SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                    }
                }
            }
        }

        public void GoToPreviousItem()
        {
            if (_selectedItemIndex > 0)
            {
                _selectedItemIndex--;
                SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                OnPropertyChanged(nameof(SelectedItemIndex));
            }
        }

        public void GoToNextItem()
        {
            if (_selectedItemIndex < ImageFiles.Count - 1)
            {
                _selectedItemIndex++;
                SelectedImage = ImageSource.FromFile(_imageFiles[_selectedItemIndex]);
                OnPropertyChanged(nameof(SelectedItemIndex));
            }
        }
    }
}
