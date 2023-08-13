using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class CaptionProcessingView : ContentView
{
    private readonly ITagProcessorService _tagProcessorService;
    private readonly IFileManipulatorService _fileManipulatorService;

    private CaptionProcessingViewModel _viewModel;

    public CaptionProcessingView(ITagProcessorService tagProcessorService, IFileManipulatorService fileManipulatorService)
    {
        InitializeComponent();

        _tagProcessorService = tagProcessorService;
        _fileManipulatorService = fileManipulatorService;

        _viewModel = new CaptionProcessingViewModel(_tagProcessorService, _fileManipulatorService);

        BindingContext = _viewModel;
    }
}