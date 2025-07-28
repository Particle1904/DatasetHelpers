using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using DatasetProcessor.src.Classes;
using DatasetProcessor.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DatasetProcessor.Views
{
    /// <summary>
    /// A view for editing tags, with the ability to highlight specific tags by changing their text color.
    /// </summary>
    public partial class TagEditorView : UserControl
    {
        private Color _highlightTextColor = Color.FromArgb(255, 255, 179, 71);

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private TimeSpan _highlightUpdateDelay = TimeSpan.FromSeconds(0.75);

        private TagEditorViewModel? _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagEditorView"/> class.
        /// </summary>
        public TagEditorView()
        {
            InitializeComponent();

            EditorHighlight.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnEditorHighlightTextChanged());
            EditorTags.TextChanged += async (sender, args) => await OnEditorTextChangedAsync();
        }

        /// <summary>
        /// Handles the TextChanged event of the EditorHighlight control to update tag highlighting.
        /// </summary>
        private void OnEditorHighlightTextChanged()
        {
            if (string.IsNullOrEmpty(EditorHighlight.Text))
            {
                EditorTags.SyntaxHighlighting = null;
                return;
            }

            if (_viewModel != null)
            {
                string[] tagsToHighlight = EditorHighlight.Text.Replace(", ", ",").Split(",");

                EditorTags.SyntaxHighlighting = new TagsSyntaxHighlight(_highlightTextColor, tagsToHighlight);
            }
        }

        /// <summary>
        /// Handles the TextChanged event of the EditorTags control to process changes in tags.
        /// </summary>
        private async Task OnEditorTextChangedAsync()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnTagsPropertyChanged;
                _viewModel.CurrentImageTags = EditorTags.Text;
                _viewModel.PropertyChanged += OnTagsPropertyChanged;

                await _viewModel.CountTokensForCurrentImageCommand.ExecuteAsync(null);
            }
        }

        /// <summary>
        /// Overrides the DataContextChanged method to update the associated view model.
        /// </summary>
        protected override void OnDataContextChanged(EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnTagsPropertyChanged;
            }

            _viewModel = DataContext as TagEditorViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnTagsPropertyChanged;
            }

            base.OnDataContextChanged(e);
        }

        /// <summary>
        /// Asynchronously debounces an action to execute after a specified delay.
        /// </summary>
        private async Task DebounceOnTextChangedAsync(Action action)
        {
            await _cancellationTokenSource.CancelAsync();
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

        /// <summary>
        /// Handles property changes in the associated view model to update tag text.
        /// </summary>
        private void OnTagsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_viewModel.CurrentImageTags))
            {
                if (EditorTags.Text != _viewModel.CurrentImageTags)
                {
                    EditorTags.Text = _viewModel.CurrentImageTags;
                }
            }
        }

        /// <summary>
        /// Handles a button down event and navigates to the item with the specified index.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        /// <param name="index">The index of the item to navigate to.</param>
        public async Task OnNavigationButtonDown(string index)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                int caretOffset = EditorTags.CaretOffset;

                if (_viewModel.IsActive)
                {
                    _viewModel.GoToItemCommand.Execute(index);
                }

                EditorTags.CaretOffset = Math.Clamp(caretOffset, 0, EditorTags.Text.Length);
            });
        }
    }
}