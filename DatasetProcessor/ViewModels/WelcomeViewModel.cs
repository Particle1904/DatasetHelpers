using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class WelcomeViewModel : ViewModelBase
    {
        public WelcomeViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
