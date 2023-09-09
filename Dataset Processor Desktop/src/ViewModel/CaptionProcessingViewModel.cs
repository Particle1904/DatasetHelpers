using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class CaptionProcessingViewModel : BaseViewModel
    {
        private readonly ITagProcessorService _tagProcessorService;
        private readonly IFileManipulatorService _fileManipulatorService;

        private string _inputFolderPath;
        public string InputFolderPath
        {
            get => _inputFolderPath;
            set
            {
                _inputFolderPath = value;
                OnPropertyChanged(nameof(InputFolderPath));
            }
        }

        private string _wordsToBeReplaced;
        public string WordsToBeReplaced
        {
            get => _wordsToBeReplaced;
            set
            {
                _wordsToBeReplaced = value;
                OnPropertyChanged(nameof(WordsToBeReplaced));
            }
        }

        private string _wordsToReplace;
        public string WordsToReplace
        {
            get => _wordsToReplace;
            set
            {
                _wordsToReplace = value;
                OnPropertyChanged(nameof(WordsToReplace));
            }
        }

        private Progress _captionProcessingProgress;
        public Progress CaptionProcessingProgress
        {
            get => _captionProcessingProgress;
            set
            {
                _captionProcessingProgress = value;
                OnPropertyChanged(nameof(CaptionProcessingProgress));
            }
        }

        private bool _isUiEnabled;
        public bool IsUiEnabled
        {
            get => _isUiEnabled;
            set
            {
                _isUiEnabled = value;
                OnPropertyChanged(nameof(IsUiEnabled));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand ProcessCaptionsCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }

        public CaptionProcessingViewModel(ITagProcessorService tagProcessorService, IFileManipulatorService fileManipulatorService)
        {
            _tagProcessorService = tagProcessorService;
            _fileManipulatorService = fileManipulatorService;

            InputFolderPath = _configsService.Configurations.CombinedOutputFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            ProcessCaptionsCommand = new RelayCommand(async () => await ProcessCaptionsAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));

            IsUiEnabled = true;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        public async Task ProcessCaptionsAsync()
        {
            IsUiEnabled = false;

            if (CaptionProcessingProgress == null)
            {
                CaptionProcessingProgress = new Progress();
            }
            if (CaptionProcessingProgress.PercentFloat != 0f)
            {
                CaptionProcessingProgress.Reset();
            }

            TaskStatus = Enums.ProcessingStatus.Running;
            try
            {
                await _tagProcessorService.FindAndReplace(InputFolderPath, WordsToBeReplaced, WordsToReplace, CaptionProcessingProgress);
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(ArgumentException))
                {
                    _loggerService.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                }
                await _loggerService.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = Enums.ProcessingStatus.Finished;
            }
        }
    }
}
