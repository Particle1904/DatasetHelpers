namespace SmartData.Lib.Interfaces
{
    public interface ILoggerService
    {
        public string LatestLogMessage { get; set; }
        public Task SaveExceptionStackTrace(Exception exception);
    }
}
