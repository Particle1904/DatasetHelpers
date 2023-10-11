using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    internal class ProcessTagsViewModel : ViewModelBase
    {
        public ProcessTagsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
