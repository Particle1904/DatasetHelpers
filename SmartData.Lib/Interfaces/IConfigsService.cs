using SmartData.Lib.Models;

namespace SmartData.Lib.Interfaces
{
    public interface IConfigsService
    {
        public Config Configurations { get; set; }

        public Task LoadConfigurations();
        public Task SaveConfigurations();
        public Task CreateConfigFileIfNotExist();
    }
}
