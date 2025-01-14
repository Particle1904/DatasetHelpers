using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class WelcomeViewModel : BaseViewModel
    {
        private const string _repoWebAddress = $@"https://github.com/Particle1904/DatasetHelpers";
        public string RepoWebAddress => _repoWebAddress;

        private const string _releasesWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/releases";
        public string ReleasesWebAddress => _releasesWebAddress;

        private const string _wikiWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/wiki";
        public string WikiWebAddress => _wikiWebAddress;

        private const string _issuesWebAddress = $@"https://github.com/Particle1904/DatasetHelpers/issues";
        public string IssuesWebAddress => _issuesWebAddress;

        public WelcomeViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}