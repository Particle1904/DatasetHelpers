using Dataset_Processor_Desktop.src.ViewModel;

using SmartData.Lib.Interfaces;

using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagEditorView : ContentView, INotifyPropertyChanged
{
    private readonly IFileManipulatorService _fileManipulatorService;
    private readonly IImageProcessorService _imageProcessorService;

    private TagEditorViewModel _viewModel;

    private Color _highlightBackgroundColor = new Color(255, 179, 71, 153);
    private Color _transparentColor = new Color(0, 0, 0, 0);
    private Color _textColor = new Color(255, 255, 255);

    private FormattedString _labelFormattedString;

    private CancellationTokenSource _cancellationTokenSource;

    public FormattedString LabelFormattedString
    {
        get => _labelFormattedString;
        set
        {
            _labelFormattedString = value;
            OnPropertyChanged(nameof(LabelFormattedString));
        }
    }

    public TagEditorView(IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _imageProcessorService = imageProcessorService;

        _viewModel = new TagEditorViewModel(_fileManipulatorService, _imageProcessorService);
        BindingContext = _viewModel;

        EditorHighlight.TextChanged += (sender, args) => DebounceOnTextChanged(() => OnTextChanged(sender, args));
        EditorTags.TextChanged += (sender, args) => DebounceOnTextChanged(() => OnTextChanged(sender, args));

        LabelFormattedString = new FormattedString();
    }

    private async void DebounceOnTextChanged(Action action)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Delay(500, _cancellationTokenSource.Token).ContinueWith(_ => action.Invoke(),
            CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (TaskCanceledException) { }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (string.IsNullOrEmpty(EditorHighlight.Text))
            {
                LabelFormattedString.Spans.Clear();
                LabelFormatted.FormattedText = LabelFormattedString;
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

            UpdateFormattedString(parts, wordsToHighlighSplit);

            LabelFormatted.FormattedText = LabelFormattedString;
        });
    }

    private void UpdateFormattedString(string[] parts, string[] wordsToHighlighSplit)
    {
        LabelFormattedString.Spans.Clear();

        for (int i = 0; i < parts.Length; i++)
        {
            Span span = new Span();
            span.Text = parts[i];

            if (i < parts.Length - 1)
            {
                span.Text += ", ";
            }

            foreach (string word in wordsToHighlighSplit)
            {
                Regex regex = new Regex(@"\b" + word + @"\b");
                Match match = regex.Match(span.Text);

                if (match.Success)
                {
                    span.BackgroundColor = _highlightBackgroundColor;
                    break;
                }
                else
                {
                    span.BackgroundColor = _transparentColor;
                }
            }
            span.TextColor = _textColor;

            LabelFormattedString.Spans.Add(span);
        }
    }
}