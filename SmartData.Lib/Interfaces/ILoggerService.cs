using SmartData.Lib.Enums;

using System.Text;

namespace SmartData.Lib.Interfaces
{
    public interface ILoggerService
    {
        public string LogsFolder { get; }
        public string LatestLogMessage { get; set; }
        public LogMessageColor MessageColor { get; set; }
        public Task SaveExceptionStackTrace(Exception exception);
        public Task SaveStringBuilderToLogFile(StringBuilder stringBuilder);
        public void SetLatestLogMessage(string logMessage, LogMessageColor messageColor);
    }
}
