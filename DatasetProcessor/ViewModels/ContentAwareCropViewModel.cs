using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ContentAwareCropViewModel : ViewModelBase
    {
        private readonly IFileManipulatorService _fileManipulator;
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
                        Logger.LatestLogMessage = $"{_invalidMinSharpenNumberMessage}{Environment.NewLine}This value will be clampled to a valid number before processing!";
                    }
                    else
                    {
                        Logger.LatestLogMessage = string.Empty;
                    }

                    _minimumResolutionForSigma = parsedValue;
                    OnPropertyChanged(nameof(MinimumResolutionForSigma));
                }
                catch
                {
                    _minimumResolutionForSigma = null;
                    Logger.LatestLogMessage = $"{_invalidMinSharpenNumberMessage}{Environment.NewLine}This value cannot be empty! Use at least 256 as its minimum valid number.";
                }
            }
        }

        [ObservableProperty]
        private bool _isUiEnabled;

        public ContentAwareCropViewModel(IFileManipulatorService fileManipulator, IContentAwareCropService contentAwareCrop,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _contentAwareCrop = contentAwareCrop;

            (_contentAwareCrop as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                CropProgress = ResetProgress(CropProgress);
                CropProgress.TotalFiles = args;
            };
            (_contentAwareCrop as INotifyProgress).ProgressUpdated += (sender, args) => CropProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.SelectedFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.ResizedFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);

            ScoreThreshold = 0.5f;
            IouThreshold = 0.4f;
            ExpansionPercentage = 0.15f;
            Dimension = SupportedDimensions.Resolution512x512;

            LanczosRadius = 3.0d;
            ApplySharpen = false;
            SharpenSigma = 0.7d;

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
            IsUiEnabled = true;
        }

        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

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
            catch (Exception exception)
            {
                Logger.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }

            _timer.Stop();
            timer.Stop();
        }

        partial void OnScoreThresholdChanged(double value)
        {
            ScoreThreshold = Math.Round(value, 2);
        }

        partial void OnIouThresholdChanged(double value)
        {
            IouThreshold = Math.Round(value, 2);
        }

        partial void OnExpansionPercentageChanged(double value)
        {
            ExpansionPercentage = Math.Round(value, 2);
            OnPropertyChanged(nameof(ExpansionPercentageString));
        }

        partial void OnLanczosRadiusChanged(double value)
        {
            LanczosRadius = Math.Clamp(Math.Round(value), 1, 25);
        }

        partial void OnSharpenSigmaChanged(double value)
        {
            SharpenSigma = Math.Clamp(Math.Round(value, 2), 0.5d, 5d);
        }
    }
}
