using Dataset_Processor_Desktop.src.Utilities;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class WelcomeViewModel : BaseViewModel
    {
        private const string _repoWebAddress = $@"https://github.com/Particle1904/DatasetHelpers";
        private const string _releasesWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/releases";
        private const string _wikiWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/wiki";
        private const string _issuesWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/issues";

        public RelayCommand OpenRepositoryPageCommand { get; private set; }
        public RelayCommand OpenReleasesPageCommand { get; private set; }
        public RelayCommand OpenWikiPageCommand { get; private set; }
        public RelayCommand OpenIssuesPageCommand { get; private set; }

        public WelcomeViewModel()
        {
            OpenRepositoryPageCommand = new RelayCommand(async () => await OpenWebPage(_repoWebAddress));
            OpenReleasesPageCommand = new RelayCommand(async () => await OpenWebPage(_releasesWebAddress));
            OpenWikiPageCommand = new RelayCommand(async () => await OpenWebPage(_wikiWebAddress));
            OpenIssuesPageCommand = new RelayCommand(async () => await OpenWebPage(_issuesWebAddress));
        }

        private async Task OpenWebPage(string webAddress)
        {
            try
            {
                Uri uri = new Uri(webAddress);
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception exception)
            {
                _loggerService.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                await _loggerService.SaveExceptionStackTrace(exception);
            }
        }
    }
}
