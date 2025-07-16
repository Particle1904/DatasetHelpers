﻿using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces;
using Interfaces.MachineLearning;

using Models.Configurations;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services.MachineLearning;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GenerateTagsViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;
        private readonly IModelManagerService _modelManager;
        private readonly IAutoTaggerService _wDautoTagger;
        private readonly IAutoTaggerService _wDv3autoTagger;
        private readonly IAutoTaggerService _wDv3largeAutoTagger;
        private readonly IAutoTaggerService _joyTagautoTagger;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _predictionProgress;
        [ObservableProperty]
        private AvailableModels _generatorModel;
        [ObservableProperty]
        private double _threshold;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        [ObservableProperty]
        private bool _weightedCaptions;
        [ObservableProperty]
        private bool _appendCaptionsToFile;
        [ObservableProperty]
        private bool _applyRedundancyRemoval;
        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public GenerateTagsViewModel(IFileManagerService fileManager, IModelManagerService modelManager, WDAutoTaggerService wDautoTagger,
            WDV3AutoTaggerService wDV3autoTagger, JoyTagAutoTaggerService joyTagautoTagger, WDV3LargeAutoTaggerService wDv3largeAutoTagger,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
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

            _wDautoTagger = wDautoTagger;
            (_wDautoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_wDautoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            _wDv3autoTagger = wDV3autoTagger;
            (_wDv3autoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_wDv3autoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            _wDv3largeAutoTagger = wDv3largeAutoTagger;
            (_wDv3largeAutoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_wDv3largeAutoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            _joyTagautoTagger = joyTagautoTagger;
            (_joyTagautoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_joyTagautoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.GenerateTagsConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.UpscaleImagesConfigs.OutputFolder;
            _fileManager.CreateFolderIfNotExist(OutputFolderPath);
            GeneratorModel = _configs.Configurations.GenerateTagsConfigs.AutoTaggerModel;
            Threshold = _configs.Configurations.GenerateTagsConfigs.PredictionsThreshold;
            WeightedCaptions = _configs.Configurations.GenerateTagsConfigs.WeightedCaptions;
            AppendCaptionsToFile = _configs.Configurations.GenerateTagsConfigs.AppendToExistingFile;
            ApplyRedundancyRemoval = _configs.Configurations.GenerateTagsConfigs.ApplyRedudancyRemoval;

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
        private async Task MakePredictionsAsync()
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
                switch (GeneratorModel)
                {
                    case AvailableModels.JoyTag:
                        await DownloadModelFiles(_modelManager, AvailableModels.JoyTag);
                        await CallAutoTaggerService(_joyTagautoTagger);
                        break;
                    case AvailableModels.WD14v2:
                        await DownloadModelFiles(_modelManager, AvailableModels.WD14v2);
                        await CallAutoTaggerService(_wDautoTagger);
                        break;
                    case AvailableModels.WDv3:
                        await DownloadModelFiles(_modelManager, AvailableModels.WDv3);
                        await CallAutoTaggerService(_wDv3autoTagger);
                        break;
                    case AvailableModels.WDv3Large:
                        await DownloadModelFiles(_modelManager, AvailableModels.WDv3Large);
                        await CallAutoTaggerService(_wDv3largeAutoTagger);
                        break;
                    default:
                        Logger.SetLatestLogMessage($"Something went wrong while trying to load one of the auto tagger models!",
                            LogMessageColor.Error);
                        break;
                }

                // Stop dispatcher timer.
                timer.Stop();
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
                UnloadAllModels();
            }

            // Stop elapsed timer
            _timer.Stop();
        }

        /// <summary>
        /// Calls the autoTagger service to generate tags based on the specified parameters.
        /// </summary>
        /// <param name="autoTagger">The instance of the autoTagger service to use.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CallAutoTaggerService(IAutoTaggerService autoTagger)
        {
            autoTagger.Threshold = (float)Threshold;

            if (ApplyRedundancyRemoval)
            {
                if (AppendCaptionsToFile)
                {
                    await autoTagger.GenerateTagsAndAppendToFile(InputFolderPath, OutputFolderPath, WeightedCaptions);
                }
                else
                {
                    await autoTagger.GenerateTags(InputFolderPath, OutputFolderPath, WeightedCaptions);
                }
            }
            else
            {
                await autoTagger.GenerateTagsAndKeepRedundant(InputFolderPath, OutputFolderPath, AppendCaptionsToFile);
            }
        }

        /// <summary>
        /// Unload all AI models to free memory.
        /// </summary>
        private void UnloadAllModels()
        {
            (_wDautoTagger as IUnloadModel)?.UnloadAIModel();
            (_wDv3autoTagger as IUnloadModel)?.UnloadAIModel();
            (_wDv3largeAutoTagger as IUnloadModel)?.UnloadAIModel();
            (_joyTagautoTagger as IUnloadModel)?.UnloadAIModel();
        }

        partial void OnThresholdChanged(double value)
        {
            Threshold = Math.Round(value, 2);
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManager as ICancellableService)?.CancelCurrentTask();
            (_wDautoTagger as ICancellableService)?.CancelCurrentTask();
            (_wDv3autoTagger as ICancellableService)?.CancelCurrentTask();
            (_wDv3largeAutoTagger as ICancellableService)?.CancelCurrentTask();
            (_joyTagautoTagger as ICancellableService)?.CancelCurrentTask();
        }

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

        partial void OnAppendCaptionsToFileChanged(bool value)
        {
            ApplyRedundancyRemoval = value;
        }
    }
}
