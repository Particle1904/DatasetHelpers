using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class PromptGeneratorViewModel : BaseViewModel
    {
        private readonly IPromptGeneratorService _promptGeneratorService;
        private readonly ITagProcessorService _tagProcessorService;

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

        private string _tagsToPrepend;
        public string TagsToPrepend
        {
            get => _tagsToPrepend;
            set
            {
                _tagsToPrepend = value;
                OnPropertyChanged(nameof(TagsToPrepend));
            }
        }

        private string _tagsToAppend;
        public string TagsToAppend
        {
            get => _tagsToAppend;
            set
            {
                _tagsToAppend = value;
                OnPropertyChanged(nameof(TagsToAppend));
            }
        }

        private int _amountOfTags;
        public string AmountOfTags
        {
            get => _amountOfTags.ToString();
            set
            {
                try
                {
                    _amountOfTags = Math.Clamp(int.Parse(value), 5, 50);
                    OnPropertyChanged(nameof(AmountOfTags));
                }
                catch
                {
                    _loggerService.LatestLogMessage = $"Needs to be a number between 5 and 50.";
                }
            }
        }

        private string _generatedPrompt;
        public string GeneratedPrompt
        {
            get => _generatedPrompt;
            set
            {
                _generatedPrompt = value;
                OnPropertyChanged(nameof(GeneratedPrompt));
            }
        }

        private bool _generateButtonEnabled;
        public bool GenerateButtonEnabled
        {
            get => _generateButtonEnabled;
            set
            {
                _generateButtonEnabled = value;
                OnPropertyChanged(nameof(GenerateButtonEnabled));
            }
        }

        private string[] _datasetTags;

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }
        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand OpenOutputFolderCommand { get; private set; }
        public RelayCommand CopyPredictedPromptCommand { get; private set; }
        public RelayCommand GeneratePromptCommand { get; private set; }

        public PromptGeneratorViewModel(IPromptGeneratorService promptGeneratorService, ITagProcessorService tagProcessorService)
        {
            _promptGeneratorService = promptGeneratorService;
            _tagProcessorService = tagProcessorService;

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;
            TagsToPrepend = string.Empty;
            TagsToAppend = "masterpiece, best quality, absurdres";
            GeneratedPrompt = string.Empty;
            _amountOfTags = 20;
            GenerateButtonEnabled = true;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));
            CopyPredictedPromptCommand = new RelayCommand(async () => await CopyToClipboard(GeneratedPrompt));
            GeneratePromptCommand = new RelayCommand(async () => await GeneratePromptAsync());
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

        public async Task GeneratePromptAsync()
        {
            try
            {
                if (_datasetTags == null || _datasetTags.Length == 0)
                {
                    GenerateButtonEnabled = false;
                    _datasetTags = Task.Run(() => _tagProcessorService.GetTagsFromDataset(InputFolderPath)).Result;
                    GenerateButtonEnabled = true;
                }

                GeneratedPrompt = _promptGeneratorService.GeneratePromptFromDataset(_datasetTags, TagsToPrepend, TagsToAppend, _amountOfTags);
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
        }
    }
}