using Avalonia;
using Avalonia.Controls.Primitives;

using System.Windows.Input;

namespace DatasetProcessor.UserControls
{
    public class SelectFolderControl : TemplatedControl
    {
        public static readonly StyledProperty<string> ButtonTextProperty =
            AvaloniaProperty.Register<SelectFolderControl, string>(nameof(ButtonText));
        public string ButtonText
        {
            get => GetValue(ButtonTextProperty);
            set => SetValue(ButtonTextProperty, value);
        }

        public static readonly StyledProperty<string> LabelTextProperty =
            AvaloniaProperty.Register<SelectFolderControl, string>(nameof(LabelText));
        public string LabelText
        {
            get => GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public static readonly StyledProperty<ICommand> ButtonCommandProperty =
            AvaloniaProperty.Register<SelectFolderControl, ICommand>(nameof(SelectFolderButtonCommand));
        public ICommand SelectFolderButtonCommand
        {
            get => GetValue(ButtonCommandProperty);
            set => SetValue(ButtonCommandProperty, value);
        }

        public static readonly StyledProperty<ICommand> OpenFolderInExplorerCommandProperty =
            AvaloniaProperty.Register<SelectFolderControl, ICommand>(nameof(OpenFolderInExplorerCommand));
        public ICommand OpenFolderInExplorerCommand
        {
            get => GetValue(OpenFolderInExplorerCommandProperty);
            set => SetValue(OpenFolderInExplorerCommandProperty, value);
        }
    }
}
