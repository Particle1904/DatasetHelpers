using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;
using DatasetProcessor.Views;

using Interfaces;

using Microsoft.Extensions.DependencyInjection;

using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DatasetProcessor.ViewModels;

/// <summary>
/// The view model that controls the main application logic and navigation.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInputHooksService _inputHooks;
    private readonly IModelManagerService _modelManager;

    [ObservableProperty]
    private UserControl _dynamicView;

    private readonly Dictionary<AppPages, UserControl> _viewCache;
    private readonly Dictionary<AppPages, (Type, Type)> _pageRegistry;

    [ObservableProperty]
    private string _pageName;

    public string LatestLogMessage
    {
        get => Logger.LatestLogMessage;
    }

    [ObservableProperty]
    private SolidColorBrush _logMessageColor;

    /// <summary>
    /// Initializes a new instance of the MainViewModel class.
    /// </summary>
    /// <param name="fileManager">The file manager service.</param>
    /// <param name="modelManager">The model manager service.</param>
    /// <param name="imageProcessor">The image processing service.</param>
    /// <param name="wDAutoTagger">The WD 1.4 auto-tagging service.</param>
    /// <param name="wDv3AutoTagger">The WD 3 auto-tagging service.</param>
    /// <param name="wDv3LargeAutoTagger">The WD 3 Large auto-tagging service.</param>
    /// <param name="joyTagAutoTagger">The JoyTag auto-tagging service.</param>
    /// <param name="e621AutoTagger">The E621 auto-tagging service.</param>
    /// <param name="tagProcessor">The tag processing service.</param>
    /// <param name="contentAwareCrop">The content-aware crop service.</param>
    /// <param name="inputHooks">The input hooks service.</param>
    /// <param name="promptGenerator">The prompt generator service.</param>
    /// <param name="clipTokenizer">The clip tokenizer.</param>
    /// <param name="upscaler">The upscaler service.</param>
    /// <param name="inpaint">The inpaint service.</param>
    /// <param name="gemini">The gemini service.</param>
    /// <param name="logger">The logger service.</param>
    /// <param name="configs">The configuration service.</param>
    public MainViewModel(IServiceProvider serviceProvider, ILoggerService logger, IConfigsService configs) : base(logger, configs)
    {
        _serviceProvider = serviceProvider;
        _inputHooks = serviceProvider.GetRequiredService<IInputHooksService>();
        _modelManager = serviceProvider.GetRequiredService<IModelManagerService>();

        ((INotifyPropertyChanged)_logger).PropertyChanged += OnLoggerServicePropertyChanged;

        _viewCache = new Dictionary<AppPages, UserControl>();
        _pageRegistry = new Dictionary<AppPages, (Type, Type)>
        {
            { AppPages.Welcome, (typeof(WelcomeView), typeof(WelcomeViewModel)) },
            { AppPages.Gallery, (typeof(GalleryView), typeof(GalleryViewModel)) },
            { AppPages.Sort_Images, (typeof(SortImagesView), typeof(SortImagesViewModel)) },
            { AppPages.Text_Remover, (typeof(TextRemoverView), typeof(TextRemoverViewModel)) },
            { AppPages.Content_Aware_Crop, (typeof(ContentAwareCropView), typeof(ContentAwareCropViewModel)) },
            { AppPages.Manual_Crop, (typeof(ManualCropView), typeof(ManualCropViewModel)) },
            { AppPages.Inpaint_Images, (typeof(InpaintView), typeof(InpaintViewModel)) },
            { AppPages.Resize_Images, (typeof(ResizeImagesView), typeof(ResizeImagesViewModel)) },
            { AppPages.Upscale_Images, (typeof(UpscaleView), typeof(UpscaleViewModel)) },
            { AppPages.Tag_Generation, (typeof(GenerateTagsView), typeof(GenerateTagsViewModel)) },
            { AppPages.Gemini_Caption, (typeof(GeminiCaptionView), typeof(GeminiCaptionViewModel)) },
            { AppPages.Florence_2_Caption, (typeof(FlorenceCaptionView), typeof(FlorenceCaptionViewModel)) },
            { AppPages.Process_Captions, (typeof(ProcessCaptionsView), typeof(ProcessCaptionsViewModel)) },
            { AppPages.Process_Tags, (typeof(ProcessTagsView), typeof(ProcessTagsViewModel)) },
            { AppPages.Tag_Editor, (typeof(TagEditorView), typeof(TagEditorViewModel)) },
            { AppPages.Extract_Subset, (typeof(ExtractSubsetView), typeof(ExtractSubsetViewModel)) },
            { AppPages.Prompt_Generator, (typeof(DatasetPromptGeneratorView), typeof(DatasetPromptGeneratorViewModel)) },
            { AppPages.Settings, (typeof(SettingsView), typeof(SettingsViewModel)) }
        };

        // Add Metadata on if the OS is Windows or macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _pageRegistry.Add(AppPages.Metadata_Viewer, (typeof(MetadataView), typeof(MetadataViewModel)));
        }
    }

    /// <summary>
    /// Navigates to a specific page and updates the dynamic view accordingly.
    /// </summary>
    /// <param name="parameter">The target page to navigate to.</param>
    [RelayCommand]
    public void NavigateToPage(AppPages parameter)
    {
        _inputHooks.UnsubscribeFromInputEvents();
        SetAllViewsAsInactive();

        if (!_viewCache.TryGetValue(parameter, out UserControl targetView))
        {
            (Type viewType, Type viewModelType) = _pageRegistry[parameter];
            targetView = (UserControl)Activator.CreateInstance(viewType);
            BaseViewModel viewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
            viewModel.Initialize(_clipboard, _storageProvider);
            targetView.DataContext = viewModel;
            _viewCache[parameter] = targetView;
        }

        DynamicView = targetView;
        SetViewAsActive(parameter);
        SetPageName(parameter);
    }

    /// <summary>
    /// Navigates to the tag editor view and updates the current selected tags.
    /// </summary>
    [RelayCommand]
    public void NavigateToTagEditorView()
    {
        TagEditorViewModel tagEditorViewModel = _serviceProvider.GetRequiredService<TagEditorViewModel>();
        tagEditorViewModel.Initialize(_clipboard, _storageProvider);
        tagEditorViewModel.UpdateCurrentSelectedTags();

        NavigateToPage(AppPages.Tag_Editor);
        _inputHooks.SubscribeToInputEvents();
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
        foreach (UserControl view in _viewCache.Values)
        {
            if (view.DataContext is BaseViewModel viewModel)
            {
                viewModel.Initialize(clipboard, storageProvider);
            }
        }

        if (DynamicView == null)
        {
            NavigateToPage(AppPages.Welcome);
        }
    }

    /// <summary>
    /// Unsubscribe from Input Events.
    /// </summary>
    public void UnsubscribeFromInputEvents()
    {
        _inputHooks.UnsubscribeFromInputEvents();
    }

    /// <summary>
    /// Subscribe to Input Events.
    /// </summary>
    public void SubscribeToInputEvents()
    {
        _inputHooks.SubscribeToInputEvents();
    }

    /// <summary>
    /// Checks if the model manager is currently downloading models.
    /// </summary>
    /// <returns></returns>
    public bool IsDownloading()
    {
        return _modelManager.IsDownloading;
    }

    /// <summary>
    /// Sets all views as inactive by updating their IsActive properties to false.
    /// </summary>
    private void SetAllViewsAsInactive()
    {
        foreach (UserControl viewInstance in _viewCache.Values)
        {
            if (viewInstance.DataContext is BaseViewModel viewModel)
            {
                viewModel.IsActive = false;
            }
        }
    }

    /// <summary>
    /// Sets a specific view as active by updating its IsActive property to true.
    /// </summary>
    /// <param name="view">The target view to set as active.</param>
    private void SetViewAsActive(AppPages view)
    {
        if (_viewCache.TryGetValue(view, out UserControl viewInstance) && viewInstance.DataContext is BaseViewModel viewModel)
        {
            viewModel.IsActive = true;
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

        if (args.PropertyName == nameof(ILoggerService.MessageColor))
        {
            ILoggerService logger = (sender as ILoggerService);

            switch (logger.MessageColor)
            {
                case SmartData.Lib.Enums.LogMessageColor.Error:
                    LogMessageColor = new SolidColorBrush(Colors.IndianRed);
                    break;
                case SmartData.Lib.Enums.LogMessageColor.Warning:
                    LogMessageColor = new SolidColorBrush(Colors.Yellow);
                    break;
                case SmartData.Lib.Enums.LogMessageColor.Informational:
                    LogMessageColor = new SolidColorBrush(Colors.LightGreen);
                    break;
                default:
                    LogMessageColor = new SolidColorBrush(Colors.IndianRed);
                    break;
            }

            OnPropertyChanged(nameof(LogMessageColor));
        }
    }
}