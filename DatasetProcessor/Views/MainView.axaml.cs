using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Immutable;

using System.Runtime.InteropServices;

namespace DatasetProcessor.Views
{

    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            TopLevel topLevel = TopLevel.GetTopLevel(this);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Metadata_ViewerButton.IsEnabled = false;
                Metadata_ViewerButton.IsVisible = false;
            }

        }

        private void OnNavigationButton(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var primaryColor = (ImmutableSolidColorBrush)Application.Current.Resources["Primary"];
            foreach (Control item in LeftMenuStackPanel.Children)
            {
                if (item.GetType() == typeof(Button))
                {
                    (item as Button).Background = primaryColor;
                }
            }

            (sender as Button).Background = (ImmutableSolidColorBrush)Application.Current.Resources["SecondaryDark"];
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            FlyoutButton.Width = MainContentScrowViewer.Bounds.Width;
            FlyoutPanel.Width = MainContentScrowViewer.Bounds.Width - 20;
        }
    }
}