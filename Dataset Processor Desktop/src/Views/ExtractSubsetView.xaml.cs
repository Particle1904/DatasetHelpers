using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class ExtractSubsetView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;

    private ExtractSubsetViewModel _viewModel;

    public ExtractSubsetView(IFileManipulatorService fileManipulatorService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;

        _viewModel = new ExtractSubsetViewModel(_fileManipulatorService);
        BindingContext = _viewModel;
    }
}