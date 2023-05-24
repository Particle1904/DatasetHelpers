using SmartData.Lib.Interfaces;

using System.ComponentModel;
using System.Diagnostics;

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
                CleanLogMessage(TimeSpan.FromSeconds(60));
            }
        }

        private Stopwatch _stopwatch = new Stopwatch();

        public event PropertyChangedEventHandler? PropertyChanged;

        private async void CleanLogMessage(TimeSpan timeSpan)
        {
            _stopwatch.Restart();
            await Task.Delay(timeSpan);
            _latestLogMessage = string.Empty;
            OnPropertyChanged(nameof(LatestLogMessage));
            _stopwatch.Stop();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
