using Dataset_Processor_Desktop.src.ViewModel;

using SharpHook;
using SharpHook.Native;

using SmartData.Lib.Interfaces;

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Dataset_Processor_Desktop.src.Views;

public partial class TagEditorView : ContentView, IDisposable
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

    SimpleGlobalHook _keyboardHook;
    Stopwatch _keyboardTimer;
    private TimeSpan _keyboardEventsDelay = TimeSpan.FromSeconds(0.1);

    public TagEditorView(IFileManipulatorService fileManipulatorService, IImageProcessorService imageProcessorService)
    {
        InitializeComponent();

        _fileManipulatorService = fileManipulatorService;
        _imageProcessorService = imageProcessorService;

        _viewModel = new TagEditorViewModel(_fileManipulatorService, _imageProcessorService);
        BindingContext = _viewModel;

        EditorHighlight.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnTextChanged(sender, args));
        EditorTags.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnTextChanged(sender, args));

        _keyboardTimer = new Stopwatch();
        _keyboardTimer.Start();

        _keyboardHook = new SimpleGlobalHook(true);
        _keyboardHook.KeyPressed += OnKeyDown;
        _keyboardHook.MousePressed += OnMouseButtonDown;
        _keyboardHook.RunAsync();

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
            LabelFormatted.FontSize = EditorTags.FontSize + 0.40f;
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

    private void OnKeyDown(object sender, KeyboardHookEventArgs e)
    {
        if (CanProcessHook())
        {
            return;
        }

        MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF1)
            {
                _viewModel.GoToPreviousItem();
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF2)
            {
                _viewModel.GoToNextItem();
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF3)
            {
                _viewModel.GoToPreviousTenItems();
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF4)
            {
                _viewModel.GoToNextTenItems();
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF5)
            {
                _viewModel.GoToPreviousOneHundredItems();
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF6)
            {
                _viewModel.GoToNextOneHundredItems();
            }
            else if (e.RawEvent.Keyboard.KeyCode == KeyCode.VcF8)
            {
                Task.Run(() => _viewModel.BlurImageAsync());
            }

            _keyboardTimer.Restart();
        });
    }

    private void OnMouseButtonDown(object sender, MouseHookEventArgs e)
    {
        if (CanProcessHook())
        {
            return;
        }

        MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (e.RawEvent.Mouse.Button == MouseButton.Button4)
            {
                _viewModel.GoToPreviousItem();
            }
            else if (e.RawEvent.Mouse.Button == MouseButton.Button5)
            {
                _viewModel.GoToNextItem();
            }
            else if (e.RawEvent.Mouse.Button == MouseButton.Button3)
            {
                Task.Run(() => _viewModel.BlurImageAsync());
            }

            _keyboardTimer.Restart();
        });
    }

    private bool CanProcessHook()
    {
        return _keyboardTimer.Elapsed.TotalMilliseconds <= _keyboardEventsDelay.TotalMilliseconds;
    }

    public void Dispose()
    {
        _keyboardHook.KeyPressed -= OnKeyDown;
        _keyboardHook?.Dispose();
        _keyboardHook = null;
    }
}