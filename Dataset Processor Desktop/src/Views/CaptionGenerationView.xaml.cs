using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class CaptionGenerationView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IAutoCaptionService _autoCaptionService;

    private CaptionGenerationViewModel _viewModel;

    public CaptionGenerationView(IFileManipulatorService fileManipulatorService, IAutoCaptionService autoCaptionService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _autoCaptionService = autoCaptionService;

        _viewModel = new CaptionGenerationViewModel(_fileManipulatorService, _autoCaptionService);

        BindingContext = _viewModel;
    }
}