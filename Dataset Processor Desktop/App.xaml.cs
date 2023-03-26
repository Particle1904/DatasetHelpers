namespace Dataset_Processor_Desktop
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            window.MinimumWidth = 1280;
            window.MinimumHeight = 960;
            window.Title = "Dataset Processor All-in-one tools - v1.0";

            return window;
        }
    }
}