using SmartData.Lib.Models;

namespace SmartData.Lib.Interfaces
{
    public interface IConfigsService
    {
        public Config Configurations { get; set; }

        public string TaggerThresholdDescription { get; }
        public string DiscardedFolderDescription { get; }
        public string SelectedFolderDescription { get; }
        public string BackupFolderDescription { get; }
        public string ResizedFolderDescription { get; }
        public string CombinedFolderDescription { get; }

        public Task LoadConfigurations();
        public Task SaveConfigurations();
        public Task CreateConfigFileIfNotExist();
    }
}
