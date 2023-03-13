using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Utilities;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class DatasetSortViewModel : BaseViewModel
    {
        private readonly IFolderPickerService _folderPicker;
        private string _selectedFolderPath;

        public string SelectedFolderPath
        {
            get => _selectedFolderPath;
            set
            {
                _selectedFolderPath = value;
                OnPropertyChanged(nameof(SelectedFolderPath));
            }
        }

        public RelayCommand SelectFolderCommand { get; private set; }

        public DatasetSortViewModel(IFolderPickerService folderPickerService)
        {
            _folderPicker = folderPickerService;

            SelectFolderCommand = new RelayCommand(async () => await SelectFolderAsync());
        }

        public async Task SelectFolderAsync()
        {
            var result = await _folderPicker.PickFolderAsync();
            if (result != null)
            {
                SelectedFolderPath = result;
            }
        }
    }
}