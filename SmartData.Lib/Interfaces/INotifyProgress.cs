namespace SmartData.Lib.Interfaces
{
    public interface INotifyProgress
    {
        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;
    }
}