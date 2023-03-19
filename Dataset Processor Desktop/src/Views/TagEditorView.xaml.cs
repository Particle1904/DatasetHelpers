using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagEditorView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IImageProcessorService _imageProcessorService;
    private readonly ILoggerService _loggerService;

    private TagEditorViewModel _viewModel;

    public TagEditorView(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService, ILoggerService loggerService)
    {
        InitializeComponent();

        _folderPickerService = folderPickerService;
        _fileManipulatorService = fileManipulatorService;
        _imageProcessorService = imageProcessorService;
        _loggerService = loggerService;

        _viewModel = new TagEditorViewModel(_folderPickerService, _fileManipulatorService, _loggerService);
        BindingContext = _viewModel;
    }
}