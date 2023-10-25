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
    public partial class ResizeImagesViewModel : ViewModelBase
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
        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }

        public ResizeImagesViewModel(IImageProcessorService imageProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _fileManipulator = fileManipulator;

            Dimension = SupportedDimensions.Resolution512x512;

            InputFolderPath = _configs.Configurations.SelectedFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.ResizedFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);

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
        public async Task ResizeImagesAsync()
        {
            IsUiEnabled = false;

            ResizeProgress = ResetProgress(ResizeProgress);

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
                _imageProcessor.MinimumResolutionForSigma = Math.Clamp((int)_minimumResolutionForSigma, byte.MaxValue + 1, ushort.MaxValue);
                await _imageProcessor.ResizeImagesAsync(InputFolderPath, OutputFolderPath, ResizeProgress, Dimension);
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
    }
}
