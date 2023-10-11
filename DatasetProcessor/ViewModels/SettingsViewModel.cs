using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
