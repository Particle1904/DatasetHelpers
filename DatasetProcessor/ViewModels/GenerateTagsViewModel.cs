using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GenerateTagsViewModel : ViewModelBase
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IAutoTaggerService _autoTagger;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _predictionProgress;
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

        public GenerateTagsViewModel(IFileManipulatorService fileManipulator, IAutoTaggerService autoTagger,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _autoTagger = autoTagger;

            (_autoTagger as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                PredictionProgress = ResetProgress(PredictionProgress);
                PredictionProgress.TotalFiles = args;
            };
            (_autoTagger as INotifyProgress).ProgressUpdated += (sender, args) => PredictionProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.ResizedFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.CombinedOutputFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);

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
            _autoTagger.Threshold = (float)Threshold;

            try
            {
                if (ApplyRedundancyRemoval)
                {
                    if (AppendCaptionsToFile)
                    {
                        await _autoTagger.GenerateTagsAndAppendToFile(InputFolderPath, OutputFolderPath, WeightedCaptions);
                    }
                    else
                    {
                        await _autoTagger.GenerateTags(InputFolderPath, OutputFolderPath, WeightedCaptions);
                    }
                }
                else
                {
                    await _autoTagger.GenerateTagsAndKeepRedundant(InputFolderPath, OutputFolderPath, AppendCaptionsToFile);
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
