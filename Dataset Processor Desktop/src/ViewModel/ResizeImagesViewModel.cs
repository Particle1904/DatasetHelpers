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

        public ResizeImagesViewModel(IFolderPickerService folderPickerService, IImageProcessorService imageProcessorService)
        {
            Dimension = SupportedDimensions.Resolution512x512;

            _inputFolderPath = Path.Combine(AppContext.BaseDirectory, "selected-images-output");
            _outputFolderPath = Path.Combine(AppContext.BaseDirectory, "resized-images-output");

            _folderPickerService = folderPickerService;
            _imageProcessorService = imageProcessorService;

            SelectInputFolderCommand = new RelayCommand(async () => await SelectInputFolderAsync());
            SelectOutputFolderCommand = new RelayCommand(async () => await SelectOutputFolderAsync());
            ResizeImagesCommand = new RelayCommand(async () => await ResizeImagesAsync());

            TaskStatus = ProcessingStatus.Idle;
        }

        public async Task SelectInputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (result != null)
            {
                InputFolderPath = result;
            }
        }

        public async Task SelectOutputFolderAsync()
        {
            var result = await _folderPickerService.PickFolderAsync();
            if (result != null)
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

            TaskStatus = ProcessingStatus.Running;
            await Task.Run(() => _imageProcessorService.ResizeImagesAsync(InputFolderPath, OutputFolderPath, ResizeProgress, Dimension));
            TaskStatus = ProcessingStatus.Finished;
        }
    }
}
