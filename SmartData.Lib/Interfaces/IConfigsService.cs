using SmartData.Lib.Models;

namespace SmartData.Lib.Interfaces
{
    public interface IConfigsService
    {
        public Config Configurations { get; }

        public string TaggerThresholdDescription { get; }
        public string DiscardedFolderDescription { get; }
        public string SelectedFolderDescription { get; }
        public string BackupFolderDescription { get; }
        public string ResizedFolderDescription { get; }
        public string CombinedFolderDescription { get; }

        public Task LoadConfigurationsAsync();
        public Task SaveConfigurationsAsync();
    }
}
