﻿using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagProcessingViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
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

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand ProcessTagsCommand { get; private set; }

        public TagProcessingViewModel(IFolderPickerService folderPickerService, ITagProcessorService tagProcessorService, IFileManipulatorService fileManipulatorService)
        {
            _folderPickerService = folderPickerService;
            _tagProcessorService = tagProcessorService;
            _fileManipulatorService = fileManipulatorService;

            _inputFolderPath = Path.Combine(AppContext.BaseDirectory, "combined-images-output");
            _fileManipulatorService.CreateFolderIfNotExist(_inputFolderPath);

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            ProcessTagsCommand = new RelayCommand(async () => await ProcessTagsAsync());

            RandomizeTags = false;
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
            if (TagProcessingProgress.PercentFloat >= 1.0f)
            {
                TagProcessingProgress.Reset();
            }

            TaskStatus = Enums.ProcessingStatus.Running;
            await _tagProcessorService.ProcessAllTagFiles(InputFolderPath, TagsToAdd, TagsToEmphasize, TagsToRemove, TagProcessingProgress);
            if (RandomizeTags)
            {
                await _tagProcessorService.RandomizeTagsOfFiles(InputFolderPath);
            }
            TaskStatus = Enums.ProcessingStatus.Finished;
        }
    }
}
