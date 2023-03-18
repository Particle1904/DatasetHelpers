using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class ResizeImagesViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        private readonly IImageProcessorService _imageProcessorService;
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

        private Progress _resizeProgress;
        public Progress ResizeProgress
        {
            get => _resizeProgress;
            set
            {
                _resizeProgress = value;
                OnPropertyChanged(nameof(ResizeProgress));
            }
        }

        private SupportedDimensions _dimension;
        public SupportedDimensions Dimension
        {
            get => _dimension;
            set
            {
                _dimension = value;
                OnPropertyChanged(nameof(Dimension));
            }
        }

        public RelayCommand SelectInputFolderCommand { get; private set; }
        public RelayCommand SelectOutputFolderCommand { get; private set; }

        public RelayCommand ResizeImagesCommand { get; private set; }

        public ResizeImagesViewModel(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService)
        {
            _folderPickerService = folderPickerService;
            _imageProcessorService = imageProcessorService;
            _fileManipulatorService = fileManipulatorService;

            _inputFolderPath = Path.Combine(AppContext.BaseDirectory, "selected-images-output");
            _outputFolderPath = Path.Combine(AppContext.BaseDirectory, "resized-images-output");
            _fileManipulatorService.CreateFolderIfNotExist(_inputFolderPath);
            _fileManipulatorService.CreateFolderIfNotExist(_outputFolderPath);

            Dimension = SupportedDimensions.Resolution512x512;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            ResizeImagesCommand = new RelayCommand(async () => await ResizeImagesAsync());

            TaskStatus = ProcessingStatus.Idle;
        }

        public async Task SelectInputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        public async Task SelectOutputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        public async Task ResizeImagesAsync()
        {
            if (ResizeProgress == null)
            {
                ResizeProgress = new Progress();
            }
            if (ResizeProgress.PercentFloat >= 1.0f)
            {
                ResizeProgress.Reset();
            }

            TaskStatus = ProcessingStatus.Running;
            await Task.Run(() => _imageProcessorService.ResizeImagesAsync(InputFolderPath, OutputFolderPath, ResizeProgress, Dimension));
            TaskStatus = ProcessingStatus.Finished;
        }
    }
}
