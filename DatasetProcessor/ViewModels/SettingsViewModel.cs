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

        [ObservableProperty]
        private bool _showManualCropSettings;
        [ObservableProperty]
        private string _manualCropInputFolder;
        [ObservableProperty]
        private string _manualCropOutputFolder;

        [ObservableProperty]
        private bool _showResizeImagesSettings;
        [ObservableProperty]
        private string _resizeImagesInputFolder;
        [ObservableProperty]
        private string _resizeImagesOutputFolder;
        [ObservableProperty]
        private SupportedDimensions _resizeImagesOutputDimension;
        [ObservableProperty]
        private int _resizeImagesLanczosRadius;
        [ObservableProperty]
        private bool _resizeImagesApplySharpen;
        [ObservableProperty]
        public double _resizeImagesSharpenSigma;
        private int? _resizeImagesSigmaResolution;
        public string ResizeImagesSigmaResolution
        {
            get => _resizeImagesSigmaResolution.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    _resizeImagesSigmaResolution = parsedValue;
                    OnPropertyChanged(nameof(ResizeImagesSigmaResolution));
                }
                catch
                {
                    _resizeImagesSigmaResolution = null;
                }
            }
        }

        [ObservableProperty]
        private bool _showUpscaleImagesSettings;
        [ObservableProperty]
        private string _upscaleImagesInputFolder;
        [ObservableProperty]
        private string _upscaleImagesOutputFolder;
        [ObservableProperty]
        private AvailableModels _upscalerModel;

        [ObservableProperty]
        private bool _showGenerateTagsSettings;

        [ObservableProperty]
        private bool _showProcessCaptionsSettings;

        [ObservableProperty]
        private bool _showProcessTagsSettings;

        [ObservableProperty]
        private bool _showEditorSettings;

        [ObservableProperty]
        private bool _showExtractSubsetSettings;

        [ObservableProperty]
        private bool _showPromptGeneratorSettings;

        [ObservableProperty]
        private bool _showMetadataViewerSettings;

        public SettingsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            GalleryInputFolder = Configs.Configurations.GalleryConfigs.InputFolder;
            GalleryDisplayImageSize = Configs.Configurations.GalleryConfigs.ImageDisplaySize.ToString();

            SortImagesInputFolder = Configs.Configurations.SortImagesConfigs.InputFolder;
            SortImagesOutputFolder = Configs.Configurations.SortImagesConfigs.OutputFolder;
            SortImagesDiscardedFolder = Configs.Configurations.SortImagesConfigs.DiscardedFolder;
            SortImagesBackupFolder = Configs.Configurations.SortImagesConfigs.BackupFolder;
            SortImagesBackup = Configs.Configurations.SortImagesConfigs.BackupBeforeProcessing;
            SortImagesMinimumDimension = Configs.Configurations.SortImagesConfigs.DimensionSizeForDiscarded;

            ContentAwareInputFolder = Configs.Configurations.ContentAwareCropConfigs.InputFolder;
            ContentAwareOutputFolder = Configs.Configurations.ContentAwareCropConfigs.OutputFolder;
            ContentAwareScoreThreshold = Configs.Configurations.ContentAwareCropConfigs.PredictionsCertaintyThreshold;
            ContentAwareIouThreshold = Configs.Configurations.ContentAwareCropConfigs.IouThreshold;
            ContentAwareExpansionPercentage = Configs.Configurations.ContentAwareCropConfigs.ExpansionPercentage;
            ContentAwareOutputDimension = Configs.Configurations.ContentAwareCropConfigs.OutputDimensionSize;
            ContentAwareLanczosRadius = Configs.Configurations.ContentAwareCropConfigs.LanczosRadius;
            ContentAwareApplySharpen = Configs.Configurations.ContentAwareCropConfigs.ApplySharpenSigma;
            ContentAwareSharpenSigma = Configs.Configurations.ContentAwareCropConfigs.SharpenSigma;
            ContentAwareSigmaResolution = Configs.Configurations.ContentAwareCropConfigs.MinimumResolutionForSharpen.ToString();

            ManualCropInputFolder = Configs.Configurations.ManualCropConfigs.InputFolder;
            ManualCropOutputFolder = Configs.Configurations.ManualCropConfigs.OutputFolder;

            ResizeImagesInputFolder = Configs.Configurations.ResizeImagesConfigs.InputFolder;
            ResizeImagesOutputFolder = Configs.Configurations.ResizeImagesConfigs.OutputFolder;
            ResizeImagesOutputDimension = Configs.Configurations.ResizeImagesConfigs.OutputDimensionSize;
            ResizeImagesLanczosRadius = Configs.Configurations.ResizeImagesConfigs.LanczosRadius;
            ResizeImagesApplySharpen = Configs.Configurations.ResizeImagesConfigs.ApplySharpenSigma;
            ResizeImagesSharpenSigma = Configs.Configurations.ResizeImagesConfigs.SharpenSigma;
            ResizeImagesSigmaResolution = Configs.Configurations.ResizeImagesConfigs.MinimumResolutionForSharpen.ToString();

            UpscaleImagesInputFolder = Configs.Configurations.UpscaleImagesConfigs.InputFolder;
            UpscaleImagesOutputFolder = Configs.Configurations.UpscaleImagesConfigs.OutputFolder;
            UpscalerModel = Configs.Configurations.UpscaleImagesConfigs.UpscalerModel;
        }

        [RelayCommand]
        private async Task SelectGalleryInputFolderAsync()
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
        private async Task SelectManualCropInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ManualCropInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectManualCropOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ManualCropOutputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectResizeImagesInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ResizeImagesInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectResizeImagesOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ResizeImagesOutputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectUpscaleImagesInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                UpscaleImagesInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectUpscaleImagesOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                UpscaleImagesOutputFolder = result;
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
                case "ShowManualCropSettings":
                    ShowManualCropSettings = !ShowManualCropSettings;
                    break;
                case "ShowResizeImagesSettings":
                    ShowResizeImagesSettings = !ShowResizeImagesSettings;
                    break;
                case "ShowUpscaleImagesSettings":
                    ShowUpscaleImagesSettings = !ShowUpscaleImagesSettings;
                    break;
                case "ShowGenerateTagsSettings":
                    ShowGenerateTagsSettings = !ShowGenerateTagsSettings;
                    break;
                case "ShowProcessCaptionsSettings":
                    ShowProcessCaptionsSettings = !ShowProcessCaptionsSettings;
                    break;
                case "ShowProcessTagsSettings":
                    ShowProcessTagsSettings = !ShowProcessTagsSettings;
                    break;
                case "ShowEditorSettings":
                    ShowEditorSettings = !ShowEditorSettings;
                    break;
                case "ShowExtractSubsetSettings":
                    ShowExtractSubsetSettings = !ShowExtractSubsetSettings;
                    break;
                case "ShowPromptGeneratorSettings":
                    ShowPromptGeneratorSettings = !ShowPromptGeneratorSettings;
                    break;
                case "ShowMetadataViewerSettings":
                    ShowMetadataViewerSettings = !ShowMetadataViewerSettings;
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

            Configs.Configurations.ManualCropConfigs.InputFolder = ManualCropInputFolder;
            Configs.Configurations.ManualCropConfigs.OutputFolder = ManualCropOutputFolder;

            Configs.Configurations.ResizeImagesConfigs.InputFolder = ResizeImagesInputFolder;
            Configs.Configurations.ResizeImagesConfigs.OutputFolder = ResizeImagesOutputFolder;
            Configs.Configurations.ResizeImagesConfigs.OutputDimensionSize = ResizeImagesOutputDimension;
            Configs.Configurations.ResizeImagesConfigs.LanczosRadius = ResizeImagesLanczosRadius;
            Configs.Configurations.ResizeImagesConfigs.ApplySharpenSigma = ResizeImagesApplySharpen;
            Configs.Configurations.ResizeImagesConfigs.SharpenSigma = (float)ResizeImagesSharpenSigma;
            Configs.Configurations.ResizeImagesConfigs.MinimumResolutionForSharpen = Math.Clamp(
                int.Parse(ResizeImagesSigmaResolution), 256, ushort.MaxValue);

            Configs.Configurations.UpscaleImagesConfigs.InputFolder = UpscaleImagesInputFolder;
            Configs.Configurations.UpscaleImagesConfigs.OutputFolder = UpscaleImagesOutputFolder;
            Configs.Configurations.UpscaleImagesConfigs.UpscalerModel = UpscalerModel;

            await Configs.SaveConfigurationsAsync();
            Logger.SetLatestLogMessage($"Settings saved!", LogMessageColor.Informational);
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

        partial void OnResizeImagesSharpenSigmaChanged(double value)
        {
            ResizeImagesSharpenSigma = Math.Round(value, 2);
        }
    }
}
