using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagGenerationView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IAutoTaggerService _autoTaggerService;

    private TagGenerationViewModel _viewModel;

    public TagGenerationView(IFileManipulatorService fileManipulatorService, IAutoTaggerService autoTaggerService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _autoTaggerService = autoTaggerService;

        _viewModel = new TagGenerationViewModel(_fileManipulatorService, _autoTaggerService);

        BindingContext = _viewModel;
    }
}