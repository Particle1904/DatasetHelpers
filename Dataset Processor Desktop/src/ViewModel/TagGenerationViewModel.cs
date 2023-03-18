using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagGenerationViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IAutoTaggerService _autoTaggerService;

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

        private Progress _predictionProgress;
        public Progress PredictionProgress
        {
            get => _predictionProgress;
            set
            {
                _predictionProgress = value;
                OnPropertyChanged(nameof(PredictionProgress));
            }
        }

        private double _threshold;
        public double Threshold
        {
            get => _threshold;
            set
            {
                _threshold = Math.Round(value, 2);
                OnPropertyChanged(nameof(Threshold));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }
        public RelayCommand MakePredictionsCommand { get; private set; }

        public TagGenerationViewModel(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, IAutoTaggerService autoTaggerService)
        {
            _folderPickerService = folderPickerService;
            _fileManipulatorService = fileManipulatorService;
            _autoTaggerService = autoTaggerService;

            _inputFolderPath = Path.Combine(AppContext.BaseDirectory, "resized-images-output");
            _outputFolderPath = Path.Combine(AppContext.BaseDirectory, "combined-images-output");
            _fileManipulatorService.CreateFolderIfNotExist(_inputFolderPath);
            _fileManipulatorService.CreateFolderIfNotExist(_outputFolderPath);

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            MakePredictionsCommand = new RelayCommand(async () => await MakePredictionsAsync());

            TaskStatus = ProcessingStatus.Idle;
            Threshold = 0.35d;
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

        public async Task MakePredictionsAsync()
        {
            if (_predictionProgress == null)
            {
                _predictionProgress = new Progress();
            }
            if (_predictionProgress.PercentFloat >= 1.0f)
            {
                _predictionProgress.Reset();
            }

            TaskStatus = ProcessingStatus.Running;
            _autoTaggerService.Threshold = (float)Threshold;
            await _autoTaggerService.GenerateTags(InputFolderPath, OutputFolderPath, PredictionProgress);
            TaskStatus = ProcessingStatus.Finished;
        }
    }
}
