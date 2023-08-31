// Ignore Spelling: Metadata

using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class MetadataViewModel : BaseViewModel
    {
        private readonly IImageProcessorService _imageProcessorService;
        private readonly IAutoTaggerService _autoTaggerService;

        private ImageSource _selectedImage;
        public ImageSource SelectedImage
        {
            get => _selectedImage;
            set
            {
                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
            }
        }

        private string _positivePrompt;
        public string PositivePrompt
        {
            get => _positivePrompt;
            set
            {
                _positivePrompt = value;
                OnPropertyChanged(nameof(PositivePrompt));
            }
        }

        private string _negativePrompt;
        public string NegativePrompt
        {
            get => _negativePrompt;
            set
            {
                _negativePrompt = value;
                OnPropertyChanged(nameof(NegativePrompt));
            }
        }

        private string _parameters;
        public string Parameters
        {
            get => _parameters;
            set
            {
                _parameters = value;
                OnPropertyChanged(nameof(Parameters));
            }
        }

        private string _predictedTags;
        public string PredictedTags
        {
            get => _predictedTags;
            set
            {
                _predictedTags = value;
                OnPropertyChanged(nameof(PredictedTags));
            }
        }

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

        public RelayCommand CopyPositivePromptCommand { get; private set; }
        public RelayCommand CopyNegativePromptCommand { get; private set; }
        public RelayCommand CopySeedParameterCommand { get; private set; }
        public RelayCommand CopyPredictedPromptCommand { get; private set; }

        public bool IsGenerating { get; private set; } = false;

        public MetadataViewModel(IImageProcessorService imageProcessorService, IAutoTaggerService autoTaggerService)
        {
            _imageProcessorService = imageProcessorService;
            _autoTaggerService = autoTaggerService;

            Threshold = _configsService.Configurations.TaggerThreshold;

            CopyPositivePromptCommand = new RelayCommand(async () => await CopyToClipboard(PositivePrompt));
            CopyNegativePromptCommand = new RelayCommand(async () => await CopyToClipboard(NegativePrompt));
            CopySeedParameterCommand = new RelayCommand(async () => await CopyToClipboard(GetSeedFromParameters(Parameters)));
            CopyPredictedPromptCommand = new RelayCommand(async () => await CopyToClipboard(PredictedTags));

            SelectedImage = ImageSource.FromFile("drag_and_drop.png");
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
                SelectedImage = ImageSource.FromStream(() => imageStream);

                List<string> metadata = await _imageProcessorService.ReadImageMetadataAsync(metadataStream);
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

                _loggerService.LatestLogMessage = $"An error occurred while trying to read the image metadata (if file is a PNG then generation metadata is probably empty).{Environment.NewLine}Only PNG metadata supported.";
            }

            try
            {
                PredictedTags = "Generating tags...";
                _autoTaggerService.Threshold = (float)Threshold;
                PredictedTags = await _autoTaggerService.InterrogateImageFromStream(interrogationStream);
            }
            catch (Exception exception)
            {
                PredictedTags = string.Empty;
                _loggerService.LatestLogMessage = $"An error occurred while trying to generate tags for the image! Error log will be saved inside the logs folder.";
                await _loggerService.SaveExceptionStackTrace(exception);
            }
            IsGenerating = false;
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