using CommunityToolkit.Mvvm.Input;

using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class WelcomeViewModel : ViewModelBase
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

        [RelayCommand]
        private async Task OpenWebPage(string webAddress)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = webAddress,
                        UseShellExecute = true
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", webAddress);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", webAddress);
                }
                else
                {
                    throw new PlatformNotSupportedException("Unsupported operating system");
                }
            }
            catch (Exception exception)
            {
                Logger.LatestLogMessage = $"Something went wrong! Error log will be saved inside the logs folder.";
                await Logger.SaveExceptionStackTrace(exception);
            }
        }
    }
}