using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class UpscaleViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IUpscalerService _upscaler;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _upscalingProgress;
        [ObservableProperty]
        private AvailableModels _upscalerModel;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public UpscaleViewModel(IFileManipulatorService fileManipulator, IUpscalerService upscalerService, ILoggerService logger,
            IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _fileManipulator.DownloadMessageEvent += (sender, args) => Logger.SetLatestLogMessage(args,
                LogMessageColor.Informational);

            _upscaler = upscalerService;
            (_upscaler as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                UpscalingProgress = ResetProgress(UpscalingProgress);
                UpscalingProgress.TotalFiles = args;
            };
            (_upscaler as INotifyProgress).ProgressUpdated += (sender, args) => UpscalingProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.DiscardedFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.ResizedFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);

            UpscalerModel = AvailableModels.SwinIR;

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
        private async Task UpscaleImagesAsync()
        {
            IsUiEnabled = false;

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            TaskStatus = ProcessingStatus.Running;

            try
            {
                IsCancelEnabled = true;
                switch (UpscalerModel)
                {
                    case AvailableModels.SwinIR:
                        await DownloadModelFiles(_fileManipulator, AvailableModels.SwinIR);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.SwinIR);
                        break;
                    case AvailableModels.Swin2SR:
                        await DownloadModelFiles(_fileManipulator, AvailableModels.Swin2SR);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.Swin2SR);
                        break;
                    default:
                        Logger.SetLatestLogMessage($"Something went wrong while trying to load one of the upscaler models!",
                            LogMessageColor.Error);
                        break;
                }
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
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_upscaler as ICancellableService)?.CancelCurrentTask();
        }
    }
}
