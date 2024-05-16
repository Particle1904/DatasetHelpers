using SmartData.Lib.Models.Configurations;

namespace SmartData.Lib.Interfaces
{
    public interface IConfigsService
    {
        public Config Configurations { get; }
        public Task LoadConfigurationsAsync();
        public Task SaveConfigurationsAsync();
    }
}
