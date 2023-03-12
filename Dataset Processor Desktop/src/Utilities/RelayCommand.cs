using System.Windows.Input;

namespace Dataset_Processor_Desktop.src.Utilities
{
    class RelayCommand : ICommand
    {
        private readonly Action _execute;

        private readonly Func<bool> _canExecute;
        private readonly Func<Task> _canExecuteAsync;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute) : this(execute, null)
        {

        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
