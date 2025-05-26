using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces;
using Interfaces.MachineLearning;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class FlorenceCaptionViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;
        private readonly IModelManagerService _modelManager;
        private readonly IFlorence2Service _florence2;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Florence2CaptionTask _captionTask;
        [ObservableProperty]
        private Progress _captionProcessingProgress;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public FlorenceCaptionViewModel(IFileManagerService fileManager, IModelManagerService modelManager, IFlorence2Service florence2, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManager = fileManager;
            _modelManager = modelManager;
            _florence2 = florence2;

            CaptionTask = Florence2CaptionTask.Caption;

            InputFolderPath = _configs.Configurations.Florence2CaptionConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.Florence2CaptionConfigs.OutputFolder;
            _fileManager.CreateFolderIfNotExist(OutputFolderPath);

            (_florence2 as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                CaptionProcessingProgress = ResetProgress(CaptionProcessingProgress);
                CaptionProcessingProgress.TotalFiles = args;
            };
            (_florence2 as INotifyProgress).ProgressUpdated += (sender, args) => CaptionProcessingProgress.UpdateProgress();

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
        private async Task CaptionWithFlorence2Async()
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
                await DownloadRequiredModels();

                await _florence2.CaptionImagesAsync(InputFolderPath, OutputFolderPath, CaptionTask);

                timer.Stop();
            }
            catch (OperationCanceledException)
            {
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
            (_fileManager as ICancellableService)?.CancelCurrentTask();
            (_florence2 as ICancellableService)?.CancelCurrentTask();
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

        private async Task DownloadRequiredModels()
        {
            Logger.SetLatestLogMessage("Downloading required models...", LogMessageColor.Informational);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2Decoder);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2Encoder);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2EmbedTokens);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2VisionEncoder);
            Logger.SetLatestLogMessage("All required models are downloaded!", LogMessageColor.Informational);
        }
    }
}
