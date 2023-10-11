using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class ResizeImagesViewModel : ViewModelBase
    {
        public ResizeImagesViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
