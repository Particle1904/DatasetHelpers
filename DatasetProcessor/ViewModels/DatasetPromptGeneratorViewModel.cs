using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.IO;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class DatasetPromptGeneratorViewModel : ViewModelBase
    {
        private const string _invalidMinPromptsNumberMessage = "Amount of Prompts needs to be a number between 10 and 65535.";

        private readonly IPromptGeneratorService _promptGenerator;
        private readonly ITagProcessorService _tagProcessor;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private Progress _generationProgress;
        [ObservableProperty]
        private string _tagsToPrepend;
        [ObservableProperty]
        private string _tagsToAppend;
        [ObservableProperty]
        private string _generatedPrompt;

        private int? _amountOfGeneratedPrompts;
        public string AmountOfGeneratedPrompts
        {
            get => _amountOfGeneratedPrompts.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    if (parsedValue < 10 || parsedValue > ushort.MaxValue)
                    {
                        Logger.LatestLogMessage = $"{_invalidMinPromptsNumberMessage}{Environment.NewLine}This value will be clampled to a valid number before processing!";
                    }
                    else
                    {
                        Logger.LatestLogMessage = string.Empty;
                    }

                    _amountOfGeneratedPrompts = parsedValue;
                    OnPropertyChanged(nameof(AmountOfGeneratedPrompts));
                }
                catch
                {
                    _amountOfGeneratedPrompts = null;
                    Logger.LatestLogMessage = $"{_invalidMinPromptsNumberMessage}{Environment.NewLine}This value cannot be empty! Use at least 10 as its minimum valid number.";
                }
            }
        }

        private int? _amountOfTags;
        public string AmountOfTags
        {
            get => _amountOfTags.ToString();
            set
            {
                try
                {
                    int parsedValue = int.Parse(value);
                    if (parsedValue < 10 || parsedValue > 50)
                    {
                        Logger.LatestLogMessage = $"Amount of tags needs to be a number between 1 and 50.";
                    }
                    else
                    {
                        Logger.LatestLogMessage = string.Empty;
                    }

                    _amountOfTags = parsedValue;
                    OnPropertyChanged(nameof(AmountOfTags));
                }
                catch
                {
                    _amountOfTags = null;
                    Logger.LatestLogMessage = $"Amount of tags needs to be a number between 1 and 50.";
                }
            }
        }

        private string[] _datasetTags;

        [ObservableProperty]
        private bool _isUiEnabled;

        public DatasetPromptGeneratorViewModel(IPromptGeneratorService promptGenerator, ITagProcessorService tagProcessor,
            ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _promptGenerator = promptGenerator;
            _tagProcessor = tagProcessor;

            InputFolderPath = string.Empty;
            OutputFolderPath = string.Empty;
            TagsToPrepend = string.Empty;
            TagsToAppend = "masterpiece, best quality, absurdres";
            GeneratedPrompt = string.Empty;
            _amountOfTags = 20;
            _amountOfGeneratedPrompts = 1000;
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
        private async Task SelectOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task GeneratePromptAsync()
        {
            try
            {
                if (_datasetTags == null || _datasetTags.Length == 0)
                {
                    IsUiEnabled = false;
                    _datasetTags = await Task.Run(() => _tagProcessor.GetTagsFromDataset(InputFolderPath));
                    IsUiEnabled = true;
                }

                if (_amountOfTags == null)
                {
                    _amountOfTags = 0;
                }
                string generatedPrompt = await Task.Run(() => _promptGenerator.GeneratePromptFromDataset(_datasetTags, TagsToPrepend,
                    TagsToAppend, Math.Clamp((int)_amountOfTags, 1, 50)));
                GeneratedPrompt = _tagProcessor.ApplyRedundancyRemoval(generatedPrompt);
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
        }

        [RelayCommand]
        private async Task GeneratePromptsAsync()
        {
            IsUiEnabled = false;

            if (GenerationProgress == null)
            {
                GenerationProgress = new Progress();
            }
            if (GenerationProgress.PercentFloat != 0f)
            {
                GenerationProgress.Reset();
            }

            TaskStatus = ProcessingStatus.Running;

            try
            {
                if (_datasetTags == null || _datasetTags.Length == 0)
                {

                    _datasetTags = Task.Run(() => _tagProcessor.GetTagsFromDataset(InputFolderPath)).Result;

                }

                string outputPath = Path.Combine(OutputFolderPath, "generatedPrompts.txt");

                if (_amountOfTags == null)
                {
                    _amountOfGeneratedPrompts = 0;
                }
                if (_amountOfGeneratedPrompts == null)
                {
                    _amountOfGeneratedPrompts = 0;
                }

                await Task.Run(() => _promptGenerator.GeneratePromptsAndSaveToFile(outputPath, _datasetTags, TagsToPrepend, TagsToAppend,
                    Math.Clamp((int)_amountOfTags, 1, 50),
                    Math.Clamp((int)_amountOfGeneratedPrompts, 10, ushort.MaxValue),
                    GenerationProgress)); ;
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

        [RelayCommand]
        private async Task CopyGeneratedPromptToClipboard()
        {
            await CopyToClipboard(GeneratedPrompt);
        }
    }
}
