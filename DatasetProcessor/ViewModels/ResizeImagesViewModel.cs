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
    public partial class ResizeImagesViewModel : BaseViewModel
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IFileManipulatorService _fileManipulator;

        private const string _invalidMinSharpenNumberMessage = "Minimum resolution for sigma needs to be a number between 256 and 65535.";

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _resizeProgress;
        [ObservableProperty]
        private SupportedDimensions _dimension;
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
                            LogMessageColor.Warning);
                    }
                    else
                    {
                        Logger.SetLatestLogMessage(string.Empty, LogMessageColor.Error);
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

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public ResizeImagesViewModel(IImageProcessorService imageProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManipulator = fileManipulator;

            (_imageProcessor as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                ResizeProgress = ResetProgress(ResizeProgress);
                ResizeProgress.TotalFiles = args;
            };
            (_imageProcessor as INotifyProgress).ProgressUpdated += (sender, args) => ResizeProgress.UpdateProgress();

            Dimension = SupportedDimensions.Resolution1024x1024;

            InputFolderPath = _configs.Configurations.ResizeImagesConfigs.InputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.ResizeImagesConfigs.OutputFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);
            Dimension = _configs.Configurations.ResizeImagesConfigs.OutputDimensionSize;
            LanczosRadius = _configs.Configurations.ResizeImagesConfigs.LanczosRadius;
            ApplySharpen = _configs.Configurations.ResizeImagesConfigs.ApplySharpenSigma;
            SharpenSigma = _configs.Configurations.ResizeImagesConfigs.SharpenSigma;
            MinimumResolutionForSigma = _configs.Configurations.ResizeImagesConfigs.MinimumResolutionForSharpen.ToString();

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
        public async Task ResizeImagesAsync()
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
                _imageProcessor.LanczosSamplerRadius = (int)LanczosRadius;
                _imageProcessor.ApplySharpen = ApplySharpen;
                _imageProcessor.SharpenSigma = (float)SharpenSigma;
                if (_minimumResolutionForSigma == null)
                {
                    _minimumResolutionForSigma = 0;
                }
                _imageProcessor.MinimumResolutionForSigma = Math.Clamp((int)_minimumResolutionForSigma, byte.MaxValue + 1,
                    ushort.MaxValue);
                await _imageProcessor.ResizeImagesAsync(InputFolderPath, OutputFolderPath, Dimension);
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
            }

            _timer.Stop();
            timer.Stop();
        }

        partial void OnLanczosRadiusChanged(double value)
        {
            LanczosRadius = Math.Round(value, 2);
        }

        partial void OnSharpenSigmaChanged(double value)
        {
            SharpenSigma = Math.Round(value, 2);
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManipulator as ICancellableService)?.CancelCurrentTask();
            (_imageProcessor as ICancellableService)?.CancelCurrentTask();
        }

        partial void OnIsUiEnabledChanged(bool value)
        {
            if (value == true)
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
