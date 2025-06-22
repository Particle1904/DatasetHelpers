using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class WelcomeViewModel : BaseViewModel
    {
        private const string _repoWebAddress = $@"https://github.com/Particle1904/DatasetHelpers";
        public static string RepoWebAddress => _repoWebAddress;

        private const string _releasesWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/releases";
        public static string ReleasesWebAddress => _releasesWebAddress;

        private const string _wikiWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/wiki";
        public static string WikiWebAddress => _wikiWebAddress;

        private const string _issuesWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/issues";
        public static string IssuesWebAddress => _issuesWebAddress;

        public WelcomeViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}