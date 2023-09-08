using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

namespace Dataset_Processor_Desktop.src.Views;

public partial class PromptGeneratorView : ContentView
{
    private readonly IPromptGeneratorService _promptGeneratorService;
    private readonly ITagProcessorService _tagProcessorService;

    private PromptGeneratorViewModel _viewModel;
    public PromptGeneratorView(IPromptGeneratorService promptGeneratorService, ITagProcessorService tagProcessorService)
    {
        InitializeComponent();

        _promptGeneratorService = promptGeneratorService;
        _tagProcessorService = tagProcessorService;

        _viewModel = new PromptGeneratorViewModel(_promptGeneratorService, _tagProcessorService);
        BindingContext = _viewModel;
    }
}