using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class DatasetSortView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly ILoggerService _loggerService;

    private DatasetSortViewModel _viewModel;

    public DatasetSortView(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService, ILoggerService loggerService)
    {
        InitializeComponent();

        _folderPickerService = folderPickerService;
        _fileManipulatorService = fileManipulatorService;
        _loggerService = loggerService;

        _viewModel = new DatasetSortViewModel(_folderPickerService, _fileManipulatorService, _loggerService);
        BindingContext = _viewModel;
    }
}