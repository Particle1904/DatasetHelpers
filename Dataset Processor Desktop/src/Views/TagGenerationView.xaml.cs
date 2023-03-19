using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagGenerationView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IAutoTaggerService _autoTaggerService;
    private readonly ILoggerService _loggerService;

    private TagGenerationViewModel _viewModel;

    public TagGenerationView(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, IAutoTaggerService autoTaggerService, ILoggerService loggerService)
    {
        InitializeComponent();

        _folderPickerService = folderPickerService;
        _fileManipulatorService = fileManipulatorService;
        _autoTaggerService = autoTaggerService;
        _loggerService = loggerService;

        _viewModel = new TagGenerationViewModel(_folderPickerService, _fileManipulatorService, _autoTaggerService, _loggerService);

        BindingContext = _viewModel;
    }
}