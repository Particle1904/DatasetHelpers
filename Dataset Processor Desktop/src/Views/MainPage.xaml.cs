using Dataset_Processor_Desktop.src.ViewModel;

namespace Dataset_Processor_Desktop
{
    public partial class MainPage
    {
        private MainPageViewModel _viewModel;


        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainPageViewModel();
            BindingContext = _viewModel;
        }
    }
}