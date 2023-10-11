using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class ProcessCaptionsViewModel : ViewModelBase
    {
        public ProcessCaptionsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
