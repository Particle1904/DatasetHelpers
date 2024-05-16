using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Interfaces;

using System;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
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
            await Configs.SaveConfigurationsAsync();
        }

        partial void OnTaggerThresholdChanged(double value)
        {
            TaggerThreshold = Math.Round(value, 2);
        }
    }
}
