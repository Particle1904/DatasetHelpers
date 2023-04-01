using Dataset_Processor_Desktop.src.Utilities;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class SettingsViewModel : BaseViewModel
    {
        private double _taggerThreshold;
        public double TaggerThreshold
        {
            get => _taggerThreshold;
            set
            {
                _taggerThreshold = Math.Round(value, 2);
                OnPropertyChanged(nameof(TaggerThreshold));
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
        private string _resizedFolderPath;
        public string ResizedFolderPath
        {
            get => _resizedFolderPath;
            set
            {
                _resizedFolderPath = value;
                OnPropertyChanged(nameof(ResizedFolderPath));
            }
        }
        private string _combinedOutputFolderPath;
        public string CombinedOutputFolderPath
        {
            get => _combinedOutputFolderPath;
            set
            {
                _combinedOutputFolderPath = value;
                OnPropertyChanged(nameof(CombinedOutputFolderPath));
            }
        }

        public RelayCommand SelectSortedFolderCommand { get; private set; }
        public RelayCommand SelectDiscardedFolderCommand { get; private set; }
        public RelayCommand SelectBackupFolderCommand { get; private set; }
        public RelayCommand SelectResizedFolderCommand { get; private set; }
        public RelayCommand SelectCombinedOutputFolderCommand { get; private set; }
        public RelayCommand SaveSettingsCommand { get; private set; }

        public SettingsViewModel()
        {
            SelectSortedFolderCommand = new RelayCommand(async () => await SelectSortedFolderAsync());
            SelectDiscardedFolderCommand = new RelayCommand(async () => await SelectDiscardedFolderAsync());
            SelectBackupFolderCommand = new RelayCommand(async () => await SelectBackupFolderAsync());
            SelectResizedFolderCommand = new RelayCommand(async () => await SelectResizedFolderAsync());
            SelectCombinedOutputFolderCommand = new RelayCommand(async () => await SelectCombinedOutputFolderAsync());
            SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync());

            TaggerThreshold = _configsService.Configurations.TaggerThreshold;
            SelectedFolderPath = _configsService.Configurations.SelectedFolder;
            DiscardedFolderPath = _configsService.Configurations.DiscardedFolder;
            BackupFolderPath = _configsService.Configurations.BackupFolder;
            ResizedFolderPath = _configsService.Configurations.ResizedFolder;
            CombinedOutputFolderPath = _configsService.Configurations.CombinedOutputFolder;
        }

        public async Task SelectSortedFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                SelectedFolderPath = result;
            }
        }
        public async Task SelectDiscardedFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                DiscardedFolderPath = result;
            }
        }
        public async Task SelectBackupFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                BackupFolderPath = result;
            }
        }
        public async Task SelectResizedFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                ResizedFolderPath = result;
            }
        }

        public async Task SelectCombinedOutputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                CombinedOutputFolderPath = result;
            }
        }

        public async Task SaveSettingsAsync()
        {
            if (TaggerThreshold != 0)
            {
                _configsService.Configurations.TaggerThreshold = (float)TaggerThreshold;
            }

            if (!string.IsNullOrEmpty(SelectedFolderPath))
            {
                _configsService.Configurations.SelectedFolder = SelectedFolderPath;
            }

            if (!string.IsNullOrEmpty(DiscardedFolderPath))
            {
                _configsService.Configurations.DiscardedFolder = DiscardedFolderPath;
            }

            if (!string.IsNullOrEmpty(BackupFolderPath))
            {
                _configsService.Configurations.BackupFolder = BackupFolderPath;
            }

            if (!string.IsNullOrEmpty(ResizedFolderPath))
            {
                _configsService.Configurations.ResizedFolder = ResizedFolderPath;
            }

            if (!string.IsNullOrEmpty(CombinedOutputFolderPath))
            {
                _configsService.Configurations.CombinedOutputFolder = CombinedOutputFolderPath;
            }

            await _configsService.SaveConfigurations();
        }
    }
}