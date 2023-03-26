using Dataset_Processor_Desktop.src.ViewModel;

namespace Dataset_Processor_Desktop.src.Views;

public partial class SettingsView : ContentView
{
    private SettingsViewModel _viewModel;
    public SettingsView()
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel();
        BindingContext = _viewModel;
    }
}