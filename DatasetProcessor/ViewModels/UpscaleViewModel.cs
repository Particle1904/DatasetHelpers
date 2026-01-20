using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces;

using Models.Configurations;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class UpscaleViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;
        private readonly IModelManagerService _modelManager;
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

        public UpscaleViewModel(IFileManagerService fileManager, IModelManagerService modelManager, IUpscalerService upscalerService, ILoggerService logger,
            IConfigsService configs) : base(logger, configs)
        {
            _fileManager = fileManager;
            _modelManager = modelManager;
            _modelManager.DownloadMessageEvent += (sender, args) =>
            {
                if (args is DownloadNotification notification)
                {
                    Logger.SetLatestLogMessage(notification.NotificationMessage, LogMessageColor.Informational, notification.PlayNotificationSound);
                }
            };

            _upscaler = upscalerService;
            (_upscaler as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                UpscalingProgress = ResetProgress(UpscalingProgress);
                UpscalingProgress.TotalFiles = args;
            };
            (_upscaler as INotifyProgress).ProgressUpdated += (sender, args) => UpscalingProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.UpscaleImagesConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.UpscaleImagesConfigs.OutputFolder;
            _fileManager.CreateFolderIfNotExist(OutputFolderPath);
            UpscalerModel = _configs.Configurations.UpscaleImagesConfigs.UpscalerModel;

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

            _timer.Restart();
            DispatcherTimer uiTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            uiTimer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            uiTimer.Start();

            UpscalingProgress = ResetProgress(UpscalingProgress);
            TaskStatus = ProcessingStatus.Running;

            try
            {
                IsCancelEnabled = true;
                switch (UpscalerModel)
                {
                    case AvailableModels.ParimgCompact_x2:
                        await DownloadModelFiles(_modelManager, AvailableModels.ParimgCompact_x2);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.ParimgCompact_x2);
                        break;
                    case AvailableModels.HFA2kCompact_x2:
                        await DownloadModelFiles(_modelManager, AvailableModels.HFA2kCompact_x2);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.HFA2kCompact_x2);
                        break;
                    case AvailableModels.HFA2kAVCSRFormerLight_x2:
                        await DownloadModelFiles(_modelManager, AvailableModels.HFA2kAVCSRFormerLight_x2);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.HFA2kAVCSRFormerLight_x2);
                        break;
                    case AvailableModels.HFA2k_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.HFA2k_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.HFA2k_x4);
                        break;
                    case AvailableModels.SwinIR_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.SwinIR_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.SwinIR_x4);
                        break;
                    case AvailableModels.Swin2SR_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.Swin2SR_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.Swin2SR_x4);
                        break;
                    case AvailableModels.Nomos8kSCSRFormer_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.Nomos8kSCSRFormer_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.Nomos8kSCSRFormer_x4);
                        break;
                    case AvailableModels.Nomos8kSC_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.Nomos8kSC_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.Nomos8kSC_x4);
                        break;
                    case AvailableModels.LSDIRplusReal_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.LSDIRplusReal_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.LSDIRplusReal_x4);
                        break;
                    case AvailableModels.LSDIRplusNone_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.LSDIRplusNone_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.LSDIRplusNone_x4);
                        break;
                    case AvailableModels.LSDIRplusCompression_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.LSDIRplusCompression_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.LSDIRplusCompression_x4);
                        break;
                    case AvailableModels.LSDIRCompact3_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.LSDIRCompact3_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.LSDIRCompact3_x4);
                        break;
                    case AvailableModels.LSDIR_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.LSDIR_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.LSDIR_x4);
                        break;
                    case AvailableModels.Nomos8k_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.Nomos8k_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.Nomos8k_x4);
                        break;
                    case AvailableModels.Nomos8kDAT_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.Nomos8kDAT_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.Nomos8kDAT_x4);
                        break;
                    case AvailableModels.NomosUni_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.NomosUni_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.NomosUni_x4);
                        break;
                    case AvailableModels.RealWebPhoto_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.RealWebPhoto_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.RealWebPhoto_x4);
                        break;
                    case AvailableModels.RealWebPhotoDAT_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.RealWebPhotoDAT_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.RealWebPhotoDAT_x4);
                        break;
                    case AvailableModels.SPANkendata_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.SPANkendata_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.SPANkendata_x4);
                        break;
                    case AvailableModels.GTAV5_x4:
                        await DownloadModelFiles(_modelManager, AvailableModels.GTAV5_x4);
                        await _upscaler.UpscaleImagesAsync(InputFolderPath, OutputFolderPath, AvailableModels.GTAV5_x4);
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
                if (exception.Message.Contains("Not enough memory resources"))
                {
                    Logger.SetLatestLogMessage($"Not enough Memory to process operation! Try reducing the image size.", LogMessageColor.Error);
                }
                else
                {
                    Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                    await Logger.SaveExceptionStackTraceAsync(exception);
                }
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
