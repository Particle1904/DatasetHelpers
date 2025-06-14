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
    public partial class TextRemoverViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;
        private readonly IModelManagerService _modelManager;
        private readonly ITextRemoverService _textRemover;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _textRemoverProgress;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public TextRemoverViewModel(IFileManagerService fileManager, IModelManagerService modelManager, ITextRemoverService textRemover,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManager = fileManager;
            _modelManager = modelManager;
            _logger = logger;
            _configs = configs;

            _textRemover = textRemover;
            (_textRemover as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                TextRemoverProgress = ResetProgress(TextRemoverProgress);
                TextRemoverProgress.TotalFiles = args;
            };
            (_textRemover as INotifyProgress).ProgressUpdated += (sender, args) => TextRemoverProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.TextRemoverConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.TextRemoverConfigs.OutputFolder;
            _fileManager.CreateFolderIfNotExist(OutputFolderPath);

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
        private async Task RemoveTextFromFilesAsync()
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

                await _textRemover.RemoveTextFromImagesAsync(InputFolderPath, OutputFolderPath);
                timer.Stop();
            }
            catch (OperationCanceledException)
            {
                IsCancelEnabled = false;
                Logger.SetLatestLogMessage("Cancelled the current operation!", LogMessageColor.Informational);
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
                UnloadAllModels();
            }

            // Stop elapsed timer
            _timer.Stop();
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_textRemover as ICancellableService)?.CancelCurrentTask();
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

        /// <summary>
        /// Unload all AI models to free memory.
        /// </summary>
        private void UnloadAllModels()
        {
            (_textRemover as IUnloadModel)?.UnloadAIModel();
        }

        private async Task DownloadRequiredModels()
        {
            Logger.SetLatestLogMessage("Downloading required models...", LogMessageColor.Informational);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2Decoder);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2Encoder);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2EmbedTokens);
            await DownloadModelFiles(_modelManager, AvailableModels.Florence2VisionEncoder);

            await DownloadModelFiles(_modelManager, AvailableModels.SAM2Encoder);
            await DownloadModelFiles(_modelManager, AvailableModels.SAM2Decoder);

            await DownloadModelFiles(_modelManager, AvailableModels.LaMa);
            Logger.SetLatestLogMessage("All required models are downloaded!", LogMessageColor.Informational);
        }
    }
}
