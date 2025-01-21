using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Exceptions;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GeminiCaptionViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IGeminiService _gemini;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private string _failedFolderPath;
        [ObservableProperty]
        private string _geminiApi;
        [ObservableProperty]
        private bool _freeApi;
        [ObservableProperty]
        private string _geminiPrompt;
        [ObservableProperty]
        private string _geminiSystemInstruction;

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

        public GeminiCaptionViewModel(IFileManipulatorService fileManipulator, IGeminiService gemini, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _gemini = gemini;

            InputFolderPath = _configs.Configurations.GeminiCaptionConfigs.InputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.GeminiCaptionConfigs.OutputFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);
            FailedFolderPath = _configs.Configurations.GeminiCaptionConfigs.FailedFolder;
            _fileManipulator.CreateFolderIfNotExist(FailedFolderPath);
            GeminiApi = _configs.Configurations.GeminiCaptionConfigs.ApiKey;
            FreeApi = _configs.Configurations.GeminiCaptionConfigs.FreeApi;
            GeminiPrompt = _configs.Configurations.GeminiCaptionConfigs.Prompt;
            GeminiSystemInstruction = _configs.Configurations.GeminiCaptionConfigs.SystemInstructions;

            (_gemini as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                CaptionProcessingProgress = ResetProgress(CaptionProcessingProgress);
                CaptionProcessingProgress.TotalFiles = args;
            };
            (_gemini as INotifyProgress).ProgressUpdated += (sender, args) => CaptionProcessingProgress.UpdateProgress();

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
        private async Task SelectFailedFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                FailedFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task CaptionWithGeminiAsync()
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
                _gemini.ApiKey = GeminiApi;
                _gemini.SystemInstructions = GeminiSystemInstruction;
                _gemini.FreeApi = FreeApi;

                await _gemini.CaptionImagesAsync(InputFolderPath, OutputFolderPath, FailedFolderPath, GeminiPrompt);

                timer.Stop();
            }
            catch (InvalidGeminiAPIKeyException)
            {
                Logger.SetLatestLogMessage("Invalid AIStudio API Key! Please verify the API Key.", LogMessageColor.Error);
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

            // Stop elapsed timer
            _timer.Stop();
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManipulator as ICancellableService)?.CancelCurrentTask();
            (_gemini as ICancellableService)?.CancelCurrentTask();
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
