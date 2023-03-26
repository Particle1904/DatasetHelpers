using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class TagGenerationViewModel : BaseViewModel
    {
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

        public RelayCommand OpenInputFolderCommand { get; private set; }
        public RelayCommand OpenOutputFolderCommand { get; private set; }

        public TagGenerationViewModel(IFileManipulatorService fileManipulatorService, IAutoTaggerService autoTaggerService)
        {
            _fileManipulatorService = fileManipulatorService;
            _autoTaggerService = autoTaggerService;

            InputFolderPath = _configsService.Configurations.ResizedFolder;
            _fileManipulatorService.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configsService.Configurations.CombinedOutputFolder;
            _fileManipulatorService.CreateFolderIfNotExist(OutputFolderPath);

            Threshold = _configsService.Configurations.TaggerThreshold;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            MakePredictionsCommand = new RelayCommand(async () => await MakePredictionsAsync());

            OpenInputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(InputFolderPath));
            OpenOutputFolderCommand = new RelayCommand(async () => await OpenFolderAsync(OutputFolderPath));

            TaskStatus = ProcessingStatus.Idle;
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
            if (PredictionProgress == null)
            {
                PredictionProgress = new Progress();
            }
            if (PredictionProgress.PercentFloat != 0f)
            {
                PredictionProgress.Reset();
            }

            TaskStatus = ProcessingStatus.Running;
            _autoTaggerService.Threshold = (float)Threshold;
            try
            {
                await _autoTaggerService.GenerateTags(InputFolderPath, OutputFolderPath, PredictionProgress);
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong! {exception.StackTrace}";
            }
            finally
            {
                TaskStatus = ProcessingStatus.Finished;
            }
        }
    }
}
