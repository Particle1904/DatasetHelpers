using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Services;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GenerateTagsViewModel : ViewModelBase
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IAutoTaggerService _wDAutoTagger;
        private readonly IAutoTaggerService _joyTagAutoTagger;

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
            JoyTagAutoTaggerService joyTagAutoTagger,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;

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
                        _joyTagAutoTagger.Threshold = (float)Threshold;
                        if (ApplyRedundancyRemoval)
                        {
                            if (AppendCaptionsToFile)
                            {
                                await _joyTagAutoTagger.GenerateTagsAndAppendToFile(InputFolderPath, OutputFolderPath, WeightedCaptions);
                            }
                            else
                            {
                                await _joyTagAutoTagger.GenerateTags(InputFolderPath, OutputFolderPath, WeightedCaptions);
                            }
                        }
                        else
                        {
                            await _joyTagAutoTagger.GenerateTagsAndKeepRedundant(InputFolderPath, OutputFolderPath, AppendCaptionsToFile);
                        }
                        break;
                    case TagGeneratorModel.WDv1_4:
                        _wDAutoTagger.Threshold = (float)Threshold;

                        if (ApplyRedundancyRemoval)
                        {
                            if (AppendCaptionsToFile)
                            {
                                await _wDAutoTagger.GenerateTagsAndAppendToFile(InputFolderPath, OutputFolderPath, WeightedCaptions);
                            }
                            else
                            {
                                await _wDAutoTagger.GenerateTags(InputFolderPath, OutputFolderPath, WeightedCaptions);
                            }
                        }
                        else
                        {
                            await _wDAutoTagger.GenerateTagsAndKeepRedundant(InputFolderPath, OutputFolderPath, AppendCaptionsToFile);
                        }
                        break;
                    default:
                        Logger.LatestLogMessage = $"Something went wrong while trying to load one of the auto tagger models!";
                        break;
                }

                timer.Stop();
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

        partial void OnThresholdChanged(double value)
        {
            Threshold = Math.Round(value, 2);
        }
    }
}
