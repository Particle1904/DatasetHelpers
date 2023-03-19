using SmartData.Lib.Interfaces;

using System.ComponentModel;

namespace SmartData.Lib.Services
{
    public class LoggerService : ILoggerService, INotifyPropertyChanged
    {
        private string _latestLogMessage = string.Empty;
        public string LatestLogMessage
        {
            get => _latestLogMessage;
            set
            {
                _latestLogMessage = value;
                OnPropertyChanged(nameof(LatestLogMessage));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
