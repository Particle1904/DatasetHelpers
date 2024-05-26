using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using DatasetProcessor.ViewModels;

using System;

using Point = System.Drawing.Point;

namespace DatasetProcessor.Views
{
    public partial class InpaintView : UserControl
    {
        private InpaintViewModel? _viewModel;

        private bool _isDragging = false;
        private Point _startingPosition = Point.Empty;

        public InpaintView()
        {
            InitializeComponent();
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb(255, 255, 179, 71), 0.5f);
        }

        /// <summary>
        /// Handles the pointer press event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CanvasPressed(object? sender, PointerPressedEventArgs e)
        {
            bool isLeftButton = e.GetCurrentPoint(sender as Panel).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
            bool isRightButton = e.GetCurrentPoint(sender as Panel).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed;

            if (isLeftButton)
            {
                _viewModel.DrawingColor = System.Drawing.Color.White;
            }
            if (isRightButton)
            {
                _viewModel.DrawingColor = System.Drawing.Color.Black;
            }

            if (sender != null && e != null && _viewModel != null)
            {
                _isDragging = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the pointer movement event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void PointerMoving(object? sender, PointerEventArgs e)
        {
            Avalonia.Point cursorPosition = e.GetPosition(CanvasPanel);
            EllipseControl.RenderTransform = new TranslateTransform(cursorPosition.X - _viewModel.CircleRadius,
                cursorPosition.Y - _viewModel.CircleRadius);

            if (!_isDragging)
            {
                e.Handled = true;
                return;
            }
            else
            {
                Avalonia.Point clickPosition = e.GetPosition(sender as Panel);
                _viewModel.CirclePosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
            }
        }

        /// <summary>
        /// Handles the pointer release event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CanvasReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender != null && e != null && _viewModel != null)
            {
                _isDragging = false;
                _viewModel.SaveCurrentImageMask();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the pointer entered event on the Canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PointerEntered(object? sender, PointerEventArgs e)
        {
            EllipseControl.IsVisible = true;
        }

        /// <summary>
        /// Handles the pointer exited event on the Canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PointerExited(object? sender, PointerEventArgs e)
        {
            EllipseControl.IsVisible = false;
        }

        /// <summary>
        /// Overrides the DataContextChanged method to update the associated view model.
        /// </summary>
        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = DataContext as InpaintViewModel;
            base.OnDataContextChanged(e);
        }
    }
}
