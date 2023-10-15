using Avalonia.Controls;
using Avalonia.Media;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DatasetProcessor.Views
{
    public partial class TagEditorView : UserControl
    {
        private Color _highlightBackgroundColor = Color.FromArgb(153, 255, 179, 71);
        private Color _transparentColor = Color.FromArgb(0, 0, 0, 0);
        private Color _textColor = Color.FromRgb(255, 255, 255);

        private FormattedText _labelFormattedString;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private TimeSpan _highlightUpdateDelay = TimeSpan.FromSeconds(3);

        public TagEditorView()
        {
            InitializeComponent();

            //EditorHighlight.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnTextChanged(sender, args));
            //EditorTags.TextChanged += async (sender, args) => await DebounceOnTextChangedAsync(() => OnTextChanged(sender, args));
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
    }
}
