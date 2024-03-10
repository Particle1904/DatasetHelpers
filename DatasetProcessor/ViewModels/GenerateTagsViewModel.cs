using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

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
        private readonly IAutoTaggerService _joyTagAutoTagger;
        private readonly IAutoTaggerService _e621AutoTagger;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _predictionProgress;
        [ObservableProperty]
        private TagGeneratorModel _generatorModel;
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

        public GenerateTagsViewModel(IFileManipulatorService fileManipulator, WDAutoTaggerService wDAutoTagger,
            JoyTagAutoTaggerService joyTagAutoTagger, E621AutoTaggerService e621AutoTagger,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
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

            GeneratorModel = TagGeneratorModel.JoyTag;
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
                    case TagGeneratorModel.JoyTag:
                        await DownloadModelFiles(AvailableModels.JoyTag);
                        await CallAutoTaggerService(_joyTagAutoTagger);
                        break;
                    case TagGeneratorModel.WDv14:
                        await DownloadModelFiles(AvailableModels.WD_v1_4);
                        await CallAutoTaggerService(_wDAutoTagger);
                        break;
                    case TagGeneratorModel.Z3DE621:
                        await DownloadModelFiles(AvailableModels.Z3D_E621);
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
        /// <param name="autoTaggerService">The instance of the AutoTagger service to use.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CallAutoTaggerService(IAutoTaggerService autoTaggerService)
        {
            autoTaggerService.Threshold = (float)Threshold;

            if (ApplyRedundancyRemoval)
            {
                if (AppendCaptionsToFile)
                {
                    await autoTaggerService.GenerateTagsAndAppendToFile(InputFolderPath, OutputFolderPath, WeightedCaptions);
                }
                else
                {
                    await autoTaggerService.GenerateTags(InputFolderPath, OutputFolderPath, WeightedCaptions);
                }
            }
            else
            {
                await autoTaggerService.GenerateTagsAndKeepRedundant(InputFolderPath, OutputFolderPath, AppendCaptionsToFile);
            }
        }

        partial void OnThresholdChanged(double value)
        {
            Threshold = Math.Round(value, 2);
        }

    }
}
