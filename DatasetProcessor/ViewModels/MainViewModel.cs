using Avalonia.Controls;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;
using DatasetProcessor.Views;

using SmartData.Lib.Interfaces;

using System.Collections.Generic;

namespace DatasetProcessor.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    protected readonly IFileManipulatorService _fileManipulator;
    protected readonly IImageProcessorService _imageProcessor;
    protected readonly IAutoTaggerService _autoTagger;
    protected readonly ITagProcessorService _tagProcessor;
    protected readonly IContentAwareCropService _contentAwareCrop;
    protected readonly IInputHooksService _inputHooks;
    protected readonly IPromptGeneratorService _promptGenerator;

    [ObservableProperty]
    public UserControl _dynamicView;
    private Dictionary<AppPages, UserControl> _views;

    public string LatestLogMessage
    {
        get => _logger.LatestLogMessage;
    }

    public MainViewModel(IFileManipulatorService fileManipulator,
                         IImageProcessorService imageProcessor,
                         IAutoTaggerService autoTagger,
                         ITagProcessorService tagProcessor,
                         IContentAwareCropService contentAwareCrop,
                         IInputHooksService inputHooks,
                         IPromptGeneratorService promptGenerator,
                         ILoggerService logger,
                         IConfigsService configs) :
        base(logger, configs)
    {
        _fileManipulator = fileManipulator;
        _imageProcessor = imageProcessor;
        _autoTagger = autoTagger;
        _tagProcessor = tagProcessor;
        _contentAwareCrop = contentAwareCrop;
        _inputHooks = inputHooks;
        _promptGenerator = promptGenerator;

        _dynamicView = new WelcomeView();
        _views = new Dictionary<AppPages, UserControl>();
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        OpenFolderInExplorer(_logger.LogsFolder);
    }
}