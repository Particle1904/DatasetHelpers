using SmartData.Lib.Enums;

using System.Text;

namespace SmartData.Lib.Interfaces
{
    public interface ILoggerService
    {
        public string LogsFolder { get; }
        public string LatestLogMessage { get; }
        public LogMessageColor MessageColor { get; }
        public void SaveExceptionStackTrace(Exception exception);
        public Task SaveExceptionStackTraceAsync(Exception exception);
        public Task SaveStringBuilderToLogFile(StringBuilder stringBuilder);
        public void SetLatestLogMessage(string logMessage, LogMessageColor messageColor, bool playNotificationSound = true);

        public event EventHandler<LogMessageColor>? LatestLogChangedEvent;
    }
}
