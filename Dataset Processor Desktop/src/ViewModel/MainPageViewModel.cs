using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;
using Dataset_Processor_Desktop.src.Views;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class MainPageViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        private readonly IFileManipulatorService _fileManipulatorService;
        private readonly IImageProcessorService _imageProcessorService;
        private readonly IAutoTaggerService _autoTaggerService;
        private readonly ITagProcessorService _tagProcessorService;

        #region Definition of App Views.
        private View _dynamicContentView;

        private View _welcomePage;
        private View _datasetSortView;
        private View _resizeImagesView;
        private View _tagGenerationView;
        private View _tagProcessingView;
        private View _settingsView;
        #endregion

        public View DynamicContentView
        {
            get => _dynamicContentView;
            set
            {
                _dynamicContentView = value;
                OnPropertyChanged("DynamicContentView");
            }
        }

        public RelayCommand NavigateToSettingsCommand { get; private set; }
        public RelayCommand NavigateToDatasetSortCommand { get; private set; }
        public RelayCommand NavigateToResizeImagesCommand { get; private set; }
        public RelayCommand NavigateToTagGenerationCommand { get; private set; }
        public RelayCommand NavigateToTagProcessingCommand { get; private set; }

        public MainPageViewModel()
        {
            _folderPickerService = Application.Current.Handler.MauiContext.Services.GetService<IFolderPickerService>();
            _fileManipulatorService = Application.Current.Handler.MauiContext.Services.GetService<IFileManipulatorService>();
            _imageProcessorService = Application.Current.Handler.MauiContext.Services.GetService<IImageProcessorService>();
            _autoTaggerService = Application.Current.Handler.MauiContext.Services.GetService<IAutoTaggerService>();
            _tagProcessorService = Application.Current.Handler.MauiContext.Services.GetService<ITagProcessorService>();

            _welcomePage = new WelcomeView();
            _dynamicContentView = _welcomePage;
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsView);
            NavigateToDatasetSortCommand = new RelayCommand(NavigateToDatasetSortView);
            NavigateToResizeImagesCommand = new RelayCommand(NavigateToResizeImagesView);
            NavigateToTagGenerationCommand = new RelayCommand(NavigateToTagGenerationView);
            NavigateToTagProcessingCommand = new RelayCommand(NavigateToTagProcessingView);
        }

        public void NavigateToSettingsView()
        {
            if (_settingsView == null)
            {
                _settingsView = new SettingsView();
            }
            DynamicContentView = _settingsView;
        }

        public void NavigateToDatasetSortView()
        {
            if (_datasetSortView == null)
            {
                _datasetSortView = new DatasetSortView(_folderPickerService, _fileManipulatorService);
            }
            DynamicContentView = _datasetSortView;
        }

        public void NavigateToResizeImagesView()
        {
            if (_resizeImagesView == null)
            {
                _resizeImagesView = new ResizeImagesView(_folderPickerService, _fileManipulatorService, _imageProcessorService);
            }
            DynamicContentView = _resizeImagesView;
        }

        public void NavigateToTagGenerationView()
        {
            if (_tagGenerationView == null)
            {
                _tagGenerationView = new TagGenerationView(_folderPickerService, _fileManipulatorService, _autoTaggerService);
            }
            DynamicContentView = _tagGenerationView;
        }

        public void NavigateToTagProcessingView()
        {
            if (_tagProcessingView == null)
            {
                _tagProcessingView = new TagProcessingView(_folderPickerService, _tagProcessorService, _fileManipulatorService);
            }
            DynamicContentView = _tagProcessingView;
        }
    }
}
