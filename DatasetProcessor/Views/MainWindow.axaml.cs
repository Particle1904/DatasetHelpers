using Avalonia.Controls;

using DatasetProcessor.ViewModels;

using System;

namespace DatasetProcessor.Views;

public partial class MainWindow : Window
{
    public MainViewModel _viewModel;

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
        if (_viewModel != null && _viewModel.FileManipulator.IsDownloading)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _viewModel = DataContext as MainViewModel;
        base.OnDataContextChanged(e);

        _viewModel.Logger.LatestLogChangedEvent += (sender, args) =>
        {
            if (args is SmartData.Lib.Enums.LogMessageColor.Error)
            {
                MainViewControl.FlyoutButton.Flyout.ShowAt(MainViewControl.FlyoutButton);
            }
        };
    }
}
