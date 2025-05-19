using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services;
using SmartData.Lib.Services.MachineLearning;

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DatasetProcessor.Views;

public partial class MainView : UserControl
{
    private readonly IFileManagerService _fileManager;
    private readonly IImageProcessorService _imageProcessor;
    private readonly IAutoTaggerService _autoTagger;
    private readonly ITagProcessorService _tagProcessor;
    private readonly IContentAwareCropService _contentAwareCrop;
    private readonly IInputHooksService _inputHooks;
    private readonly IPromptGeneratorService _promptGenerator;
    private readonly ILoggerService _logger;
    private readonly IConfigsService _configs;

    private IClipboard _clipboard;
    private IStorageProvider _storageProvider;

    public MainView(IFileManagerService fileManager,
                    IImageProcessorService imageProcessor,
                    IAutoTaggerService autoTagger,
                    ITagProcessorService tagProcessor,
                    IContentAwareCropService contentAwareCrop,
                    IInputHooksService inputHooks,
                    IPromptGeneratorService promptGenerator,
                    ILoggerService logger,
                    IConfigsService configs)
    {
        _fileManager = fileManager;
        _imageProcessor = imageProcessor;
        _autoTagger = autoTagger;
        _tagProcessor = tagProcessor;
        _contentAwareCrop = contentAwareCrop;
        _inputHooks = inputHooks;
        _promptGenerator = promptGenerator;
        _logger = logger;
        _configs = configs;

        InitializeComponent();
    }

    /// <summary>
    /// --> THIS CONSTRUCTOR IS ONLY HERE TO ENABLE IDE DESIGNER TO WORK <--
    /// </summary>
    public MainView()
    {
        if (Design.IsDesignMode)
        {
            string modelsPath = Path.Combine(AppContext.BaseDirectory, "models");
            string WDOnnxFilename = "wdModel.onnx";
            string csvFilename = "wdTags.csv";

            string YoloV4OnnxFilename = "yolov4.onnx";

            _fileManager = new FileManagerService();
            _imageProcessor = new ImageProcessorService();
            _tagProcessor = new TagProcessorService();
            _inputHooks = new InputHooksService();
            _logger = new LoggerService();
            _configs = new ConfigurationsService();
            _contentAwareCrop = new ContentAwareCropService(_imageProcessor,
                Path.Combine(modelsPath, YoloV4OnnxFilename));
            _promptGenerator = new PromptGeneratorService(_tagProcessor, _fileManager);
            _autoTagger = new WDAutoTaggerService(_imageProcessor, _tagProcessor,
                Path.Combine(modelsPath, WDOnnxFilename), Path.Combine(modelsPath, csvFilename));
        }

        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        TopLevel topLevel = TopLevel.GetTopLevel(this);
        _clipboard = topLevel.Clipboard;
        _storageProvider = topLevel.StorageProvider;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Metadata_ViewerButton.IsEnabled = false;
            Metadata_ViewerButton.IsVisible = false;
        }

    }

    private void OnNavigationButton(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var primaryColor = (ImmutableSolidColorBrush)Application.Current.Resources["Primary"];
        foreach (Control item in LeftMenuStackPanel.Children)
        {
            if (item.GetType() == typeof(Button))
            {
                (item as Button).Background = primaryColor;
            }
        }

        (sender as Button).Background = (ImmutableSolidColorBrush)Application.Current.Resources["SecondaryDark"];
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        FlyoutButton.Width = MainContentScrowViewer.Bounds.Width;
        FlyoutPanel.Width = MainContentScrowViewer.Bounds.Width - 20;
    }
}