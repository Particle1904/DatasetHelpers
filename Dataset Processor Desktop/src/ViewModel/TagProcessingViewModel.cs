using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagProcessingViewModel : BaseViewModel
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

        private string _tagsToAdd;
        public string TagsToAdd
        {
            get => _tagsToAdd;
            set
            {
                _tagsToAdd = value;
                OnPropertyChanged(nameof(TagsToAdd));
            }
        }

        private string _tagsToEmphasize;
        public string TagsToEmphasize
        {
            get => _tagsToEmphasize;
            set
            {
                _tagsToEmphasize = value;
                OnPropertyChanged(nameof(TagsToEmphasize));
            }
        }

        private string _tagsToRemove;
        public string TagsToRemove
        {
            get => _tagsToRemove;
            set
            {
                _tagsToRemove = value;
                OnPropertyChanged(nameof(TagsToRemove));
            }
        }

        private string _tagsToReplace;
        public string TagsToReplace
        {
            get => _tagsToReplace;
            set
            {
                _tagsToReplace = value;
                OnPropertyChanged(nameof(TagsToReplace));
            }
        }

        private string _tagsToBeReplaced;
        public string TagsToBeReplaced
        {
            get => _tagsToBeReplaced;
            set
            {
                _tagsToBeReplaced = value;
                OnPropertyChanged(nameof(TagsToBeReplaced));
            }
        }

        private Progress _tagProcessingProgress;
        public Progress TagProcessingProgress
        {
            get => _tagProcessingProgress;
            set
            {
                _tagProcessingProgress = value;
                OnPropertyChanged(nameof(TagProcessingProgress));
            }
        }

        private bool _randomizeTags;
        public bool RandomizeTags
        {
            get => _randomizeTags;
            set
            {
                _randomizeTags = value;
                OnPropertyChanged(nameof(RandomizeTags));
            }
        }

        private bool _renameFilesToCrescent;
        public bool RenameFilesToCrescent
        {
            get => _renameFilesToCrescent;
            set
            {
                _renameFilesToCrescent = value;
                OnPropertyChanged(nameof(RenameFilesToCrescent));
            }
        }

        private bool _applyRedundancyRemoval;
        public bool ApplyRedundancyRemoval
        {
            get => _applyRedundancyRemoval;
            set
            {
                _applyRedundancyRemoval = value;
                OnPropertyChanged(nameof(ApplyRedundancyRemoval));
            }
        }

        private string _sortedByFrequency;
        public string SortedByFrequency
        {
            get => _sortedByFrequency;
            set
            {
                _sortedByFrequency = value;
                OnPropertyChanged(nameof(SortedByFrequency));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand ProcessTagsCommand { get; private set; }
        public RelayCommand CalculateByFrequencyCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }

        public TagProcessingViewModel(ITagProcessorService tagProcessorService, IFileManipulatorService fileManipulatorService)
        {
            _tagProcessorService = tagProcessorService;
            _fileManipulatorService = fileManipulatorService;

            InputFolderPath = _configsService.Configurations.CombinedOutputFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            ProcessTagsCommand = new RelayCommand(async () => await ProcessTagsAsync());
            CalculateByFrequencyCommand = new RelayCommand(CalculateByFrequencyAsync);
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));

            SortedByFrequency = "Click the button in the right to process tags by frequency.\nThis will use the .txt files in the input folder.";

            RandomizeTags = false;
            RenameFilesToCrescent = true;
            ApplyRedundancyRemoval = false;
        }

        public async Task SelectInputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        public async Task ProcessTagsAsync()
        {
            if (TagProcessingProgress == null)
            {
                TagProcessingProgress = new Progress();
            }
            if (TagProcessingProgress.PercentFloat != 0f)
            {
                TagProcessingProgress.Reset();
            }

            TaskStatus = Enums.ProcessingStatus.Running;
            try
            {
                await _tagProcessorService.ProcessAllTagFiles(InputFolderPath, TagsToAdd, TagsToEmphasize, TagsToRemove, TagProcessingProgress);
                if (RandomizeTags)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessorService.RandomizeTagsOfFiles(InputFolderPath, TagProcessingProgress);
                }
                if (RenameFilesToCrescent)
                {
                    TagProcessingProgress.Reset();
                    await _fileManipulatorService.RenameAllToCrescentAsync(InputFolderPath, TagProcessingProgress);
                }
                if (ApplyRedundancyRemoval)
                {
                    TagProcessingProgress.Reset();
                    await _tagProcessorService.ApplyRedundancyRemovalToFiles(InputFolderPath, TagProcessingProgress);
                }
            }
            catch (Exception exception)
            {
                if (exception.GetType() == typeof(IOException))
                {
                    _loggerService.LatestLogMessage = $"Images and Tag files are named in crescent order already!";
                }
                else
                {
                    _loggerService.LatestLogMessage = $"Something went wrong! {exception.StackTrace}";
                }
            }
            finally
            {
                TaskStatus = Enums.ProcessingStatus.Finished;
            }

            if (!string.IsNullOrEmpty(TagsToReplace))
            {
                TaskStatus = Enums.ProcessingStatus.Running;
                TagProcessingProgress.Reset();

                try
                {
                    await _tagProcessorService.ProcessTagsReplacement(InputFolderPath, TagsToReplace, TagsToBeReplaced, TagProcessingProgress);
                }
                catch (Exception exception)
                {
                    if (exception.GetType() == typeof(ArgumentException))
                    {
                        _loggerService.LatestLogMessage = exception.Message;
                    }
                }
                finally
                {
                    TaskStatus = Enums.ProcessingStatus.Finished;
                }
            }
        }

        public void CalculateByFrequencyAsync()
        {
            try
            {
                SortedByFrequency = _tagProcessorService.CalculateListOfMostFrequentTags(InputFolderPath);
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong! {exception.StackTrace}";
            }
        }
    }
}
