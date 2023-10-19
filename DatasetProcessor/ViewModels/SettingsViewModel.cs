using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Interfaces;

using System;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private double _taggerThreshold;
        [ObservableProperty]
        private string _selectedFolderPath;
        [ObservableProperty]
        private string _discardedFolderPath;
        [ObservableProperty]
        private string _backupFolderPath;
        [ObservableProperty]
        private string _resizedFolderPath;
        [ObservableProperty]
        private string _combinedOutputFolderPath;

        public SettingsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            TaggerThreshold = _configs.Configurations.TaggerThreshold;
            SelectedFolderPath = _configs.Configurations.SelectedFolder;
            DiscardedFolderPath = _configs.Configurations.DiscardedFolder;
            BackupFolderPath = _configs.Configurations.BackupFolder;
            ResizedFolderPath = _configs.Configurations.ResizedFolder;
            CombinedOutputFolderPath = _configs.Configurations.CombinedOutputFolder;
        }

        [RelayCommand]
        private async Task SelectSortedFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                SelectedFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectDiscardedFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                DiscardedFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectBackupFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                BackupFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectResizedFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ResizedFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectCombinedOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                CombinedOutputFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            if (TaggerThreshold != 0)
            {
                Configs.Configurations.TaggerThreshold = (float)TaggerThreshold;
            }

            if (!string.IsNullOrEmpty(SelectedFolderPath))
            {
                Configs.Configurations.SelectedFolder = SelectedFolderPath;
            }

            if (!string.IsNullOrEmpty(DiscardedFolderPath))
            {
                Configs.Configurations.DiscardedFolder = DiscardedFolderPath;
            }

            if (!string.IsNullOrEmpty(BackupFolderPath))
            {
                Configs.Configurations.BackupFolder = BackupFolderPath;
            }

            if (!string.IsNullOrEmpty(ResizedFolderPath))
            {
                Configs.Configurations.ResizedFolder = ResizedFolderPath;
            }

            if (!string.IsNullOrEmpty(CombinedOutputFolderPath))
            {
                Configs.Configurations.CombinedOutputFolder = CombinedOutputFolderPath;
            }

            await Configs.SaveConfigurationsAsync();
        }

        partial void OnTaggerThresholdChanged(double value)
        {
            TaggerThreshold = Math.Round(value, 2);
        }
    }
}
