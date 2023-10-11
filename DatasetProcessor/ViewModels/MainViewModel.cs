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
        _views = new Dictionary<AppPages, UserControl>()
        {
            { AppPages.Welcome, new WelcomeView() { DataContext = new WelcomeViewModel(logger, configs) }},
            { AppPages.SortImages, new SortImagesView() { DataContext = new SortImagesViewModel(logger, configs) }},
            { AppPages.ContentAwareCrop, new ContentAwareCropView() { DataContext =  new ContentAwareCropViewModel(logger, configs) }},
            { AppPages.ResizeImages, new ResizeImagesView() { DataContext = new ResizeImagesViewModel(logger, configs) }},
            { AppPages.TagGeneration, new GenerateTagsView() { DataContext = new GenerateTagsViewModel(logger, configs) }},
            { AppPages.ProcessCaptions, new ProcessCaptionsView() { DataContext = new ProcessCaptionsViewModel(logger, configs) }},
            { AppPages.ProcessTags, new ProcessTagsView() { DataContext = new ProcessTagsViewModel(logger, configs) }},
            { AppPages.TagEditor, new TagEditorView() { DataContext = new TagEditorViewModel(logger, configs) }},
            { AppPages.ExtractSubset, new ExtractSubsetView() { DataContext= new ExtractSubsetViewModel(logger, configs) }},
            { AppPages.PromptGenerator, new DatasetPromptGeneratorView() { DataContext = new DatasetPromptGeneratorViewModel(logger, configs) }},
            { AppPages.Metadata, new MetadataView() { DataContext = new MetadataViewModel(logger, configs) }},
            { AppPages.Settings, new SettingsView() { DataContext = new SettingsViewModel(logger, configs) }}
        };

        _dynamicView = _views[AppPages.Welcome];
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        OpenFolderInExplorer(_logger.LogsFolder);
    }

    [RelayCommand]
    public void NavigateToPage(AppPages parameter)
    {
        SetAllViewsAsInactive();
        SetViewAsActive(parameter);
        DynamicView = _views[parameter];
    }

    private void SetAllViewsAsInactive()
    {
        foreach (var item in _views)
        {
            ViewModelBase bindingContext = (ViewModelBase)item.Value.DataContext;
            if (bindingContext != null)
            {
                bindingContext.IsActive = false;
            }
        }
    }

    private void SetViewAsActive(AppPages view)
    {
        ViewModelBase bindingContext = (ViewModelBase)_views[view].DataContext;
        if (bindingContext != null)
        {
            bindingContext.IsActive = true;
        }
    }
}