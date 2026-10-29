using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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

        private double _scale = 1.0;
        private double _translateX = 0.0;
        private double _translateY = 0.0;
        private const double ZoomFactor = 1.15;
        private const double MinScale = 1.0;
        private const double MaxScale = 10.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagEditorView"/> class.
        /// </summary>
        public TagEditorView()
        {
            InitializeComponent();

            EditorImage.GetObservable(Image.SourceProperty).Subscribe(_ => ResetZoom());
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

        /// <summary>
        /// Handles a event to blur the current image in the editor.
        /// </summary>
        public async Task OnBlurButtonDown()
        {
            if (_viewModel.IsActive)
            {
                await _viewModel.BlurImageAsync();
            }
        }

        /// <summary>
        /// Handles pointer wheel events to perform focal zoom and pan adjustments on the editor image relative to the mouse cursor position.
        /// </summary>
        /// <param name="e">The <see cref="PointerWheelEventArgs"/> containing pointer wheel event data.</param>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            PointerPointProperties pointerPointProperties = e.GetCurrentPoint(this).Properties;

            if (!EditorImage.IsPointerOver)
            {
                return;
            }

            // Get the mouse position relative to the image's parent container (the viewport)
            // Using the parent control gives a stable reference frame unaffected by the image's own transform
            Visual parentContainer = EditorImage.Parent as Visual ?? EditorImage;
            Point mousePos = e.GetPosition(parentContainer);

            double oldScale = _scale;

            // Multiplicative scaling gives consistent speed at both 1x and 8x zoom
            if (e.Delta.Y > 0)
            {
                _scale *= ZoomFactor;
            }
            else
            {
                _scale /= ZoomFactor;
            }
            _scale = Math.Clamp(_scale, MinScale, MaxScale);

            if (Math.Abs(_scale - oldScale) < 0.001)
            {
                return;
            }

            if (_scale <= MinScale)
            {
                // Reset when zoomed all the way out
                _scale = MinScale;
                _translateX = 0;
                _translateY = 0;
            }
            else
            {
                // Focal Zoom Math: adjust pan so the pixel under mousePos stays under mousePos
                double scaleRatio = _scale / oldScale;
                _translateX = mousePos.X - (mousePos.X - _translateX) * scaleRatio;
                _translateY = mousePos.Y - (mousePos.Y - _translateY) * scaleRatio;
            }

            // Anchor origin to Top-Left so our TranslateTransform math aligns 1:1
            EditorImage.RenderTransformOrigin = RelativePoint.TopLeft;

            // Apply both Scale and Translation together via TransformGroup
            TransformGroup transformGroup = new TransformGroup();

            transformGroup.Children.Add(new ScaleTransform(_scale, _scale));
            transformGroup.Children.Add(new TranslateTransform(_translateX, _translateY));

            EditorImage.RenderTransform = transformGroup;

            e.Handled = true;
        }

        /// <summary>
        /// Handles pointer press events to enable mouse button navigation (XButton1 and XButton2) and image blurring via the middle mouse button.
        /// </summary>
        /// <param name="e">The <see cref="PointerPressedEventArgs"/> containing pointer event data.</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            PointerPointProperties props = e.GetCurrentPoint(this).Properties;

            // Mouse Button 4 (Back) -> Previous image
            if (props.IsXButton1Pressed)
            {
                NavigatePreservingCaret("-1");
                e.Handled = true;
            }
            // Mouse Button 5 (Forward) -> Next image
            else if (props.IsXButton2Pressed)
            {
                NavigatePreservingCaret("1");
                e.Handled = true;
            }
            // Middle Mouse Button -> Toggle Blur
            else if (props.IsMiddleButtonPressed)
            {
                _viewModel?.BlurImageCommand.Execute(null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Navigates to a relative image index while preserving the current caret position within the tag editor.
        /// </summary>
        /// <param name="step">The relative step or offset to navigate (e.g., "-1" for previous, "1" for next).</param>
        private void NavigatePreservingCaret(string step)
        {
            if (_viewModel is not { IsActive: true })
            {
                return;
            }

            int caretOffset = EditorTags.CaretOffset;
            _viewModel.GoToItemCommand.Execute(step);

            // Keep the text cursor position stable after image switch
            Dispatcher.UIThread.Post(() =>
            {
                EditorTags.CaretOffset = Math.Clamp(caretOffset, 0, EditorTags.Text.Length);
            });
        }

        /// <summary>
        /// Resets the zoom scale and pan translation offsets back to their default values and clears the image's render transform.
        /// </summary>
        private void ResetZoom()
        {
            _scale = 1.0;
            _translateX = 0.0;
            _translateY = 0.0;
            if (EditorImage != null)
            {
                EditorImage.RenderTransform = null;
            }
        }
    }
}