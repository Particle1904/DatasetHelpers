using Avalonia;
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

        this.PropertyChanged += OnWindowPropertyChanged;
        this.Activated += OnWindowActivated;
        this.Deactivated += OnWindowDeactivated;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            WindowState newState = (WindowState)e.NewValue!;
            switch (newState)
            {
                // Disable/Enable InputEvents based on WindowState
                case WindowState.Minimized:
                    _viewModel.UnsubscribeFromInputEvents();
                    break;
                case WindowState.Normal:
                case WindowState.Maximized:
                    _viewModel.SubscribeToInputEvents();
                    break;
            }
        }
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        _viewModel.SubscribeToInputEvents();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        _viewModel.UnsubscribeFromInputEvents();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        WindowState = WindowState.Maximized;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_viewModel != null && _viewModel.ModelManager.IsDownloading)
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
