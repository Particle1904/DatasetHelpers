using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class ExtractSubsetViewModel : BaseViewModel
    {
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

        private string _outputFolderPath;
        public string OutputFolderPath
        {
            get => _outputFolderPath;
            set
            {
                _outputFolderPath = value;
                OnPropertyChanged(nameof(OutputFolderPath));
            }
        }

        private bool _searchTags;
        public bool SearchTags
        {
            get => _searchTags;
            set
            {
                _searchTags = value;
                OnPropertyChanged(nameof(SearchTags));
            }
        }

        private bool _searchCaptions;
        public bool SearchCaptions
        {
            get => _searchCaptions;
            set
            {
                _searchCaptions = value;
                OnPropertyChanged(nameof(SearchCaptions));
            }
        }

        private bool _isExactFilter;
        public bool IsExactFilter
        {
            get => _isExactFilter;
            set
            {
                _isExactFilter = value;
                OnPropertyChanged(nameof(IsExactFilter));
            }
        }

        private string _tagsToFilter;
        public string TagsToFilter
        {
            get => _tagsToFilter;
            set
            {
                _tagsToFilter = value;
                OnPropertyChanged(nameof(TagsToFilter));
            }
        }

        private Progress _filterProgress;
        public Progress FilterProgress
        {
            get => _filterProgress;
            set
            {
                _filterProgress = value;
                OnPropertyChanged(nameof(FilterProgress));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand OpenOutputFolderCommand { get; private set; }
        public RelayCommand FilterSubsetCommand { get; private set; }

        public ExtractSubsetViewModel(IFileManipulatorService fileManipulatorService)
        {
            _fileManipulatorService = fileManipulatorService;

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));
            FilterSubsetCommand = new RelayCommand(async () => await FilterSubsetAsync());

            SearchTags = true;
            SearchCaptions = true;
        }

        public async Task SelectInputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        public async Task SelectOutputFolderAsync()
        {
            string result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        public async Task FilterSubsetAsync()
        {
            if (FilterProgress == null)
            {
                FilterProgress = new Progress();
            }
            if (FilterProgress.PercentFloat != 0f)
            {
                FilterProgress.Reset();
            }

            TaskStatus = Enums.ProcessingStatus.Running;
            try
            {
                List<string> tagsResult = new List<string>();

                if (SearchTags)
                {
                    tagsResult = await Task.Run(() => _fileManipulatorService.GetFilteredImageFiles(InputFolderPath, ".txt", TagsToFilter, FilterProgress));
                }

                List<string> captionsResult = new List<string>();
                if (SearchCaptions)
                {
                    FilterProgress.Reset();
                    captionsResult = await Task.Run(() => _fileManipulatorService.GetFilteredImageFiles(InputFolderPath, ".caption", TagsToFilter, FilterProgress));
                }

                List<string> result = captionsResult.Union(tagsResult).ToList();

                FilterProgress.Reset();
                await _fileManipulatorService.CreateSubsetAsync(result, OutputFolderPath, FilterProgress);
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(FileNotFoundException))
                {
                    _loggerService.LatestLogMessage = $"{exception.Message}";
                }
                else if (exception.GetType() == typeof(ArgumentNullException))
                {
                    _loggerService.LatestLogMessage = exception.Message;
                }
                else
                {
                    _loggerService.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                    await _loggerService.SaveExceptionStackTrace(exception);
                }
            }
            finally
            {
                TaskStatus = Enums.ProcessingStatus.Finished;
            }
        }
    }
}
