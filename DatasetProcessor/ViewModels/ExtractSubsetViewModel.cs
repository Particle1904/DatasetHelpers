using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ExtractSubsetViewModel : BaseViewModel
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
        [ObservableProperty]
        private bool _isCancelEnabled;

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

            InputFolderPath = _configs.Configurations.ExtractSubsetConfigs.InputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.ExtractSubsetConfigs.OutputFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);
            SearchTags = _configs.Configurations.ExtractSubsetConfigs.SearchTxt;
            SearchCaptions = _configs.Configurations.ExtractSubsetConfigs.SearchCaption;
            IsExactFilter = _configs.Configurations.ExtractSubsetConfigs.ExactMatchesFiltering;

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
            catch (OperationCanceledException)
            {
                IsCancelEnabled = false;
                Logger.SetLatestLogMessage($"Cancelled the current operation!", LogMessageColor.Informational);
            }
            catch (FileNotFoundException exception)
            {
                Logger.SetLatestLogMessage(exception.Message, LogMessageColor.Error);
            }
            catch (ArgumentNullException exception)
            {
                Logger.SetLatestLogMessage(exception.Message, LogMessageColor.Error);
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
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManipulator as ICancellableService)?.CancelCurrentTask();
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
