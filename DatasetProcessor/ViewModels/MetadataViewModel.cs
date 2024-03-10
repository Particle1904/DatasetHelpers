using Avalonia.Media.Imaging;
using Avalonia.Platform;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class MetadataViewModel : BaseViewModel
    {
        private const string _dragAndDropPath = @"avares://DatasetProcessor/Assets/Images/drag_and_drop.png";
        private readonly IImageProcessorService _imageProcessor;
        private readonly IAutoTaggerService _autoTagger;

        [ObservableProperty]
        private Bitmap _selectedImage;
        [ObservableProperty]
        private string _positivePrompt;
        [ObservableProperty]
        private string _negativePrompt;
        [ObservableProperty]
        private string _parameters;
        [ObservableProperty]
        private string _predictedTags;

        private double _threshold;
        public double Threshold
        {
            get => _threshold;
            set
            {
                if (Math.Round(value, 2) != _threshold)
                {
                    _threshold = Math.Round(value, 2);
                    OnPropertyChanged(nameof(Threshold));
                }
            }
        }

        public bool IsGenerating { get; private set; } = false;

        public MetadataViewModel(IImageProcessorService imageProcessor, IAutoTaggerService autoTagger, ILoggerService logger,
            IConfigsService configs) : base(logger, configs)
        {
            _imageProcessor = imageProcessor;
            _autoTagger = autoTagger;

            Threshold = configs.Configurations.TaggerThreshold;

            SelectedImage = new Bitmap(AssetLoader.Open(new Uri(_dragAndDropPath)));
        }

        public async Task OpenFileAsync(Stream stream)
        {
            IsGenerating = true;
            byte[] streamBytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                streamBytes = memoryStream.ToArray();
            }
            MemoryStream imageStream = new MemoryStream(streamBytes);
            MemoryStream metadataStream = new MemoryStream(streamBytes);
            MemoryStream interrogationStream = new MemoryStream(streamBytes);

            try
            {
                SelectedImage = new Bitmap(imageStream);

                List<string> metadata = await _imageProcessor.ReadImageMetadataAsync(metadataStream);
                if (metadata != null)
                {
                    PositivePrompt = metadata[0];
                    NegativePrompt = metadata[1];
                    Parameters = metadata[2];
                }
            }
            catch (Exception)
            {
                PositivePrompt = string.Empty;
                NegativePrompt = string.Empty;
                Parameters = string.Empty;

                Logger.SetLatestLogMessage($"An error occurred while trying to read the image metadata (if file is a PNG then generation metadata is probably empty).{Environment.NewLine}Only PNG metadata supported.",
                    LogMessageColor.Warning);
            }

            try
            {
                PredictedTags = "Generating tags...";
                _autoTagger.Threshold = (float)Threshold;
                PredictedTags = await _autoTagger.InterrogateImageFromStream(interrogationStream);
            }
            catch (Exception exception)
            {
                PredictedTags = string.Empty;
                Logger.SetLatestLogMessage($"An error occurred while trying to generate tags for the image! Error log will be saved inside the logs folder.",
                    LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            IsGenerating = false;
        }

        [RelayCommand]
        private async Task CopyPositivePromptToClipboard()
        {
            await CopyToClipboard(PositivePrompt);
        }

        [RelayCommand]
        private async Task CopyNegativePromptToClipboard()
        {
            await CopyToClipboard(NegativePrompt);
        }

        [RelayCommand]
        private async Task CopySeedFromParametersToClipboard()
        {
            await CopyToClipboard(GetSeedFromParameters(Parameters));
        }

        [RelayCommand]
        private async Task CopyPredictedTagsToClipboard()
        {
            await CopyToClipboard(PredictedTags);
        }

        private string GetSeedFromParameters(string parameters)
        {
            string[] parametersSplit = parameters.Split(",");

            string seed = null;
            foreach (string parameter in parametersSplit)
            {
                if (parameter.Contains("Seed"))
                {
                    string[] parameterSplit = parameter.Split(":");
                    seed = parameterSplit.Last().Trim();
                }
            }

            return seed;
        }
    }
}
