using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class ContentAwareCroppingView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IContentAwareCropService _contentAwareCropService;

    private ContentAwareCropViewModel _viewModel;

    public ContentAwareCroppingView(IFileManipulatorService fileManipulatorService, IContentAwareCropService contentAwareCropService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _contentAwareCropService = contentAwareCropService;

        _viewModel = new ContentAwareCropViewModel(_fileManipulatorService, _contentAwareCropService);

        BindingContext = _viewModel;
    }
}