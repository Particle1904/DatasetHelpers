using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ProcessTagsViewModel : ViewModelBase
    {
        private readonly ITagProcessorService _tagProcessor;
        private readonly IFileManipulatorService _fileManipulator;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _tagsToAdd;
        [ObservableProperty]
        private string _tagsToEmphasize;
        [ObservableProperty]
        private string _tagsToRemove;
        [ObservableProperty]
        private string _tagsToReplace;
        [ObservableProperty]
        private string _tagsToBeReplaced;
        [ObservableProperty]
        private Progress _tagProcessingProgress;
        [ObservableProperty]
        private bool _randomizeTags;
        [ObservableProperty]
        private bool _renameFilesToCrescent;
        [ObservableProperty]
        private bool _applyRedundancyRemoval;
        [ObservableProperty]
        private bool _applyConsolidateTags;
        [ObservableProperty]
        private string _sortedByFrequency;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        [ObservableProperty]
        private bool _isUiEnabled;

        public ProcessTagsViewModel(ITagProcessorService tagProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _tagProcessor = tagProcessor;
            _fileManipulator = fileManipulator;

            InputFolderPath = _configs.Configurations.CombinedOutputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);

            SortedByFrequency = string.Empty;

            RandomizeTags = false;
            RenameFilesToCrescent = false;
            ApplyRedundancyRemoval = false;

            _timer = new Stopwatch();

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
        private async Task ProcessTagsAsync()
        {
            IsUiEnabled = false;

            TagProcessingProgress = ResetProgress(TagProcessingProgress);

            _timer.Reset();
            TaskStatus = ProcessingStatus.Running;

            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            try
            {
                await _tagProcessor.ProcessAllTagFiles(InputFolderPath, TagsToAdd, TagsToEmphasize, TagsToRemove, TagProcessingProgress);
                if (RandomizeTags)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessor.RandomizeTagsOfFiles(InputFolderPath, TagProcessingProgress);
                }
                if (RenameFilesToCrescent)
                {
                    TagProcessingProgress.Reset();
                    await _fileManipulator.RenameAllToCrescentAsync(InputFolderPath, TagProcessingProgress);
                }
                if (ApplyConsolidateTags)
                {
                    TagProcessingProgress.Reset();
                    //await _tagProcessorService.ConsolidateTags(InputFolderPath);
                    await _tagProcessor.ConsolidateTagsAndLogEdgeCases(InputFolderPath, Logger, TagProcessingProgress);
                }
                if (ApplyRedundancyRemoval)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessor.ApplyRedundancyRemovalToFiles(InputFolderPath, TagProcessingProgress);
                }
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(IOException))
                {
                    Logger.LatestLogMessage = $"Images and Tag files are named in crescent order already!";
                }
                else
                {
                    Logger.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                    await Logger.SaveExceptionStackTrace(exception);
                }
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }

            if (!string.IsNullOrEmpty(TagsToReplace))
            {
                TaskStatus = ProcessingStatus.Running;
                TagProcessingProgress.Reset();

                try
                {
                    await _tagProcessor.ProcessTagsReplacement(InputFolderPath, TagsToBeReplaced, TagsToReplace, TagProcessingProgress);
                }
                catch (Exception exception)
                {
                    if (exception.GetType() == typeof(ArgumentException))
                    {
                        Logger.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                    }
                    await Logger.SaveExceptionStackTrace(exception);
                }
                finally
                {
                    IsUiEnabled = true;
                    TaskStatus = ProcessingStatus.Finished;
                }
            }

            _timer.Stop();
            timer.Stop();
        }

        [RelayCommand]
        private async Task CalculateByFrequencyAsync()
        {
            try
            {
                SortedByFrequency = _tagProcessor.CalculateListOfMostFrequentTags(InputFolderPath);
            }
            catch (Exception exception)
            {
                Logger.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                await Logger.SaveExceptionStackTrace(exception);
            }
        }
    }
}
