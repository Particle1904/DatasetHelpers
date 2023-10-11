using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class GenerateTagsViewModel : ViewModelBase
    {
        public GenerateTagsViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
