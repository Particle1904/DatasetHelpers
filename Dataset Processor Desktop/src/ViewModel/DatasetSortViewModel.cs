using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class DatasetSortViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        private readonly IFileManipulatorService _fileManipulatorService;

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

        private string _selectedFolderPath;
        public string SelectedFolderPath
        {
            get => _selectedFolderPath;
            set
            {
                _selectedFolderPath = value;
                OnPropertyChanged(nameof(SelectedFolderPath));
            }
        }

        private string _discardedFolderPath;
        public string DiscardedFolderPath
        {
            get => _discardedFolderPath;
            set
            {
                _discardedFolderPath = value;
                OnPropertyChanged(nameof(DiscardedFolderPath));
            }
        }

        private string _backupFolderPath;
        public string BackupFolderPath
        {
            get => _backupFolderPath;
            set
            {
                _backupFolderPath = value;
                OnPropertyChanged(nameof(BackupFolderPath));
            }
        }

        private bool _backupImages = false;
        public bool BackupImages
        {
            get => _backupImages;
            set
            {
                _backupImages = value;
                OnPropertyChanged(nameof(BackupImages));
            }
        }

        private Progress _sortProgress;
        public Progress SortProgress
        {
            get => _sortProgress;
            set
            {
                _sortProgress = value;
                OnPropertyChanged(nameof(SortProgress));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectSelectedFolderCommand { get; private set; }
        public RelayCommand SelectDiscardedFolderCommand { get; private set; }
        public RelayCommand SelectBackupFolderCommand { get; private set; }

        public RelayCommand SortImagesCommand { get; private set; }

        public DatasetSortViewModel(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService)
        {
            _discardedFolderPath = Path.Combine(AppContext.BaseDirectory, "discarded-images-output");
            _selectedFolderPath = Path.Combine(AppContext.BaseDirectory, "selected-images-output");
            _backupFolderPath = Path.Combine(AppContext.BaseDirectory, "images-backup");

            _folderPickerService = folderPickerService;
            _fileManipulatorService = fileManipulatorService;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectSelectedFolderCommand = new RelayCommand(async () => await SelectSelectedFolderAsync());
            SelectDiscardedFolderCommand = new RelayCommand(async () => await SelectDiscardedFolderAsync());
            SelectBackupFolderCommand = new RelayCommand(async () => await SelectBackupFolderAsync());
            SortImagesCommand = new RelayCommand(async () => await SortImagesAsync());
        }

        public async Task SelectInputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (result != null)
            {
                InputFolderPath = result;
            }
        }
        public async Task SelectSelectedFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (result != null)
            {
                SelectedFolderPath = result;
            }
        }
        public async Task SelectDiscardedFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (result != null)
            {
                DiscardedFolderPath = result;
            }
        }
        public async Task SelectBackupFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (result != null)
            {
                BackupFolderPath = result;
            }
        }

        public async Task SortImagesAsync()
        {
            _fileManipulatorService.CreateFolderIfNotExist(_discardedFolderPath);
            _fileManipulatorService.CreateFolderIfNotExist(_selectedFolderPath);
            _fileManipulatorService.CreateFolderIfNotExist(_backupFolderPath);

            if (SortProgress == null)
            {
                SortProgress = new Progress();
            }

            await _fileManipulatorService.SortImagesAsync(_inputFolderPath, _discardedFolderPath, _selectedFolderPath, SortProgress, 512);
        }
    }
}