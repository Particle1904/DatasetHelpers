using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;

using System;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool _showGallerySettings;
        [ObservableProperty]
        private string _galleryInputFolder;
        private int? _galleryDisplayImageSize;
        public string GalleryDisplayImageSize
        {
            get => _galleryDisplayImageSize.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    _galleryDisplayImageSize = parsedValue;
                    OnPropertyChanged(nameof(GalleryDisplayImageSize));
                }
                catch
                {
                    _galleryDisplayImageSize = null;
                }
            }
        }

        [ObservableProperty]
        private bool _showSortImagesSettings;
        [ObservableProperty]
        private string _sortImagesInputFolder;
        [ObservableProperty]
        private string _sortImagesOutputFolder;
        [ObservableProperty]
        private string _sortImagesDiscardedFolder;
        [ObservableProperty]
        private string _sortImagesBackupFolder;
        [ObservableProperty]
        private bool _sortImagesBackup;
        [ObservableProperty]
        private SupportedDimensions _sortImagesMinimumDimension;

        [ObservableProperty]
        private bool _showContentAwareSettings;
        [ObservableProperty]
        private string _contentAwareInputFolder;
        [ObservableProperty]
        private string _contentAwareOutputFolder;
        [ObservableProperty]
        private double _contentAwareScoreThreshold;
        [ObservableProperty]
        private double _contentAwareIouThreshold;
        [ObservableProperty]
        private double _contentAwareExpansionPercentage;
        public string ExpansionPercentageString => $"{(int)(ContentAwareExpansionPercentage * 100)}%";
        [ObservableProperty]
        private SupportedDimensions _contentAwareOutputDimension;
        [ObservableProperty]
        private int _contentAwareLanczosRadius;
        [ObservableProperty]
        private bool _contentAwareApplySharpen;
        [ObservableProperty]
        public double _contentAwareSharpenSigma;
        private int? _contentAwareSigmaResolution;
        public string ContentAwareSigmaResolution
        {
            get => _contentAwareSigmaResolution.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    _contentAwareSigmaResolution = parsedValue;
                    OnPropertyChanged(nameof(ContentAwareSigmaResolution));
                }
                catch
                {
                    _contentAwareSigmaResolution = null;
                }
            }
        }

        public SettingsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            ShowGallerySettings = false;
            GalleryInputFolder = _configs.Configurations.GalleryConfigs.InputFolder;
            GalleryDisplayImageSize = _configs.Configurations.GalleryConfigs.ImageDisplaySize.ToString();

            ShowSortImagesSettings = false;
            SortImagesInputFolder = _configs.Configurations.SortImagesConfigs.InputFolder;
            SortImagesOutputFolder = _configs.Configurations.SortImagesConfigs.OutputFolder;
            SortImagesDiscardedFolder = _configs.Configurations.SortImagesConfigs.DiscardedFolder;
            SortImagesBackupFolder = _configs.Configurations.SortImagesConfigs.BackupFolder;
            SortImagesBackup = _configs.Configurations.SortImagesConfigs.BackupBeforeProcessing;
            SortImagesMinimumDimension = _configs.Configurations.SortImagesConfigs.DimensionSizeForDiscarded;

            ShowContentAwareSettings = true;
            ContentAwareInputFolder = _configs.Configurations.ContentAwareCropConfigs.InputFolder;
            ContentAwareOutputFolder = _configs.Configurations.ContentAwareCropConfigs.OutputFolder;
            ContentAwareScoreThreshold = _configs.Configurations.ContentAwareCropConfigs.PredictionsCertaintyThreshold;
            ContentAwareIouThreshold = _configs.Configurations.ContentAwareCropConfigs.IouThreshold;
            ContentAwareExpansionPercentage = _configs.Configurations.ContentAwareCropConfigs.ExpansionPercentage;
            ContentAwareOutputDimension = _configs.Configurations.ContentAwareCropConfigs.OutputDimensionSize;
            ContentAwareLanczosRadius = _configs.Configurations.ContentAwareCropConfigs.LanczosRadius;
            ContentAwareApplySharpen = _configs.Configurations.ContentAwareCropConfigs.ApplySharpenSigma;
            ContentAwareSharpenSigma = _configs.Configurations.ContentAwareCropConfigs.SharpenSigma;
            ContentAwareSigmaResolution = _configs.Configurations.ContentAwareCropConfigs.MinimumResolutionForSharpen.ToString();
        }

        [RelayCommand]
        private async Task SelectGaleryInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                GalleryInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectSortImagesInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                SortImagesInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectSortImagesOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                SortImagesOutputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectSortImagesDiscardedFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                SortImagesDiscardedFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectSortImagesBackupFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                SortImagesBackupFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectContentAwareInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ContentAwareInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectContentAwareOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ContentAwareOutputFolder = result;
            }
        }

        [RelayCommand]
        private void ToggleBool(string propertyName)
        {
            switch (propertyName)
            {
                case "ShowGallerySettings":
                    ShowGallerySettings = !ShowGallerySettings;
                    break;
                case "ShowSortImagesSettings":
                    ShowSortImagesSettings = !ShowSortImagesSettings;
                    break;
                case "ShowContentAwareSettings":
                    ShowContentAwareSettings = !ShowContentAwareSettings;
                    break;
            }
        }

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            Configs.Configurations.GalleryConfigs.InputFolder = GalleryInputFolder;
            Configs.Configurations.GalleryConfigs.ImageDisplaySize = Math.Clamp(int.Parse(GalleryDisplayImageSize), 256, 576);

            Configs.Configurations.SortImagesConfigs.InputFolder = SortImagesInputFolder;
            Configs.Configurations.SortImagesConfigs.OutputFolder = SortImagesOutputFolder;
            Configs.Configurations.SortImagesConfigs.DiscardedFolder = SortImagesDiscardedFolder;
            Configs.Configurations.SortImagesConfigs.BackupFolder = SortImagesBackupFolder;
            Configs.Configurations.SortImagesConfigs.BackupBeforeProcessing = SortImagesBackup;
            Configs.Configurations.SortImagesConfigs.DimensionSizeForDiscarded = SortImagesMinimumDimension;

            Configs.Configurations.ContentAwareCropConfigs.InputFolder = ContentAwareInputFolder;
            Configs.Configurations.ContentAwareCropConfigs.OutputFolder = ContentAwareOutputFolder;
            Configs.Configurations.ContentAwareCropConfigs.PredictionsCertaintyThreshold = (float)ContentAwareScoreThreshold;
            Configs.Configurations.ContentAwareCropConfigs.IouThreshold = (float)ContentAwareIouThreshold;
            Configs.Configurations.ContentAwareCropConfigs.ExpansionPercentage = (float)ContentAwareExpansionPercentage;
            Configs.Configurations.ContentAwareCropConfigs.OutputDimensionSize = ContentAwareOutputDimension;
            Configs.Configurations.ContentAwareCropConfigs.LanczosRadius = ContentAwareLanczosRadius;
            Configs.Configurations.ContentAwareCropConfigs.ApplySharpenSigma = ContentAwareApplySharpen;
            Configs.Configurations.ContentAwareCropConfigs.SharpenSigma = (float)ContentAwareSharpenSigma;
            Configs.Configurations.ContentAwareCropConfigs.MinimumResolutionForSharpen = Math.Clamp(
                int.Parse(ContentAwareSigmaResolution), 256, ushort.MaxValue);

            await Configs.SaveConfigurationsAsync();
        }

        partial void OnContentAwareScoreThresholdChanged(double value)
        {
            ContentAwareScoreThreshold = Math.Round(value, 2);
        }

        partial void OnContentAwareIouThresholdChanged(double value)
        {
            ContentAwareIouThreshold = Math.Round(value, 2);
        }

        partial void OnContentAwareExpansionPercentageChanged(double value)
        {
            ContentAwareExpansionPercentage = Math.Round(value, 2);
            OnPropertyChanged(nameof(ExpansionPercentageString));
        }

        partial void OnContentAwareSharpenSigmaChanged(double value)
        {
            ContentAwareSharpenSigma = Math.Round(value, 2);
        }
    }
}
