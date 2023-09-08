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
                    _loggerService.LatestLogMessage = $"Amount of tags needs to be a number between 5 and 50.";
                }
            }
        }

        private int _amountOfGeneratedPrompts;
        public string AmountOfGeneratedPrompts
        {
            get => _amountOfGeneratedPrompts.ToString();
            set
            {
                try
                {
                    _amountOfGeneratedPrompts = Math.Clamp(int.Parse(value), 10, ushort.MaxValue);
                    OnPropertyChanged(nameof(AmountOfTags));
                }
                catch
                {
                    _loggerService.LatestLogMessage = $"Amount of Prompts to Generate needs to be a number between 10 and 65535.";
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
        public RelayCommand GeneratePromptsCommand { get; private set; }

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
            _amountOfGeneratedPrompts = 1000;
            GenerateButtonEnabled = true;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));
            CopyPredictedPromptCommand = new RelayCommand(async () => await CopyToClipboard(GeneratedPrompt));
            GeneratePromptCommand = new RelayCommand(async () => await GeneratePromptAsync());
            GeneratePromptsCommand = new RelayCommand(async () => await GeneratePromptsAsync());
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
                    _datasetTags = await Task.Run(() => _tagProcessorService.GetTagsFromDataset(InputFolderPath));
                    GenerateButtonEnabled = true;
                }

                string generatedPrompt = await Task.Run(() => _promptGeneratorService.GeneratePromptFromDataset(_datasetTags, TagsToPrepend, TagsToAppend, _amountOfTags));
                GeneratedPrompt = _tagProcessorService.ApplyRedundancyRemoval(generatedPrompt);
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

        public async Task GeneratePromptsAsync()
        {
            try
            {
                GenerateButtonEnabled = false;
                if (_datasetTags == null || _datasetTags.Length == 0)
                {

                    _datasetTags = Task.Run(() => _tagProcessorService.GetTagsFromDataset(InputFolderPath)).Result;

                }

                string outputPath = Path.Combine(OutputFolderPath, "generatedPrompts.txt");

                await Task.Run(() => _promptGeneratorService.GeneratePromptsAndSaveToFile(outputPath, _datasetTags, TagsToPrepend,
                    TagsToAppend, _amountOfTags, _amountOfGeneratedPrompts));
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
                GenerateButtonEnabled = true;
            }
        }
    }
}