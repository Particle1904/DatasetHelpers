using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class DatasetSortView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IFileManipulatorService _fileManipulatorService;

    private DatasetSortViewModel _viewModel;

    public DatasetSortView(IFolderPickerService folderPickerService, IFileManipulatorService fileManipulatorService)
    {
        InitializeComponent();

        _folderPickerService = folderPickerService;
        _fileManipulatorService = fileManipulatorService;

        _viewModel = new DatasetSortViewModel(_folderPickerService, _fileManipulatorService);
        BindingContext = _viewModel;
    }
}