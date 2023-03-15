using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class ResizeImagesView : ContentView
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly IImageProcessorService _imageProcessorService;

    private ResizeImagesViewModel _viewModel;
    public ResizeImagesView(IFolderPickerService folderPickerService, IImageProcessorService imageProcessorService)
    {
        _folderPickerService = folderPickerService;
        _imageProcessorService = imageProcessorService;

        InitializeComponent();
        _viewModel = new ResizeImagesViewModel(_folderPickerService, _imageProcessorService);
        BindingContext = _viewModel;
    }
}