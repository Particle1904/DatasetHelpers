using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagEditorView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IImageProcessorService _imageProcessorService;

    private TagEditorViewModel _viewModel;

    public TagEditorView(IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _imageProcessorService = imageProcessorService;

        _viewModel = new TagEditorViewModel(_fileManipulatorService, _imageProcessorService);
        BindingContext = _viewModel;
    }
}