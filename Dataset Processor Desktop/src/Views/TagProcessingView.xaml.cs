using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagProcessingView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly ITagProcessorService _tagProcessorService;
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly ILoggerService _loggerService;

    private TagProcessingViewModel _viewModel;

    public TagProcessingView(IFolderPickerService folderPickerService, ITagProcessorService tagProcessorService, IFileManipulatorService fileManipulatorService, ILoggerService loggerService)
    {
        InitializeComponent();

        _folderPickerService = folderPickerService;
        _tagProcessorService = tagProcessorService;
        _fileManipulatorService = fileManipulatorService;
        _loggerService = loggerService;

        _viewModel = new TagProcessingViewModel(_folderPickerService, _tagProcessorService, _fileManipulatorService, _loggerService);

        BindingContext = _viewModel;
    }
}