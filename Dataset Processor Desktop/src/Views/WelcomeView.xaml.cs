using Dataset_Processor_Desktop.src.ViewModel;

namespace Dataset_Processor_Desktop.src.Views;

public partial class WelcomeView : ContentView
{
    private WelcomeViewModel _viewModel;
    public WelcomeView()
    {
        InitializeComponent();

        _viewModel = new WelcomeViewModel();

        BindingContext = _viewModel;
    }
}