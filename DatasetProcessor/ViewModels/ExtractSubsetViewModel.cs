using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class ExtractSubsetViewModel : ViewModelBase
    {
        public ExtractSubsetViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
