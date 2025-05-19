using Models.Configurations;

using SmartData.Lib.Enums;

namespace Interfaces
{
    public interface IModelManagerService
    {
        public event EventHandler<DownloadNotification> DownloadMessageEvent;
        public bool IsDownloading { get; }

        public bool FileNeedsToBeDownloaded(AvailableModels model);
        public Task DownloadModelFileAsync(AvailableModels model);
    }
}
