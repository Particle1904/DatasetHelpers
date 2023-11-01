using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ExtractSubsetViewModel : ViewModelBase
    {
        private readonly IFileManipulatorService _fileManipulator;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private bool _searchTags;
        [ObservableProperty]
        private bool _searchCaptions;
        [ObservableProperty]
        private bool _isExactFilter;
        [ObservableProperty]
        private string _tagsToFilter;
        [ObservableProperty]
        private Progress _filterProgress;
        [ObservableProperty]
        private bool _isUiEnabled;

        public ExtractSubsetViewModel(IFileManipulatorService fileManipulator, ILoggerService logger,
            IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;

            (_fileManipulator as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                FilterProgress = ResetProgress(FilterProgress);
                FilterProgress.TotalFiles = args;
            };
            (_fileManipulator as INotifyProgress).ProgressUpdated += (sender, args) => FilterProgress.UpdateProgress();

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;

            SearchTags = true;
            SearchCaptions = true;
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
        private async Task FilterSubsetAsync()
        {
            IsUiEnabled = false;

            if (FilterProgress == null)
            {
                FilterProgress = new Progress();
            }
            if (FilterProgress.PercentFloat != 0f)
            {
                FilterProgress.Reset();
            }

            TaskStatus = ProcessingStatus.Running;
            try
            {
                List<string> tagsResult = new List<string>();

                if (SearchTags)
                {
                    tagsResult = await Task.Run(() => _fileManipulator.GetFilteredImageFiles(InputFolderPath, ".txt", TagsToFilter, IsExactFilter));
                }

                List<string> captionsResult = new List<string>();
                if (SearchCaptions)
                {
                    FilterProgress.Reset();
                    captionsResult = await Task.Run(() => _fileManipulator.GetFilteredImageFiles(InputFolderPath, ".caption", TagsToFilter));
                }

                List<string> result = captionsResult.Union(tagsResult).ToList();

                FilterProgress.Reset();
                await _fileManipulator.CreateSubsetAsync(result, OutputFolderPath);
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(FileNotFoundException))
                {
                    Logger.LatestLogMessage = $"{exception.Message}";
                }
                else if (exception.GetType() == typeof(ArgumentNullException))
                {
                    Logger.LatestLogMessage = exception.Message;
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
        }
    }
}
