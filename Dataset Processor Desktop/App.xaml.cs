// Ignore Spelling: App

using System.Globalization;

namespace Dataset_Processor_Desktop
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
            var culture = new CultureInfo("en-US");
            Preferences.Set("Language", culture.Name);
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            window.MinimumWidth = 1280;
            window.MinimumHeight = 960;
            window.Title = "Dataset Processor All-in-one tools - v1.5";

            return window;
        }
    }
}