using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class DatasetPromptGeneratorViewModel : ViewModelBase
    {
        public DatasetPromptGeneratorViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
