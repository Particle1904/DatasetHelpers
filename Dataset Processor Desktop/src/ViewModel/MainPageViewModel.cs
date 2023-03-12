using Dataset_Processor_Desktop.src.Utilities;
using Dataset_Processor_Desktop.src.Views;

using System.ComponentModel;
using System.Windows.Input;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region Definition of App Views.
        private View _dynamicContentView;

        private View _welcomePage;
        private View _datasetSortView;
        private View _settingsView;
        #endregion

        public View DynamicContentView
        {
            get
            {
                return _dynamicContentView;
            }
            set
            {
                _dynamicContentView = value;
                OnPropertyChanged("DynamicContentView");
            }
        }

        public ICommand NavigateToSettingsCommand { get; private set; }
        public ICommand NavigateToDatasetSortCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPageViewModel()
        {
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
                _datasetSortView = new DatasetSortView();
            }
            DynamicContentView = _datasetSortView;
        }

        public virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
