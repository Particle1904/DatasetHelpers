using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagEditorView : ContentView
{
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IImageProcessorService _imageProcessorService;

    private TagEditorViewModel _viewModel;

    private Color _highlightBackgroundColor = Color.FromRgba(255, 179, 71, 153);
    private Color _transparentColor = Color.FromRgba(0, 0, 0, 0);
    private Color _textColor = Color.FromRgb(255, 255, 255);

    private FormattedString _labelFormattedString;

    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private TimeSpan _highlightUpdateDelay = TimeSpan.FromSeconds(3);

    public TagEditorView(IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService, IInputHooksService inputHooksService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _imageProcessorService = imageProcessorService;

        _viewModel = new TagEditorViewModel(_fileManipulatorService, _imageProcessorService, inputHooksService);
        BindingContext = _viewModel;

        EditorHighlight.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnTextChanged(sender, args));
        EditorTags.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnTextChanged(sender, args));

        _labelFormattedString = new FormattedString();
    }

    private async Task DebounceOnTextChangedAsync(Action action)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(_highlightUpdateDelay, _cancellationTokenSource.Token);
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                action.Invoke();
            }
        }
        catch (TaskCanceledException)
        {
            Debug.WriteLine("Task canceled.");
        }
    }

    private async void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(EditorHighlight.Text))
        {
            _labelFormattedString.Spans.Clear();
            LabelFormatted.FormattedText = _labelFormattedString;
            return;
        }
        if (string.IsNullOrEmpty(_viewModel.WordsToHighlight))
        {
            return;
        }
        if (string.IsNullOrEmpty(EditorTags.Text))
        {
            return;
        }

        string editorText = EditorTags.Text;
        string[] parts = editorText.Replace(", ", ",").Split(",");
        string[] wordsToHighlighSplit = _viewModel.WordsToHighlight.Replace(", ", ",").Split(",");

        _labelFormattedString = await Task.Run(() => UpdateFormattedString(parts, wordsToHighlighSplit));

        MainThread.BeginInvokeOnMainThread(() =>
        {
            LabelFormatted.FormattedText = _labelFormattedString;
            LabelFormatted.FontSize = EditorTags.FontSize + 0.80f;
        });
    }

    private FormattedString UpdateFormattedString(string[] parts, string[] wordsToHighlighSplit)
    {
        FormattedString formattedString = new FormattedString();

        string regexSearchPattern = $@"\b({string.Join("|", wordsToHighlighSplit)})\b";
        Regex regex = new Regex(regexSearchPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        for (int i = 0; i < parts.Length; i++)
        {
            Span span = new Span();
            span.Text = parts[i];

            MatchCollection matches = regex.Matches(span.Text);
            if (matches.Count > 0)
            {
                span.BackgroundColor = _highlightBackgroundColor;
            }
            else
            {
                span.BackgroundColor = _transparentColor;
            }

            if (i < parts.Length - 1)
            {
                span.Text += ", ";
            }

            span.TextColor = _textColor;
            formattedString.Spans.Add(span);
        }

        return formattedString;
    }
}