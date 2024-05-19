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
        private string _generateTagsInputFolder;
        [ObservableProperty]
        private string _generateTagsOutputFolder;
        [ObservableProperty]
        private AvailableModels _tagGeneratorModel;
        [ObservableProperty]
        private double _generateTagsThreshold;
        [ObservableProperty]
        private bool _generateTagsApplyRedudancyRemoval;
        [ObservableProperty]
        private bool _generateTagsAppendGeneratedTags;
        [ObservableProperty]
        private bool _generateTagsWeighted;

        [ObservableProperty]
        private bool _showProcessCaptionsSettings;
        [ObservableProperty]
        private string _processCaptionsInputFolder;

        [ObservableProperty]
        private bool _showProcessTagsSettings;
        [ObservableProperty]
        private string _processTagsInputFolder;
        [ObservableProperty]
        private bool _processTagsRandomizeTags;
        [ObservableProperty]
        private bool _processTagsRenameFiles;
        [ObservableProperty]
        private bool _processTagsApplyRedudancyRemoval;
        [ObservableProperty]
        private bool _processTagsConsolidateTags;

        [ObservableProperty]
        private bool _showEditorSettings;
        [ObservableProperty]
        private string _editorInputFolder;
        [ObservableProperty]
        private bool _editorExactMatches;

        [ObservableProperty]
        private bool _showExtractSubsetSettings;
        [ObservableProperty]
        private string _extractSubsetInputFolder;
        [ObservableProperty]
        private string _extractSubsetOutputFolder;
        [ObservableProperty]
        private bool _extractSubsetSearchTxt;
        [ObservableProperty]
        private bool _extractSubsetSearchCaption;
        [ObservableProperty]
        private bool _extractSubsetExactMatchesFiltering;

        [ObservableProperty]
        private bool _showPromptGeneratorSettings;
        [ObservableProperty]
        private string _promptGeneratorInputFolder;
        [ObservableProperty]
        private string _promptGeneratorOutputFolder;
        [ObservableProperty]
        private string _promptGeneratorTagsToPrepend;
        [ObservableProperty]
        private string _promptGeneratorTagsToAppend;
        private int? _promptGeneratorAmountOfTags;
        public string PromptGeneratorAmountOfTags
        {
            get => _promptGeneratorAmountOfTags.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    _promptGeneratorAmountOfTags = parsedValue;
                    OnPropertyChanged(nameof(PromptGeneratorAmountOfTags));
                }
                catch
                {
                    _promptGeneratorAmountOfTags = null;
                }
            }
        }
        private int? _promptGeneratorAmountOfPrompts;
        public string PromptGeneratorAmountOfPrompts
        {
            get => _promptGeneratorAmountOfPrompts.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    _promptGeneratorAmountOfPrompts = parsedValue;
                    OnPropertyChanged(nameof(PromptGeneratorAmountOfPrompts));
                }
                catch
                {
                    _promptGeneratorAmountOfPrompts = null;
                }
            }
        }

        [ObservableProperty]
        private bool _showMetadataViewerSettings;
        // TODO: Add configs for Metadata page.

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

            GenerateTagsInputFolder = Configs.Configurations.GenerateTagsConfigs.InputFolder;
            GenerateTagsOutputFolder = Configs.Configurations.GenerateTagsConfigs.OutputFolder;
            TagGeneratorModel = Configs.Configurations.GenerateTagsConfigs.AutoTaggerModel;
            GenerateTagsThreshold = Configs.Configurations.GenerateTagsConfigs.PredictionsThreshold;
            GenerateTagsApplyRedudancyRemoval = Configs.Configurations.GenerateTagsConfigs.ApplyRedudancyRemoval;
            GenerateTagsAppendGeneratedTags = Configs.Configurations.GenerateTagsConfigs.AppendToExistingFile;
            GenerateTagsWeighted = Configs.Configurations.GenerateTagsConfigs.WeightedCaptions;

            ProcessCaptionsInputFolder = Configs.Configurations.ProcessCaptionsConfigs.InputFolder;

            ProcessTagsInputFolder = Configs.Configurations.ProcessTagsConfigs.InputFolder;
            ProcessTagsRandomizeTags = Configs.Configurations.ProcessTagsConfigs.RandomizeTags;
            ProcessTagsRenameFiles = Configs.Configurations.ProcessTagsConfigs.RenameFiles;
            ProcessTagsApplyRedudancyRemoval = Configs.Configurations.ProcessTagsConfigs.ApplyRedudancyRemoval;
            ProcessTagsConsolidateTags = Configs.Configurations.ProcessTagsConfigs.ConsolidateTags;

            EditorInputFolder = Configs.Configurations.TagEditorConfigs.InputFolder;
            EditorExactMatches = Configs.Configurations.TagEditorConfigs.ExactMatchesFiltering;

            ExtractSubsetInputFolder = Configs.Configurations.ExtractSubsetConfigs.InputFolder;
            ExtractSubsetOutputFolder = Configs.Configurations.ExtractSubsetConfigs.OutputFolder;
            ExtractSubsetSearchTxt = Configs.Configurations.ExtractSubsetConfigs.SearchTxt;
            ExtractSubsetSearchCaption = Configs.Configurations.ExtractSubsetConfigs.SearchCaption;
            ExtractSubsetExactMatchesFiltering = Configs.Configurations.ExtractSubsetConfigs.ExactMatchesFiltering;

            PromptGeneratorInputFolder = Configs.Configurations.PromptGeneratorConfigs.InputFolder;
            PromptGeneratorOutputFolder = Configs.Configurations.PromptGeneratorConfigs.OutputFolder;
            PromptGeneratorTagsToPrepend = Configs.Configurations.PromptGeneratorConfigs.TagsToPrepend;
            PromptGeneratorTagsToAppend = Configs.Configurations.PromptGeneratorConfigs.TagsToAppend;
            PromptGeneratorAmountOfTags = Configs.Configurations.PromptGeneratorConfigs.AmountOfTags.ToString();
            PromptGeneratorAmountOfPrompts = Configs.Configurations.PromptGeneratorConfigs.AmountOfPrompts.ToString();
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
        private async Task SelectGenerateTagsInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                GenerateTagsInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectGenerateTagsOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                GenerateTagsOutputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectProcessCaptionsInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ProcessCaptionsInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectProcessTagsInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ProcessTagsInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectEditorInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                EditorInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectExtractSubsetInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ExtractSubsetInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectExtractSubsetOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                ExtractSubsetOutputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectPromptGeneratorInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                PromptGeneratorInputFolder = result;
            }
        }

        [RelayCommand]
        private async Task SelectPromptGeneratorOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                PromptGeneratorOutputFolder = result;
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

            Configs.Configurations.GenerateTagsConfigs.InputFolder = GenerateTagsInputFolder;
            Configs.Configurations.GenerateTagsConfigs.OutputFolder = GenerateTagsOutputFolder;
            Configs.Configurations.GenerateTagsConfigs.AutoTaggerModel = TagGeneratorModel;
            Configs.Configurations.GenerateTagsConfigs.PredictionsThreshold = (float)GenerateTagsThreshold;
            Configs.Configurations.GenerateTagsConfigs.ApplyRedudancyRemoval = GenerateTagsApplyRedudancyRemoval;
            Configs.Configurations.GenerateTagsConfigs.AppendToExistingFile = GenerateTagsAppendGeneratedTags;
            Configs.Configurations.GenerateTagsConfigs.WeightedCaptions = GenerateTagsWeighted;

            Configs.Configurations.ProcessCaptionsConfigs.InputFolder = ProcessCaptionsInputFolder;

            Configs.Configurations.ProcessTagsConfigs.InputFolder = ProcessTagsInputFolder;
            Configs.Configurations.ProcessTagsConfigs.RandomizeTags = ProcessTagsRandomizeTags;
            Configs.Configurations.ProcessTagsConfigs.RenameFiles = ProcessTagsRenameFiles;
            Configs.Configurations.ProcessTagsConfigs.ApplyRedudancyRemoval = ProcessTagsApplyRedudancyRemoval;
            Configs.Configurations.ProcessTagsConfigs.ConsolidateTags = ProcessTagsConsolidateTags;

            Configs.Configurations.TagEditorConfigs.InputFolder = EditorInputFolder;
            Configs.Configurations.TagEditorConfigs.ExactMatchesFiltering = EditorExactMatches;

            Configs.Configurations.ExtractSubsetConfigs.InputFolder = ExtractSubsetInputFolder;
            Configs.Configurations.ExtractSubsetConfigs.OutputFolder = ExtractSubsetOutputFolder;
            Configs.Configurations.ExtractSubsetConfigs.SearchTxt = ExtractSubsetSearchTxt;
            Configs.Configurations.ExtractSubsetConfigs.SearchCaption = ExtractSubsetSearchCaption;
            Configs.Configurations.ExtractSubsetConfigs.ExactMatchesFiltering = ExtractSubsetExactMatchesFiltering;

            Configs.Configurations.PromptGeneratorConfigs.InputFolder = PromptGeneratorInputFolder;
            Configs.Configurations.PromptGeneratorConfigs.OutputFolder = PromptGeneratorOutputFolder;
            Configs.Configurations.PromptGeneratorConfigs.TagsToPrepend = PromptGeneratorTagsToPrepend;
            Configs.Configurations.PromptGeneratorConfigs.TagsToAppend = PromptGeneratorTagsToAppend;
            Configs.Configurations.PromptGeneratorConfigs.AmountOfTags = Math.Clamp(int.Parse(PromptGeneratorAmountOfTags),
                10, 50);
            Configs.Configurations.PromptGeneratorConfigs.AmountOfPrompts = Math.Clamp(int.Parse(PromptGeneratorAmountOfPrompts),
                10, ushort.MaxValue);

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

        partial void OnGenerateTagsThresholdChanged(double value)
        {
            GenerateTagsThreshold = Math.Round(value, 2);
        }
    }
}
