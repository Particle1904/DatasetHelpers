using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class SortImagesViewModel : ViewModelBase
    {
        public SortImagesViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
