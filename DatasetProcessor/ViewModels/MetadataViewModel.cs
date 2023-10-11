using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class MetadataViewModel : ViewModelBase
    {
        public MetadataViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
