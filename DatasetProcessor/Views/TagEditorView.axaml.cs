using Avalonia.Controls;
using Avalonia.Media;

using AvaloniaEdit.Document;

using DatasetProcessor.src.Classes;
using DatasetProcessor.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DatasetProcessor.Views
{
    public partial class TagEditorView : UserControl
    {
        private Color _highlightTextColor = Color.FromArgb(255, 255, 179, 71);

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private TimeSpan _highlightUpdateDelay = TimeSpan.FromSeconds(0.75);

        private TagEditorViewModel _viewModel;

        private TextDocument _textDocument;

        SolidColorBrush _transparentForeground;
        SolidColorBrush _highlightForeground;

        public TagEditorView()
        {
            InitializeComponent();

            EditorHighlight.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnEditorHighlightTextChanged(sender, args));
            EditorTags.TextChanged += OnTextChanged;
            _textDocument = new TextDocument();
            EditorTags.Document = _textDocument;
        }

        private void OnEditorHighlightTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                string[] tagsToHighlight = EditorHighlight.Text.Replace(", ", ",").Split(",");

                EditorTags.SyntaxHighlighting = new TagsSyntaxHighlight(_highlightTextColor, tagsToHighlight);
            }
        }

        private void OnTextChanged(object sender, EventArgs args)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnTagsPropertyChanged;
                _viewModel.CurrentImageTags = EditorTags.Text;
                _viewModel.PropertyChanged += OnTagsPropertyChanged;
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = (TagEditorViewModel)DataContext;
            _viewModel.PropertyChanged += OnTagsPropertyChanged;

            base.OnDataContextChanged(e);
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

        private void OnTagsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.CurrentImageTags))
            {
                EditorTags.Text = _viewModel.CurrentImageTags;
            }
        }
    }
}
