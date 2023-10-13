using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

using DatasetProcessor.ViewModels;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Services;

using System;
using System.IO;

namespace DatasetProcessor.Views;

public partial class MainView : UserControl
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

    private IClipboard _clipboard;
    private IStorageProvider _storageProvider;

    private MainViewModel? _viewModel;

    public MainView(IFileManipulatorService fileManipulator,
                    IImageProcessorService imageProcessor,
                    IAutoTaggerService autoTagger,
                    ITagProcessorService tagProcessor,
                    IContentAwareCropService contentAwareCrop,
                    IInputHooksService inputHooks,
                    IPromptGeneratorService promptGenerator,
                    ILoggerService logger,
                    IConfigsService configs)
    {
        _fileManipulator = fileManipulator;
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

            _fileManipulator = new FileManipulatorService();
            _imageProcessor = new ImageProcessorService();
            _tagProcessor = new TagProcessorService();
            _inputHooks = new InputHooksService();
            _logger = new LoggerService();
            _configs = new ConfigsService();
            _contentAwareCrop = new ContentAwareCropService(_imageProcessor,
                Path.Combine(modelsPath, YoloV4OnnxFilename));
            _promptGenerator = new PromptGeneratorService(_tagProcessor, _fileManipulator);
            _autoTagger = new AutoTaggerService(_imageProcessor, _tagProcessor,
                Path.Combine(modelsPath, WDOnnxFilename), Path.Combine(modelsPath, csvFilename));
        }

        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        _clipboard = topLevel.Clipboard;
        _storageProvider = topLevel.StorageProvider;
    }
}