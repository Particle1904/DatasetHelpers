using SmartData.Lib.Interfaces;

namespace DatasetProcessor.ViewModels
{
    public partial class TagEditorViewModel : ViewModelBase
    {
        public TagEditorViewModel(ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
        }
    }
}
