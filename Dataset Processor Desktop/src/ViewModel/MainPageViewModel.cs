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
        private readonly IAutoCaptionService _autoCaptionService;
        private readonly ITagProcessorService _tagProcessorService;
        private readonly IContentAwareCropService _contentAwareCropService;

        #region Definition of App Views.
        private View _dynamicContentView;

        private View _welcomePage;
        private View _datasetSortView;
        private View _contentAwareCropView;
        private View _resizeImagesView;
        private View _tagGenerationView;
        private View _captionGenerationView;
        private View _tagProcessingView;
        private View _tagEditorView;
        private View _metadataView;
        private View _settingsView;
        #endregion

        public string LatestLogMessage
        {
            get => _loggerService.LatestLogMessage;
        }

        public View DynamicContentView
        {
            get => _dynamicContentView;
            set
            {
                _dynamicContentView = value;
                OnPropertyChanged("DynamicContentView");
            }
        }

        public RelayCommand NavigateToWelcomePageCommand { get; private set; }
        public RelayCommand NavigateToDatasetSortCommand { get; private set; }
        public RelayCommand NavigateToContentAwareCropCommand { get; private set; }
        public RelayCommand NavigateToResizeImagesCommand { get; private set; }
        public RelayCommand NavigateToTagGenerationCommand { get; private set; }
        public RelayCommand NavigateToCaptionGenerationCommand { get; private set; }
        public RelayCommand NavigateToTagProcessingCommand { get; private set; }
        public RelayCommand NavigateToTagEditorCommand { get; private set; }
        public RelayCommand NavigateToMetadataCommand { get; private set; }
        public RelayCommand NavigateToSettingsCommand { get; private set; }

        public MainPageViewModel()
        {
            _fileManipulatorService = Application.Current.Handler.MauiContext.Services.GetService<IFileManipulatorService>();
            _imageProcessorService = Application.Current.Handler.MauiContext.Services.GetService<IImageProcessorService>();
            _autoTaggerService = Application.Current.Handler.MauiContext.Services.GetService<IAutoTaggerService>();
            _autoCaptionService = Application.Current.Handler.MauiContext.Services.GetService<IAutoCaptionService>();
            _tagProcessorService = Application.Current.Handler.MauiContext.Services.GetService<ITagProcessorService>();
            _contentAwareCropService = Application.Current.Handler.MauiContext.Services.GetService<IContentAwareCropService>();

            ((INotifyPropertyChanged)_loggerService).PropertyChanged += OnLoggerServicePropertyChanged;

            _welcomePage = new WelcomeView();
            _dynamicContentView = _welcomePage;
            NavigateToWelcomePageCommand = new RelayCommand(NavigateToWelcomeView);
            NavigateToDatasetSortCommand = new RelayCommand(NavigateToDatasetSortView);
            NavigateToContentAwareCropCommand = new RelayCommand(NavigateToContentAwareCropView);
            NavigateToResizeImagesCommand = new RelayCommand(NavigateToResizeImagesView);
            NavigateToTagGenerationCommand = new RelayCommand(NavigateToTagGenerationView);
            NavigateToCaptionGenerationCommand = new RelayCommand(NavigateToCaptionGenerationView);
            NavigateToTagProcessingCommand = new RelayCommand(NavigateToTagProcessingView);
            NavigateToTagEditorCommand = new RelayCommand(NavigateToTagEditorView);
            NavigateToMetadataCommand = new RelayCommand(NavigateToMetadataView);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsView);

            _configsService.LoadConfigurations();
        }

        public void NavigateToWelcomeView()
        {
            if (_welcomePage == null)
            {
                _welcomePage = new WelcomeView();
            }
            DynamicContentView = _welcomePage;
        }

        public void NavigateToDatasetSortView()
        {
            if (_datasetSortView == null)
            {
                _datasetSortView = new DatasetSortView(_fileManipulatorService);
            }
            DynamicContentView = _datasetSortView;
        }

        public void NavigateToContentAwareCropView()
        {
            if (_contentAwareCropView == null)
            {
                _contentAwareCropView = new ContentAwareCroppingView(_fileManipulatorService, _contentAwareCropService);
            }
            DynamicContentView = _contentAwareCropView;
        }

        public void NavigateToResizeImagesView()
        {
            if (_resizeImagesView == null)
            {
                _resizeImagesView = new ResizeImagesView(_fileManipulatorService, _imageProcessorService);
            }
            DynamicContentView = _resizeImagesView;
        }

        public void NavigateToTagGenerationView()
        {
            if (_tagGenerationView == null)
            {
                _tagGenerationView = new TagGenerationView(_fileManipulatorService, _autoTaggerService);
            }
            DynamicContentView = _tagGenerationView;
        }

        public void NavigateToCaptionGenerationView()
        {
            if (_captionGenerationView == null)
            {
                _captionGenerationView = new CaptionGenerationView(_fileManipulatorService, _autoCaptionService);
            }
            DynamicContentView = _captionGenerationView;
        }

        public void NavigateToTagProcessingView()
        {
            if (_tagProcessingView == null)
            {
                _tagProcessingView = new TagProcessingView(_tagProcessorService, _fileManipulatorService);
            }
            DynamicContentView = _tagProcessingView;
        }

        public void NavigateToTagEditorView()
        {
            if (_tagEditorView == null)
            {
                _tagEditorView = new TagEditorView(_fileManipulatorService, _imageProcessorService);
            }

            var tagEditorViewModel = (TagEditorViewModel)_tagEditorView.BindingContext;
            tagEditorViewModel.UpdateCurrentSelectedTags();
            DynamicContentView = _tagEditorView;
        }

        public void NavigateToMetadataView()
        {
            if (_metadataView == null)
            {
                _metadataView = new MetadataView(_imageProcessorService, _autoTaggerService);
            }
            DynamicContentView = _metadataView;
        }

        public void NavigateToSettingsView()
        {
            if (_settingsView == null)
            {
                _settingsView = new SettingsView();
            }
            DynamicContentView = _settingsView;
        }

        private void OnLoggerServicePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ILoggerService.LatestLogMessage))
            {
                OnPropertyChanged(nameof(LatestLogMessage));
            }
        }
    }
}
