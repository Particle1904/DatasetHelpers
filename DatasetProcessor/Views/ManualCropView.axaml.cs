using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

using DatasetProcessor.ViewModels;

using System;
using System.ComponentModel;
using System.Drawing;

namespace DatasetProcessor.Views
{
    public partial class ManualCropView : UserControl
    {
        private ManualCropViewModel? _viewModel;

        private bool _isDragging = false;
        private bool _shouldSaveCroppedImage = true;
        private Point _startingPosition = Point.Empty;

        Line[] _lines;

        public ManualCropView()
        {
            InitializeComponent();
            _lines = new Line[4];
            SolidColorBrush brush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(255, 255, 179, 71), 0.5f);
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i] = new Line()
                {
                    StrokeThickness = 3,
                    Stroke = brush
                };
                CanvasPanel.Children.Add(_lines[i]);
            }
        }

        /// <summary>
        /// Clears the lines representing the crop area by setting their stroke thickness to 0.
        /// </summary>
        /// <param name="sender">The object that triggered the PropertyChanged event.</param>
        /// <param name="e">The PropertyChangedEventArgs object containing information about the property change.</param>
        private void ClearLines(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("SelectedItemIndex"))
            {
                for (int i = 0; i < _lines.Length; i++)
                {
                    _lines[i].StrokeThickness = 0;
                }
            }
        }

        /// <summary>
        /// Handles the pointer press event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CanvasPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender != null && e != null && _viewModel != null)
            {
                Avalonia.Point clickPosition = e.GetPosition(sender as Button);
                _startingPosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
                _viewModel.StartingPosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
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
            if (!_isDragging)
            {
                return;
            }

            if (e.GetCurrentPoint(sender as Button).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                _isDragging = false;
                _shouldSaveCroppedImage = false;
                e.Handled = true;
                for (int i = 0; i < _lines.Length; i++)
                {
                    _lines[i].StrokeThickness = 0;
                }

                return;
            }

            Avalonia.Point cursorPosition = e.GetPosition(sender as Button);
            DrawCropAreaRectangle(cursorPosition);
            _shouldSaveCroppedImage = true;
        }

        /// <summary>
        /// Handles the pointer release event on the canvas.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void CanvasReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_shouldSaveCroppedImage)
            {
                e.Handled = true;
                return;
            }

            if (sender != null && e != null && _viewModel != null)
            {
                Avalonia.Point clickPosition = e.GetPosition(sender as Button);
                _viewModel.EndingPosition = new Point((int)clickPosition.X, (int)clickPosition.Y);
                _isDragging = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Draws the rectangle representing the crop area.
        /// </summary>
        /// <param name="cursorPosition">The current cursor position.</param>
        private void DrawCropAreaRectangle(Avalonia.Point cursorPosition)
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i].StrokeThickness = 3;
            }

            // TOP LINE
            _lines[0].StartPoint = new Avalonia.Point(_startingPosition.X, _startingPosition.Y);
            _lines[0].EndPoint = new Avalonia.Point(cursorPosition.X, _startingPosition.Y);

            // LEFT LINE
            _lines[1].StartPoint = new Avalonia.Point(cursorPosition.X, _startingPosition.Y);
            _lines[1].EndPoint = new Avalonia.Point(cursorPosition.X, cursorPosition.Y);

            // BOTTOM LINE
            _lines[2].StartPoint = new Avalonia.Point(_startingPosition.X, cursorPosition.Y);
            _lines[2].EndPoint = new Avalonia.Point(cursorPosition.X, cursorPosition.Y);

            // RIGHT LINE
            _lines[3].StartPoint = new Avalonia.Point(_startingPosition.X, _startingPosition.Y);
            _lines[3].EndPoint = new Avalonia.Point(_startingPosition.X, cursorPosition.Y);
        }

        /// <summary>
        /// Overrides the DataContextChanged method to update the associated view model.
        /// </summary>
        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = DataContext as ManualCropViewModel;
            _viewModel.PropertyChanged += (sender, e) => ClearLines(sender, e);
            base.OnDataContextChanged(e);
        }
    }
}
