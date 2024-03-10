using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class ProcessCaptionsViewModel : BaseViewModel
    {
        private readonly ITagProcessorService _tagProcessor;
        private readonly IFileManipulatorService _fileManipulator;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _wordsToBeReplaced;
        [ObservableProperty]
        private string _wordsToReplace;
        [ObservableProperty]
        private Progress _captionProcessingProgress;
        [ObservableProperty]
        private bool _isUiEnabled;

        public ProcessCaptionsViewModel(ITagProcessorService tagProcessor, IFileManipulatorService fileManipulator,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _tagProcessor = tagProcessor;
            _fileManipulator = fileManipulator;

            (_tagProcessor as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                CaptionProcessingProgress = ResetProgress(CaptionProcessingProgress);
                CaptionProcessingProgress.TotalFiles = args;
            };
            (_tagProcessor as INotifyProgress).ProgressUpdated += (sender, args) => CaptionProcessingProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.CombinedOutputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);

            IsUiEnabled = true;
            TaskStatus = ProcessingStatus.Idle;
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
        public async Task ProcessCaptionsAsync()
        {
            IsUiEnabled = false;

            CaptionProcessingProgress = ResetProgress(CaptionProcessingProgress);

            TaskStatus = ProcessingStatus.Running;
            try
            {
                await _tagProcessor.FindAndReplace(InputFolderPath, WordsToBeReplaced, WordsToReplace);
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(ArgumentException))
                {
                    Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                }
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }
        }
    }
}
