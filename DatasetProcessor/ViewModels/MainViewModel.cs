using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;
using DatasetProcessor.Views;

using SmartData.Lib.Interfaces;

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DatasetProcessor.ViewModels;

/// <summary>
/// The view model that controls the main application logic and navigation.
/// </summary>
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
    private UserControl _dynamicView;

    private Dictionary<AppPages, UserControl> _views;

    [ObservableProperty]
    private string _pageName;

    public string LatestLogMessage
    {
        get => Logger.LatestLogMessage;
    }

    /// <summary>
    /// Initializes a new instance of the MainViewModel class.
    /// </summary>
    /// <param name="fileManipulator">The file manipulation service.</param>
    /// <param name="imageProcessor">The image processing service.</param>
    /// <param name="autoTagger">The auto-tagging service.</param>
    /// <param name="tagProcessor">The tag processing service.</param>
    /// <param name="contentAwareCrop">The content-aware crop service.</param>
    /// <param name="inputHooks">The input hooks service.</param>
    /// <param name="promptGenerator">The prompt generator service.</param>
    /// <param name="logger">The logger service.</param>
    /// <param name="configs">The configuration service.</param>
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

        ((INotifyPropertyChanged)_logger).PropertyChanged += OnLoggerServicePropertyChanged;

        _views = new Dictionary<AppPages, UserControl>()
        {
            { AppPages.Welcome, new WelcomeView() { DataContext = new WelcomeViewModel(logger, configs) }},
            { AppPages.Sort_Images, new SortImagesView() { DataContext = new SortImagesViewModel(fileManipulator, logger, configs) }},
            { AppPages.Content_Aware_Crop, new ContentAwareCropView() { DataContext =  new ContentAwareCropViewModel(fileManipulator, contentAwareCrop, logger, configs) }},
            { AppPages.Resize_Images, new ResizeImagesView() { DataContext = new ResizeImagesViewModel(imageProcessor, fileManipulator, logger, configs) }},
            { AppPages.Tag_Generation, new GenerateTagsView() { DataContext = new GenerateTagsViewModel(fileManipulator, autoTagger, logger, configs) }},
            { AppPages.Process_Captions, new ProcessCaptionsView() { DataContext = new ProcessCaptionsViewModel(tagProcessor, fileManipulator, logger, configs) }},
            { AppPages.Process_Tags, new ProcessTagsView() { DataContext = new ProcessTagsViewModel(tagProcessor, fileManipulator, logger, configs) }},
            { AppPages.Tag_Editor, new TagEditorView() { DataContext = new TagEditorViewModel(fileManipulator, imageProcessor, inputHooks, logger, configs) }},
            { AppPages.Extract_Subset, new ExtractSubsetView() { DataContext= new ExtractSubsetViewModel(fileManipulator, logger, configs) }},
            { AppPages.Prompt_Generator, new DatasetPromptGeneratorView() { DataContext = new DatasetPromptGeneratorViewModel(promptGenerator, tagProcessor, logger, configs) }},
            { AppPages.Settings, new SettingsView() { DataContext = new SettingsViewModel(logger, configs) }}
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _views.Add(AppPages.Metadata_Viewer, new MetadataView() { DataContext = new MetadataViewModel(imageProcessor, autoTagger, logger, configs) });
        }

        _dynamicView = _views[AppPages.Welcome];
        SetPageName(AppPages.Welcome);
    }

    /// <summary>
    /// Navigates to a specific page and updates the dynamic view accordingly.
    /// </summary>
    /// <param name="parameter">The target page to navigate to.</param>
    [RelayCommand]
    public void NavigateToPage(AppPages parameter)
    {
        SetAllViewsAsInactive();
        SetViewAsActive(parameter);
        SetPageName(parameter);
        DynamicView = _views[parameter];
    }

    /// <summary>
    /// Navigates to the tag editor view and updates the current selected tags.
    /// </summary>
    [RelayCommand]
    public void NavigateToTagEditorView()
    {
        TagEditorViewModel tagEditorViewModel = (TagEditorViewModel)_views[AppPages.Tag_Editor].DataContext;
        tagEditorViewModel.UpdateCurrentSelectedTags();
        NavigateToPage(AppPages.Tag_Editor);
    }

    /// <summary>
    /// Initializes the clipboard and storage provider for the application and its views.
    /// </summary>
    /// <param name="clipboard">An implementation of the IClipboard interface for managing clipboard operations.</param>
    /// <param name="storageProvider">An implementation of the IStorageProvider interface for handling storage-related operations.</param>
    /// <remarks>
    /// This method sets the clipboard and storage provider for the application and ensures that each view's associated view model
    /// is also initialized with the provided clipboard and storage provider.
    /// </remarks>
    /// <param name="clipboard">The clipboard provider to use for the application.</param>
    /// <param name="storageProvider">The storage provider to use for the application.</param>
    public void InitializeClipboardAndStorageProvider(IClipboard clipboard, IStorageProvider storageProvider)
    {
        Initialize(clipboard, storageProvider);
        foreach (KeyValuePair<AppPages, UserControl> view in _views)
        {
            (view.Value.DataContext as ViewModelBase).Initialize(clipboard, storageProvider);
        }
    }

    /// <summary>
    /// Sets all views as inactive by updating their IsActive properties to false.
    /// </summary>
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

    /// <summary>
    /// Sets a specific view as active by updating its IsActive property to true.
    /// </summary>
    /// <param name="view">The target view to set as active.</param>
    private void SetViewAsActive(AppPages view)
    {
        ViewModelBase bindingContext = (ViewModelBase)_views[view].DataContext;
        if (bindingContext != null)
        {
            bindingContext.IsActive = true;
        }
    }

    /// <summary>
    /// Sets the page name based on the provided view's enum name.
    /// </summary>
    /// <param name="view">The target view to set the page name for.</param>
    private void SetPageName(AppPages view)
    {
        string enumName = view.ToString();
        PageName = enumName.Replace('_', ' ');
    }

    /// <summary>
    /// Handles property changes in the logger service and updates the LatestLogMessage property.
    /// </summary>
    /// <param name="sender">The sender of the property change event.</param>
    /// <param name="args">The property change event arguments.</param>
    private void OnLoggerServicePropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ILoggerService.LatestLogMessage))
        {
            OnPropertyChanged(nameof(LatestLogMessage));
        }
    }
}