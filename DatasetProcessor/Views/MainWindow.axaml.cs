using Avalonia.Controls;
using Avalonia.Interactivity;

using DatasetProcessor.ViewModels;

namespace DatasetProcessor.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        WindowState = WindowState.Maximized;
    }
}
