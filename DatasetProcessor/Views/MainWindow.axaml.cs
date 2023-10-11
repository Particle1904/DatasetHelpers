using Avalonia;
using Avalonia.Controls;

using DatasetProcessor.ViewModels;

using SmartData.Lib.Interfaces;

namespace DatasetProcessor.Views;

public partial class MainWindow : Window
{
    private readonly IFileManipulatorService _fileManipulator;
    private readonly IImageProcessorService _imageProcessor;
    private readonly IAutoTaggerService _autoTagger;
    private readonly ITagProcessorService _tagProcessor;
    private readonly IContentAwareCropService _contentAwareCrop;
    private readonly IInputHooksService _inputHooks;
    private readonly IPromptGeneratorService _promptGenerator;
    private readonly ILoggerService _logger;
    private readonly IConfigsService _configs;

    public MainViewModel ViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
    }
}
