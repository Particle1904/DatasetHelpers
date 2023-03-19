using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class ResizeImagesView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IImageProcessorService _imageProcessorService;
    private readonly ILoggerService _loggerService;

    private ResizeImagesViewModel _viewModel;
    public ResizeImagesView(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService, ILoggerService loggerService)
    {
        InitializeComponent();

        _folderPickerService = folderPickerService;
        _fileManipulatorService = fileManipulatorService;
        _imageProcessorService = imageProcessorService;
        _loggerService = loggerService;

        _viewModel = new ResizeImagesViewModel(_folderPickerService, _fileManipulatorService, _imageProcessorService, _loggerService);
        BindingContext = _viewModel;
        _loggerService = loggerService;
    }
}