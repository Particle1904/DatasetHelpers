using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using DatasetProcessor.ViewModels;

using System;
using System.Diagnostics;

namespace DatasetProcessor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            WindowState = WindowState.Maximized;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (_viewModel != null && _viewModel.IsDownloading())
            {
                e.Cancel = true;
            }

            base.OnClosing(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            PointerPointProperties pointerPointProperties = e.GetCurrentPoint(this).Properties;

            if (_viewModel.DynamicView is TagEditorView tagEditorView)
            {
                if (pointerPointProperties.IsXButton1Pressed)
                {
                    _ = tagEditorView.OnNavigationButtonDown("-1");
                    e.Handled = true;
                }
                else if (pointerPointProperties.IsXButton2Pressed)
                {
                    _ = tagEditorView.OnNavigationButtonDown("1");
                    e.Handled = true;
                }
                else if (pointerPointProperties.IsMiddleButtonPressed)
                {
                    _ = tagEditorView.OnBlurButtonDown();
                    e.Handled = true;
                }
            }

            base.OnPointerPressed(e);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            base.OnDataContextChanged(e);

            if (_viewModel?.Logger != null)
            {
                _viewModel.Logger.LatestLogChangedEvent += (sender, args) =>
                {
                    if (args is SmartData.Lib.Enums.LogMessageColor.Error)
                    {
                        MainViewControl.FlyoutButton.Flyout.ShowAt(MainViewControl.FlyoutButton);
                    }
                };
            }
        }
    }
}