﻿using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Services;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GeminiCaptionViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulator;
        private readonly IGeminiService _gemini;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private string _geminiApi;
        [ObservableProperty]
        private bool _freeApi;
        [ObservableProperty]
        private string _geminiPrompt;
        [ObservableProperty]
        private string _geminiSystemInstruction;

        [ObservableProperty]
        private Progress _captionProcessingProgress;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime
        {
            get => _timer.Elapsed;
        }

        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        public GeminiCaptionViewModel(IFileManipulatorService fileManipulator, IGeminiService gemini, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;
            _gemini = gemini;

            InputFolderPath = _configs.Configurations.GeminiCaptionConfigs.InputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.GeminiCaptionConfigs.OutputFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);

            FreeApi = true;
            GeminiPrompt = GeminiService.BASE_PROMPT;
            GeminiSystemInstruction = GeminiService.CreateBaseSystemInstruction();

            (_gemini as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                CaptionProcessingProgress = ResetProgress(CaptionProcessingProgress);
                CaptionProcessingProgress.TotalFiles = args;
            };
            (_gemini as INotifyProgress).ProgressUpdated += (sender, args) => CaptionProcessingProgress.UpdateProgress();

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
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
        private async Task CaptionWithGeminiAsync()
        {
            IsUiEnabled = false;

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            TaskStatus = ProcessingStatus.Running;

            try
            {
                _gemini.ApiKey = GeminiApi;
                _gemini.SystemInstructions = GeminiSystemInstruction;

                await _gemini.CaptionImagesAsync(InputFolderPath, OutputFolderPath, GeminiPrompt);

                timer.Stop();
            }
            catch (HttpRequestException exception)
            {
                if (exception.Message.Contains("unregistered callers"))
                {
                    Logger.SetLatestLogMessage("A valid API key is required to use Gemini!", LogMessageColor.Error);
                }
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;
            }

            // Stop elapsed timer
            _timer.Stop();
        }
    }
}
