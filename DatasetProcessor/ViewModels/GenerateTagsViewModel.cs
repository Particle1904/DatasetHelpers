using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces.MachineLearning;

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
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IAutoTaggerService _wDAutoTagger;
        private readonly IAutoTaggerService _wDv3AutoTagger;
        private readonly IAutoTaggerService _joyTagAutoTagger;
        private readonly IAutoTaggerService _e621AutoTagger;

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

        public GenerateTagsViewModel(IFileManipulatorService fileManipulator, WDAutoTaggerService wDAutoTagger,
            WDV3AutoTaggerService wDV3AutoTagger, JoyTagAutoTaggerService joyTagAutoTagger,
            E621AutoTaggerService e621AutoTagger, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _fileManipulator.DownloadMessageEvent += (sender, args) => Logger.SetLatestLogMessage(args, LogMessageColor.Informational);

            _wDAutoTagger = wDAutoTagger;
            (_wDAutoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_wDAutoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            _wDv3AutoTagger = wDV3AutoTagger;
            (_wDv3AutoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_wDv3AutoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            _joyTagAutoTagger = joyTagAutoTagger;
            (_joyTagAutoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_joyTagAutoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            _e621AutoTagger = e621AutoTagger;
            (_e621AutoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_e621AutoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.ResizedFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.CombinedOutputFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);

            GeneratorModel = AvailableModels.JoyTag;
            Threshold = _configs.Configurations.TaggerThreshold;

            WeightedCaptions = false;
            AppendCaptionsToFile = false;
            ApplyRedundancyRemoval = true;

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
                        await DownloadModelFiles(AvailableModels.JoyTag);
                        await CallAutoTaggerService(_joyTagAutoTagger);
                        break;
                    case AvailableModels.WD14v2:
                        await DownloadModelFiles(AvailableModels.WD14v2);
                        await CallAutoTaggerService(_wDAutoTagger);
                        break;
                    case AvailableModels.WDv3:
                        await DownloadModelFiles(AvailableModels.WDv3);
                        await CallAutoTaggerService(_wDv3AutoTagger);
                        break;
                    case AvailableModels.Z3DE621:
                        await DownloadModelFiles(AvailableModels.Z3DE621);
                        await CallAutoTaggerService(_e621AutoTagger);
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

        private async Task DownloadModelFiles(AvailableModels model)
        {
            if (_fileManipulator.FileNeedsToBeDownloaded(model))
            {
                await _fileManipulator.DownloadModelFile(model);
            }
        }

        /// <summary>
        /// Calls the AutoTagger service to generate tags based on the specified parameters.
        /// </summary>
        /// <param name="autoTagger">The instance of the AutoTagger service to use.</param>
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
            (_wDAutoTagger as IUnloadModel)?.UnloadAIModel();
            (_wDv3AutoTagger as IUnloadModel)?.UnloadAIModel();
            (_joyTagAutoTagger as IUnloadModel)?.UnloadAIModel();
            (_e621AutoTagger as IUnloadModel)?.UnloadAIModel();
        }

        partial void OnThresholdChanged(double value)
        {
            Threshold = Math.Round(value, 2);
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManipulator as ICancellableService)?.CancelCurrentTask();
            (_wDAutoTagger as ICancellableService)?.CancelCurrentTask();
            (_wDv3AutoTagger as ICancellableService)?.CancelCurrentTask();
            (_joyTagAutoTagger as ICancellableService)?.CancelCurrentTask();
            (_e621AutoTagger as ICancellableService)?.CancelCurrentTask();
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
