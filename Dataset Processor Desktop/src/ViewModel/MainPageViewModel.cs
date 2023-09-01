// Ignore Spelling: Metadata

using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Utilities;
using Dataset_Processor_Desktop.src.Views;

using SmartData.Lib.Interfaces;

using System.ComponentModel;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class MainPageViewModel : BaseViewModel
    {
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IImageProcessorService _imageProcessorService;
        private readonly IAutoTaggerService _autoTaggerService;
        private readonly ITagProcessorService _tagProcessorService;
        private readonly IContentAwareCropService _contentAwareCropService;
        private readonly IInputHooksService _inputHooksService;

        #region Definition of App Views.
        private View _dynamicContentView;

        private Dictionary<AppViews, View> views;

        #endregion

        public string LatestLogMessage
        {
            get => _loggerService.LatestLogMessage;
        }

        public View DynamicContentView
        {
            get => _dynamicContentView;
            protected set
            {
                _dynamicContentView = value;
                OnPropertyChanged(nameof(DynamicContentView));
            }
        }

        public RelayCommand NavigateToWelcomePageCommand { get; private set; }
        public RelayCommand NavigateToDatasetSortCommand { get; private set; }
        public RelayCommand NavigateToContentAwareCropCommand { get; private set; }
        public RelayCommand NavigateToResizeImagesCommand { get; private set; }
        public RelayCommand NavigateToTagGenerationCommand { get; private set; }
        public RelayCommand NavigateToCaptionProcessingCommand { get; private set; }
        public RelayCommand NavigateToTagProcessingCommand { get; private set; }
        public RelayCommand NavigateToTagEditorCommand { get; private set; }
        public RelayCommand NavigateToExtractSubsetCommand { get; private set; }
        public RelayCommand NavigateToMetadataCommand { get; private set; }
        public RelayCommand NavigateToSettingsCommand { get; private set; }

        public MainPageViewModel()
        {
            _fileManipulatorService = Application.Current.Handler.MauiContext.Services.GetService<IFileManipulatorService>();
            _imageProcessorService = Application.Current.Handler.MauiContext.Services.GetService<IImageProcessorService>();
            _autoTaggerService = Application.Current.Handler.MauiContext.Services.GetService<IAutoTaggerService>();
            _tagProcessorService = Application.Current.Handler.MauiContext.Services.GetService<ITagProcessorService>();
            _contentAwareCropService = Application.Current.Handler.MauiContext.Services.GetService<IContentAwareCropService>();
            _inputHooksService = Application.Current.Handler.MauiContext.Services.GetService<IInputHooksService>();

            ((INotifyPropertyChanged)_loggerService).PropertyChanged += OnLoggerServicePropertyChanged;

            views = new Dictionary<AppViews, View>()
            {
                { AppViews.Welcome, new WelcomeView() },
                { AppViews.DatasetSort, new DatasetSortView(_fileManipulatorService) },
                { AppViews.ContentAwareCrop, new ContentAwareCroppingView(_fileManipulatorService, _contentAwareCropService) },
                { AppViews.ResizeImages, new ResizeImagesView(_fileManipulatorService, _imageProcessorService) },
                { AppViews.TagGeneration, new TagGenerationView(_fileManipulatorService, _autoTaggerService) },
                { AppViews.CaptionProcessing, new CaptionProcessingView(_tagProcessorService, _fileManipulatorService) },
                { AppViews.TagProcessing, new TagProcessingView(_tagProcessorService, _fileManipulatorService) },
                { AppViews.TagEditor, new TagEditorView(_fileManipulatorService, _imageProcessorService, _inputHooksService) },
                { AppViews.ExtractSubset, new ExtractSubsetView(_fileManipulatorService) },
                { AppViews.Metadata, new MetadataView(_imageProcessorService, _autoTaggerService) },
                { AppViews.Settings, new SettingsView() }
            };

            _dynamicContentView = views[AppViews.Welcome];

            NavigateToWelcomePageCommand = new RelayCommand(() => NavigateToPage(AppViews.Welcome));
            NavigateToDatasetSortCommand = new RelayCommand(() => NavigateToPage(AppViews.DatasetSort));
            NavigateToContentAwareCropCommand = new RelayCommand(() => NavigateToPage(AppViews.ContentAwareCrop));
            NavigateToResizeImagesCommand = new RelayCommand(() => NavigateToPage(AppViews.ResizeImages));
            NavigateToTagGenerationCommand = new RelayCommand(() => NavigateToPage(AppViews.TagGeneration));
            NavigateToCaptionProcessingCommand = new RelayCommand(() => NavigateToPage(AppViews.CaptionProcessing));
            NavigateToTagProcessingCommand = new RelayCommand(() => NavigateToPage(AppViews.TagProcessing));
            NavigateToTagEditorCommand = new RelayCommand(NavigateToTagEditorView);
            NavigateToExtractSubsetCommand = new RelayCommand(() => NavigateToPage(AppViews.ExtractSubset));
            NavigateToMetadataCommand = new RelayCommand(() => NavigateToPage(AppViews.Metadata));
            NavigateToSettingsCommand = new RelayCommand(() => NavigateToPage(AppViews.Settings));

            try
            {
                _configsService.LoadConfigurations();
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = "An error occured while trying to load the .cfg file. A brand new one will be generated instead.";
                _loggerService.SaveExceptionStackTrace(exception);
            }

            _inputHooksService.IsActive = true;
        }

        public void NavigateToPage(AppViews view)
        {
            SetAllViewsAsInactive();
            SetViewAsActive(view);
            DynamicContentView = views[view];
        }

        public void NavigateToTagEditorView()
        {
            TagEditorViewModel tagEditorViewModel = (TagEditorViewModel)views[AppViews.TagEditor].BindingContext;
            tagEditorViewModel.UpdateCurrentSelectedTags();
            NavigateToPage(AppViews.TagEditor);
        }

        private void OnLoggerServicePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ILoggerService.LatestLogMessage))
            {
                OnPropertyChanged(nameof(LatestLogMessage));
            }
        }

        private void SetAllViewsAsInactive()
        {
            foreach (var item in views)
            {
                BaseViewModel bindingContext = (BaseViewModel)item.Value.BindingContext;
                if (bindingContext != null)
                {
                    bindingContext.IsActive = false;
                }
            }
        }

        private void SetViewAsActive(AppViews view)
        {
            BaseViewModel bindingContext = (BaseViewModel)views[view].BindingContext;
            if (bindingContext != null)
            {
                bindingContext.IsActive = true;
            }
        }
    }
}
