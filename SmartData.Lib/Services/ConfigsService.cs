using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

using System.ComponentModel;

namespace SmartData.Lib.Services
{
    public class ConfigsService : IConfigsService, INotifyPropertyChanged
    {
        public Config Configurations { get; set; }

        private readonly string _cfgFilePath = Path.Combine(AppContext.BaseDirectory, "config.cfg");

        private readonly string _taggerThresholdDescription = "#Threshold for AutoTagger, must be a decimal value between 0-1.0 | 0 meaning 0% and 1 is 100%. Values will be clamped if necessary.";
        public string TaggerThresholdDescription { get => _taggerThresholdDescription.Replace("#", ""); }

        private readonly string _discardedFolderDescription = "#Folder for Discarded Images.";
        public string DiscardedFolderDescription { get => _discardedFolderDescription.Replace("#", ""); }

        private readonly string _selectedFolderDescription = "#Folder for Selected Images. This is also the folder the Resize page will use as Input.";
        public string SelectedFolderDescription { get => _selectedFolderDescription.Replace("#", ""); }

        private readonly string _backupFolderDescription = "#Folder for Backup.";
        public string BackupFolderDescription { get => _backupFolderDescription.Replace("#", ""); }

        private readonly string _resizedFolderDescription = "#Folder for Resized images output. This is also the folder the Generate page will use as Input.";
        public string ResizedFolderDescription { get => _resizedFolderDescription.Replace("#", ""); }

        private readonly string _combinedFolderDescription = "#Folder for Generated Tags and their corresponding Images. This is also the folder the Processor and Tag Editor pages will use as Input.";
        public string CombinedFolderDescription { get => _combinedFolderDescription.Replace("#", ""); }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ConfigsService()
        {
            Configurations = new Config();
        }

        /// <summary>
        /// Loads the configuration values from the configuration file.
        /// </summary>
        /// <remarks>
        /// This method first ensures that the configuration file exists by calling the <see cref="CreateConfigFileIfNotExist"/> method.
        /// It then reads all lines from the configuration file and filters out any lines starting with '#' (comments).
        /// Each remaining line represents a configuration option in the format "ConfigurationDescription=ConfigurationValue".
        /// The method parses each line and assigns the corresponding configuration value to the appropriate property in the <see cref="Configurations"/> object.
        /// If a folder path is specified in the configuration file, the method checks if the folder exists, and if not, assigns a default folder path.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task LoadConfigurations()
        {
            await CreateConfigFileIfNotExist();

            string[] file = await File.ReadAllLinesAsync(_cfgFilePath);

            IEnumerable<string> configLines = file.Where(x => !x.StartsWith('#'));

            foreach (string line in configLines)
            {
                if (line.StartsWith("TaggerThreshold"))
                {
                    Configurations.TaggerThreshold = GetFloatConfig(line, 0.35f);
                }
                else if (line.StartsWith("DiscardedFolder"))
                {
                    Configurations.DiscardedFolder = GetConfiguredFolder(line, "discarded-images-output");
                }
                else if (line.StartsWith("SortedFolder"))
                {
                    Configurations.SelectedFolder = GetConfiguredFolder(line, "sorted-images-output");
                }
                else if (line.StartsWith("BackupFolder"))
                {
                    Configurations.BackupFolder = GetConfiguredFolder(line, "images-backup");
                }
                else if (line.StartsWith("ResizedFolder"))
                {
                    Configurations.ResizedFolder = GetConfiguredFolder(line, "resized-images-output");
                }
                else if (line.StartsWith("CombinedFolder"))
                {
                    Configurations.CombinedOutputFolder = GetConfiguredFolder(line, "combined-images-output");
                }
            }
        }

        /// <summary>
        /// Saves the current configuration values to the configuration file.
        /// </summary>
        /// <remarks>
        /// This method writes the current configuration values, including the tagger threshold, discarded folder, selected folder,
        /// backup folder, resized folder, and combined folder, to the configuration file. Each configuration option is written as a
        /// separate line in the file, following the format "ConfigurationDescription=ConfigurationValue". The file is overwritten
        /// with the new configuration values.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveConfigurations()
        {
            List<string> configsList = new List<string>
            {
                _taggerThresholdDescription,
                $"TaggerThreshold={Configurations.TaggerThreshold}",
                "\n",
                _discardedFolderDescription,
                $"DiscardedFolder={Configurations.DiscardedFolder}",
                "\n",
                _selectedFolderDescription,
                $"SortedFolder={Configurations.SelectedFolder}",
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

            await File.WriteAllLinesAsync(_cfgFilePath, configsList);
        }

        /// <summary>
        /// Creates the configuration file if it doesn't already exist. Writes the default configuration values to the file.
        /// </summary>
        /// <remarks>
        /// This method checks if the configuration file exists. If it doesn't, it creates the file and writes the default
        /// configuration values to it. The default values include the descriptions and paths of various configuration options,
        /// such as the tagger threshold, discarded folder, selected folder, backup folder, resized folder, and combined folder.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateConfigFileIfNotExist()
        {
            if (!File.Exists(_cfgFilePath))
            {
                List<string> configsList = new List<string>
                {
                    _taggerThresholdDescription,
                    $"TaggerThreshold={Configurations.TaggerThreshold}",
                    "\n",
                    _discardedFolderDescription,
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
        /// <param name="defaultValue">The default value to be returned if parsing fails.</param>
        /// <returns>The parsed float value. If parsing fails, the default value is returned.</returns>
        private static float GetFloatConfig(string line, float defaultValue)
        {
            string[] splitLine = line.Split('=');
            if (float.TryParse(splitLine[splitLine.Length - 1], out float value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets the string configuration value from the specified configuration line.
        /// </summary>
        /// <param name="line">The configuration line to retrieve the value from.</param>
        /// <returns>The string configuration value extracted from the "key=value" format.</returns>
        private static string GetStringConfig(string line)
        {
            string[] splitLine = line.Split('=');
            return splitLine[splitLine.Length - 1];
        }

        /// <summary>
        /// Gets the configured folder path from the specified configuration line or uses the default folder path if the configured path does not exist.
        /// </summary>
        /// <param name="line">The configuration line to retrieve the folder path from.</param>
        /// <param name="defaultFolder">The default folder path to be used if the configured folder path does not exist.</param>
        /// <returns>The configured folder path or the default folder path if the configured path does not exist.</returns>
        private static string GetConfiguredFolder(string line, string defaultFolder)
        {
            string folder = GetStringConfig(line);
            if (Path.Exists(folder))
            {
                return folder;
            }
            else
            {
                return Path.Combine(AppContext.BaseDirectory, defaultFolder);
            }
        }

        public virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}