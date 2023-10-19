using Avalonia.Controls;

using DatasetProcessor.ViewModels;

namespace DatasetProcessor.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }
}
