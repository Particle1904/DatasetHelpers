using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

namespace Dataset_Processor_Desktop.src.Views;

public partial class DatasetSortView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;

    private DatasetSortViewModel _viewModel;

    public DatasetSortView(IFolderPickerService folderPickerService)
    {
        _folderPickerService = folderPickerService;
        InitializeComponent();
        _viewModel = new DatasetSortViewModel(_folderPickerService);
        BindingContext = _viewModel;
    }
}