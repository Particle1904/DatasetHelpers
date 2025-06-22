using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces;
using Interfaces.MachineLearning;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ContentAwareCropViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;
        private readonly IModelManagerService _modelManager;
        private readonly IContentAwareCropService _contentAwareCrop;

        private const string _invalidMinSharpenNumberMessage = "Minimum resolution for sigma needs to be a number between 256 and 65535.";

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _cropProgress;
        [ObservableProperty]
        private double _scoreThreshold;
        [ObservableProperty]
        private double _iouThreshold;
        [ObservableProperty]
        private double _expansionPercentage;
        public string ExpansionPercentageString => $"{(int)(ExpansionPercentage * 100)}%";
        [ObservableProperty]
        private SupportedDimensions _dimension;
        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }
        [ObservableProperty]
        private double _lanczosRadius;
        [ObservableProperty]
        private bool _applySharpen;
        [ObservableProperty]
        private double _sharpenSigma;

        private int? _minimumResolutionForSigma;
        public string MinimumResolutionForSigma
        {
            get => _minimumResolutionForSigma.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    if (parsedValue < byte.MaxValue + 1 || parsedValue > ushort.MaxValue)
                    {
                        Logger.SetLatestLogMessage($"{_invalidMinSharpenNumberMessage}{Environment.NewLine}This value will be clampled to a valid number before processing!",
                            LogMessageColor.Warning, false);
                    }

                    _minimumResolutionForSigma = parsedValue;
                    OnPropertyChanged(nameof(MinimumResolutionForSigma));
                }
                catch
                {
                    _minimumResolutionForSigma = null;
                    Logger.SetLatestLogMessage($"{_invalidMinSharpenNumberMessage}{Environment.NewLine}This value cannot be empty! Use at least 256 as its minimum valid number.",
                        LogMessageColor.Warning);
                }
            }
        }

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public ContentAwareCropViewModel(IFileManagerService fileManager, IModelManagerService modelManager, IContentAwareCropService contentAwareCrop,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManager = fileManager;
            _modelManager = modelManager;
            _contentAwareCrop = contentAwareCrop;

            (_contentAwareCrop as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                CropProgress = ResetProgress(CropProgress);
                CropProgress.TotalFiles = args;
            };
            (_contentAwareCrop as INotifyProgress).ProgressUpdated += (sender, args) => CropProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.ContentAwareCropConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.ContentAwareCropConfigs.OutputFolder;
            _fileManager.CreateFolderIfNotExist(OutputFolderPath);
            ScoreThreshold = _configs.Configurations.ContentAwareCropConfigs.PredictionsCertaintyThreshold;
            IouThreshold = _configs.Configurations.ContentAwareCropConfigs.IouThreshold;
            ExpansionPercentage = _configs.Configurations.ContentAwareCropConfigs.ExpansionPercentage;
            Dimension = _configs.Configurations.ContentAwareCropConfigs.OutputDimensionSize;
            LanczosRadius = _configs.Configurations.ContentAwareCropConfigs.LanczosRadius;
            ApplySharpen = _configs.Configurations.ContentAwareCropConfigs.ApplySharpenSigma;
            SharpenSigma = _configs.Configurations.ContentAwareCropConfigs.SharpenSigma;
            MinimumResolutionForSigma = configs.Configurations.ContentAwareCropConfigs.MinimumResolutionForSharpen.ToString();

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
            IsUiEnabled = true;
        }

        /// <summary>
        /// Selects a folder path using the folder picker dialog and updates the InputFolderPath property.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        /// <summary>
        /// Selects a folder path using the folder picker dialog and updates the OutputFolderPath property.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task SelectOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        /// <summary>
        /// Starts the cropping process for images in the input folder and saves the results to the output folder.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        public async Task CropImagesAsync()
        {
            IsUiEnabled = false;

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (s, e) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            TaskStatus = ProcessingStatus.Running;
            try
            {
                await DownloadModelFiles(AvailableModels.Yolov4);

                _contentAwareCrop.ScoreThreshold = (float)ScoreThreshold;
                _contentAwareCrop.IouThreshold = (float)IouThreshold;
                _contentAwareCrop.ExpansionPercentage = (float)ExpansionPercentage + 1.0f;
                _contentAwareCrop.LanczosRadius = (int)LanczosRadius;
                _contentAwareCrop.ApplySharpen = ApplySharpen;
                _contentAwareCrop.SharpenSigma = (float)SharpenSigma;
                if (_minimumResolutionForSigma == null)
                {
                    _minimumResolutionForSigma = 0;
                }
                _contentAwareCrop.MinimumResolutionForSigma = Math.Clamp((int)_minimumResolutionForSigma, byte.MaxValue + 1, ushort.MaxValue);
                await _contentAwareCrop.ProcessCroppedImagesAsync(InputFolderPath, OutputFolderPath, Dimension);
            }
            catch (OperationCanceledException)
            {
                IsCancelEnabled = false;
                Logger.SetLatestLogMessage($"Cancelled the current operation!", LogMessageColor.Informational);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                    LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
                (_contentAwareCrop as IUnloadModel)?.UnloadAIModel();
            }

            _timer.Stop();
            timer.Stop();
        }

        /// <summary>
        /// Downloads the model files if they are not already present on the system.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task DownloadModelFiles(AvailableModels model)
        {
            if (_modelManager.FileNeedsToBeDownloaded(model))
            {
                await _modelManager.DownloadModelFileAsync(model);
            }
        }

        /// <summary>
        /// Rounds the score threshold value to two decimal places when it changes.
        /// </summary>
        /// <param name="value"></param>
        partial void OnScoreThresholdChanged(double value)
        {
            ScoreThreshold = Math.Round(value, 2);
        }

        /// <summary>
        /// Rounds the IoU threshold value to two decimal places when it changes.
        /// </summary>
        /// <param name="value"></param>
        partial void OnIouThresholdChanged(double value)
        {
            IouThreshold = Math.Round(value, 2);
        }

        /// <summary>
        /// Rounds the expansion percentage value to two decimal places when it changes.
        /// </summary>
        /// <param name="value"></param>
        partial void OnExpansionPercentageChanged(double value)
        {
            ExpansionPercentage = Math.Round(value, 2);
            OnPropertyChanged(nameof(ExpansionPercentageString));
        }

        /// <summary>
        /// Clamps the Lanczos radius value to a range of 1 to 25 when it changes.
        /// </summary>
        /// <param name="value"></param>
        partial void OnLanczosRadiusChanged(double value)
        {
            LanczosRadius = Math.Clamp(Math.Round(value), 1, 25);
        }

        /// <summary>
        /// Clamps the sharpen sigma value to a range of 0.5 to 5 when it changes.
        /// </summary>
        /// <param name="value"></param>
        partial void OnSharpenSigmaChanged(double value)
        {
            SharpenSigma = Math.Clamp(Math.Round(value, 2), 0.5d, 5d);
        }

        /// <summary>
        /// Cancels the current cropping task if it is running.
        /// </summary>
        [RelayCommand]
        private void CancelTask()
        {
            (_fileManager as ICancellableService)?.CancelCurrentTask();
            (_contentAwareCrop as ICancellableService)?.CancelCurrentTask();
        }

        /// <summary>
        /// Handles changes to the IsUiEnabled property and updates the IsCancelEnabled property accordingly.
        /// </summary>
        /// <param name="value"></param>
        partial void OnIsUiEnabledChanged(bool value)
        {
            if (value)
            {
                IsCancelEnabled = false;
            }
            else
            {
                IsCancelEnabled = true;
            }
        }
    }
}
