using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class DatasetSortView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;

    private SortViewModel _viewModel;

    public DatasetSortView(IFileManipulatorService fileManipulatorService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;

        _viewModel = new SortViewModel(_fileManipulatorService);
        BindingContext = _viewModel;
    }
}