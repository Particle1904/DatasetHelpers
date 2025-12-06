using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Immutable;

using System;
using System.Reactive.Linq;
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Metadata_ViewerButton.IsEnabled = false;
                Metadata_ViewerButton.IsVisible = false;
            }

            if (FlyoutPanel != null && MainContentScrowViewer != null)
            {
                FlyoutPanel.Bind(WidthProperty,
                    MainContentScrowViewer.GetObservable(BoundsProperty)
                    .Select(bounds => Math.Max(0, bounds.Width - 20)));
            }
        }

        private void OnNavigationButton(object? sender, RoutedEventArgs e)
        {
            ImmutableSolidColorBrush primaryColor = (ImmutableSolidColorBrush)Application.Current.Resources["Primary"];
            foreach (Control item in LeftMenuStackPanel.Children)
            {
                if (item.GetType() == typeof(Button))
                {
                    (item as Button).Background = primaryColor;
                }
            }

            (sender as Button).Background = (ImmutableSolidColorBrush)Application.Current.Resources["SecondaryDark"];
        }
    }
}