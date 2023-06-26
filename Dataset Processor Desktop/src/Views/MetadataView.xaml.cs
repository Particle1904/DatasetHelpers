using Dataset_Processor_Desktop.src.Utilities;
using Dataset_Processor_Desktop.src.ViewModel;

using Microsoft.Maui.Platform;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class MetadataView : ContentView
{
    private readonly IImageProcessorService _imageProcessorService;
    private readonly IAutoTaggerService _autoTaggerService;

    private MetadataViewModel _viewModel;
    public MetadataView(IImageProcessorService imageProcessorService, IAutoTaggerService autoTaggerService)
    {
        InitializeComponent();

        _imageProcessorService = imageProcessorService;
        _autoTaggerService = autoTaggerService;

        _viewModel = new MetadataViewModel(_imageProcessorService, _autoTaggerService);
        BindingContext = _viewModel;

        Loaded += (sender, args) =>
        {
            if (Handler?.MauiContext != null)
            {
                var uiElement = this.ToPlatform(Handler.MauiContext);
                DragDropExtensions.RegisterDragDrop(uiElement, async stream =>
                {
                    await _viewModel.OpenFileAsync(stream, CancellationToken.None);
                });
            }
        };

        Unloaded += (sender, args) =>
        {
            if (Handler?.MauiContext != null)
            {
                var uiElement = this.ToPlatform(Handler.MauiContext);
                DragDropExtensions.UnRegisterDragDrop(uiElement);
            }
        };
    }
}