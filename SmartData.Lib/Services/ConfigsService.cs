using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class ConfigsService : IConfigsService
    {
        public Config Configurations { get; set; }

        private readonly string _cfgFilePath = Path.Combine(AppContext.BaseDirectory, "config.cfg");

        private string _taggerThresholdDescription = "#Threshold for AutoTagger, must be a decimal value between 0-1.0 | 0 meaning 0% and 1 is 100%. Values will be clamped if necessary.";
        public string TaggerTrresholdDescription { get => _taggerThresholdDescription; }
        private string _sortedFolderDescription = "#Folder for Discarded Images.";
        public string SortedFolderDrescription { get => _sortedFolderDescription; }
        private string _selectedFolderDescription = "#Folder for Selected Images. This is also the folder the Resize page will use as Input.";
        public string SelectedFolderDescription { get => _selectedFolderDescription; }
        private string _backupFolderDescription = "#Folder for Backup.";
        public string BackupFolderDescription { get => _backupFolderDescription; }
        private string _resizedFolderDescription = "#Folder for Resized images output. This is also the folder the Generate page will use as Input.";
        public string ResizedFolderDescription { get => _resizedFolderDescription; }
        private string _combinedFolderDescription = "#Folder for Generated Tags and their corresponding Images. This is also the folder the Processor and Tag Editor pages will use as Input.";
        public string CombinedFolderDescription { get => _combinedFolderDescription; }

        public ConfigsService()
        {
            Configurations = new Config();
        }

        public async Task LoadConfigurations()
        {
            await CreateConfigFileIfNotExist();

            string[] file = await File.ReadAllLinesAsync(_cfgFilePath);

            IEnumerable<string> configLines = file.Where(x => !x.StartsWith("#"));

            foreach (string line in configLines)
            {
                if (line.StartsWith("TaggerThreshold"))
                {
                    Configurations.TaggerThreshold = GetFloatConfig(line);
                }
                else if (line.StartsWith("DiscardedFolder"))
                {
                    Configurations.DiscardedFolder = GetStringConfig(line);
                }
                else if (line.StartsWith("SortedFolder"))
                {
                    Configurations.SortedFolder = GetStringConfig(line);
                }
                else if (line.StartsWith("BackupFolder"))
                {
                    Configurations.BackupFolder = GetStringConfig(line);
                }
                else if (line.StartsWith("ResizedFolder"))
                {
                    Configurations.ResizedFolder = GetStringConfig(line);
                }
                else if (line.StartsWith("CombinedFolder"))
                {
                    Configurations.CombinedOutputFolder = GetStringConfig(line);
                }
            }
        }

        public async Task SaveConfigurations()
        {
            List<string> configsList = new List<string>
            {
                _taggerThresholdDescription,
                $"TaggerThreshold={Configurations.TaggerThreshold}",
                "\n",
                _sortedFolderDescription,
                $"DiscardedFolder={Configurations.DiscardedFolder}",
                "\n",
                _selectedFolderDescription,
                $"SortedFolder={Configurations.SortedFolder}",
                "\n",
                _backupFolderDescription,
                $"BackupFolder={Configurations.BackupFolder}",
                "\n",
                _resizedFolderDescription,
                $"ResizedFolder={Configurations.ResizedFolder}",
                "\n",
                _combinedFolderDescription,
                $"CombinedFolder={Configurations.CombinedOutputFolder}",
                "\n",
            };

            await File.WriteAllTextAsync(_cfgFilePath, string.Join(Environment.NewLine, configsList));
        }

        public async Task CreateConfigFileIfNotExist()
        {
            if (!File.Exists(_cfgFilePath))
            {
                List<string> configsList = new List<string>
                {
                    _taggerThresholdDescription,
                    $"TaggerThreshold={Configurations.TaggerThreshold}",
                    "\n",
                    _sortedFolderDescription,
                    $"DiscardedFolder={Path.Combine(AppContext.BaseDirectory, "discarded-images-output")}",
                    "\n",
                    _selectedFolderDescription,
                    $"SortedFolder={Path.Combine(AppContext.BaseDirectory, "sorted-images-output")}",
                    "\n",
                    _backupFolderDescription,
                    $"BackupFolder={Path.Combine(AppContext.BaseDirectory, "images-backup")}",
                    "\n",
                    _resizedFolderDescription,
                    $"ResizedFolder={Path.Combine(AppContext.BaseDirectory, "resized-images-output")}",
                    "\n",
                    _combinedFolderDescription,
                    $"CombinedFolder={Path.Combine(AppContext.BaseDirectory, "combined-images-output")}",
                    "\n",
                };

                await File.AppendAllLinesAsync(_cfgFilePath, configsList);
            }
        }

        /// <summary>
        /// Parses a float value from a configuration line in the format "key=value".
        /// </summary>
        /// <param name="line">The configuration line to parse.</param>
        /// <returns>The parsed float value.</returns>
        private float GetFloatConfig(string line)
        {
            string[] splitLine = line.Split("=");
            float.TryParse(splitLine.Last(), out float value);
            return value;
        }

        /// <summary>
        /// Gets the string configuration value from the specified configuration line.
        /// </summary>
        /// <param name="line">The configuration line to retrieve the value from.</param>
        /// <returns>The string configuration value.</returns>
        private string GetStringConfig(string line)
        {
            string[] splitLine = line.Split("=");
            return splitLine.Last();
        }
    }
}