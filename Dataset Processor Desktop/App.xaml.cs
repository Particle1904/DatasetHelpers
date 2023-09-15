// Ignore Spelling: App

using System.Globalization;

namespace Dataset_Processor_Desktop
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SetDefaultLanguage("en-US");
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            window.MinimumWidth = 1280;
            window.MinimumHeight = 960;
            window.Title = "Dataset Processor All-in-one tools - v1.6.1";
            return window;
        }

        private static void SetDefaultLanguage(string cultureCode)
        {
            CultureInfo culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            //Preferences.Set("Language", cultureCode);
        }
    }
}