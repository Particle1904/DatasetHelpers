using NetCoreAudio;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace SmartData.Lib.Services
{
    public class LoggerService : ILoggerService, INotifyPropertyChanged
    {
        private readonly Player _audioPlayer;
        private readonly string _notificationSoundPath;

        private string _latestLogMessage = string.Empty;
        public string LatestLogMessage
        {
            get => _latestLogMessage;
            private set
            {
                _latestLogMessage = value;
                OnPropertyChanged(nameof(LatestLogMessage));
                _ = CleanLogMessageAsync(TimeSpan.FromSeconds(60));
            }
        }

        private LogMessageColor _color = LogMessageColor.Error;
        public LogMessageColor MessageColor
        {
            get => _color;
            private set
            {
                _color = value;
                OnPropertyChanged(nameof(MessageColor));
            }
        }

        public string LogsFolder { get => GetLogsFolder(); }

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler<LogMessageColor>? LatestLogChangedEvent;

        public LoggerService()
        {
            _audioPlayer = new Player();

            string _assetsFolder = Path.Combine(AppContext.BaseDirectory, "Assets");
            _notificationSoundPath = Path.Combine(_assetsFolder, "system_notification.wav");
        }

        /// <summary>
        /// Set the latest Log Message and its color.
        /// </summary>
        /// <param name="logMessage">The Log message.</param>
        /// <param name="messageColor">The message color.</param>
        public void SetLatestLogMessage(string logMessage, LogMessageColor messageColor, bool playNotificationSound = true)
        {
            LatestLogMessage = logMessage;
            MessageColor = messageColor;

            if (playNotificationSound)
            {
                LatestLogChangedEvent?.Invoke(this, messageColor);
                try
                {
                    _audioPlayer.Play(_notificationSoundPath);
                }
                catch (Exception) { /* Ignore audio playback errors */}
            }
        }

        /// <summary>
        /// Saves the details of an exception and its stack trace to a text file.
        /// </summary>
        /// <param name="exception">The exception to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method creates a text file in the "logs" folder to store the details of the specified exception.
        /// It constructs a string containing the exception details, including the date and time, source, message, help link, HResult, and inner exception details if present.
        /// The method appends the stack trace and target site information to the string.
        /// It also includes additional information from the exception's data dictionary, if available.
        /// Finally, it appends the constructed string to the text file.
        /// </remarks>
        public async Task SaveExceptionStackTrace(Exception exception)
        {
            string outputFolder = GetLogsFolder();
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string filePath = Path.Combine(outputFolder, $"error_{GetTimeNowString(true)}.txt");

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Exception Details");
            stringBuilder.AppendLine("=================");
            stringBuilder.AppendLine($"Date and Time: {GetTimeNowString(false)}");
            stringBuilder.AppendLine($"Source: {exception.Source}");
            stringBuilder.AppendLine($"Message: {exception.Message}");
            stringBuilder.AppendLine($"Help Link: {exception.HelpLink}");
            stringBuilder.AppendLine($"HResult: {exception.HResult}");
            stringBuilder.AppendLine();

            if (exception.InnerException != null)
            {
                stringBuilder.AppendLine("Inner Exception Details");
                stringBuilder.AppendLine("======================");
                stringBuilder.AppendLine($"Source: {exception.InnerException.Source}");
                stringBuilder.AppendLine($"Message: {exception.InnerException.Message}");
                stringBuilder.AppendLine($"Help Link: {exception.InnerException.HelpLink}");
                stringBuilder.AppendLine($"HResult: {exception.InnerException.HResult}");
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine("Stack Trace");
            stringBuilder.AppendLine("============");
            stringBuilder.AppendLine(exception.StackTrace);
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("Target Site");
            stringBuilder.AppendLine("============");
            stringBuilder.AppendLine($"Declaring Type: {exception.TargetSite.DeclaringType}");
            stringBuilder.AppendLine($"Method Name: {exception.TargetSite.Name}");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine("Additional Information");
            stringBuilder.AppendLine("======================");
            foreach (object key in exception.Data.Keys)
            {
                stringBuilder.AppendLine($"{key}: {exception.Data[key]}");
            }

            await File.AppendAllTextAsync(filePath, stringBuilder.ToString());
        }

        /// <summary>
        /// Saves the contents of a StringBuilder to a log file named "consolidated_tags_log.txt"
        /// in the logs folder within the current application's directory.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder containing log content to be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveStringBuilderToLogFile(StringBuilder stringBuilder)
        {
            string outputFolder = GetLogsFolder();
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }
            string filePath = Path.Combine(outputFolder, $"consolidated_tags_log-{GetTimeNowString(true)}.txt");

            await File.WriteAllTextAsync(filePath, stringBuilder.ToString());
            LatestLogMessage = $"Saved a Log File with consolidated tags that looks like possible outliers. Please check the file: {filePath}";
        }

        /// <summary>
        /// Cleans the latest log message after a specified time span.
        /// </summary>
        /// <param name="timeSpan">The time span after which the latest log message should be cleaned.</param>
        /// <remarks>
        /// This method is invoked to clean the latest log message after a specified time span has elapsed.
        /// It starts by restarting the internal stopwatch to measure the time elapsed.
        /// The method then asynchronously delays for the specified time span using the <see cref="Task.Delay"/> method.
        /// After the delay, it sets the value of the latest log message to an empty string, effectively cleaning it.
        /// It raises the <see cref="PropertyChanged"/> event for the "LatestLogMessage" property to notify any subscribers of the change.
        /// Finally, it stops the stopwatch and the method execution completes.
        /// </remarks>
        private async Task CleanLogMessageAsync(TimeSpan timeSpan)
        {
            _stopwatch.Restart();
            await Task.Delay(timeSpan);
            _latestLogMessage = string.Empty;
            OnPropertyChanged(nameof(LatestLogMessage));
            _stopwatch.Stop();
        }

        /// <summary>
        /// Gets the folder path for storing log files within the current application's directory.
        /// </summary>
        /// <returns>The folder path for log storage.</returns>
        private static string GetLogsFolder() => Path.Combine(Environment.CurrentDirectory, "logs");

        /// <summary>
        /// Gets the current date and time formatted as a string in the specified format.
        /// </summary>
        /// <param name="isForFileName">A flag indicating whether the formatted string is intended for use in a file name.</param>
        /// <returns>A string representing the current date and time formatted as specified.</returns>
        private static string GetTimeNowString(bool IsForFileName)
        {
            if (IsForFileName)
            {
                return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            }

            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
