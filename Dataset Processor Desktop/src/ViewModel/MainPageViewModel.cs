using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;
using Dataset_Processor_Desktop.src.Views;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class MainPageViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPickerService;
        public IFolderPickerService FolderPickerService { get; set; }

        #region Definition of App Views.
        private View _dynamicContentView;

        private View _welcomePage;
        private View _datasetSortView;
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

        public MainPageViewModel()
        {
            _folderPickerService = Application.Current.Handler.MauiContext.Services.GetService<IFolderPickerService>();

            _welcomePage = new WelcomeView();
            _dynamicContentView = _welcomePage;
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsPage);
            NavigateToDatasetSortCommand = new RelayCommand(NavigateToDatasetSortView);
        }

        public void NavigateToSettingsPage()
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
                _datasetSortView = new DatasetSortView(_folderPickerService);
            }
            DynamicContentView = _datasetSortView;
        }
    }
}
