using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class ContentAwareCropViewModel : ViewModelBase
    {
        public ContentAwareCropViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
