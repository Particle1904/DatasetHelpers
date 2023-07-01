using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;

using System.ComponentModel;

namespace SmartData.Lib.Services
{
    public class ConfigsService : IConfigsService, INotifyPropertyChanged
    {
        public Config Configurations { get; set; }

        private readonly string _cfgFilePath = Path.Combine(AppContext.BaseDirectory, "config.cfg");

        private string _taggerThresholdDescription = "#Threshold for AutoTagger, must be a decimal value between 0-1.0 | 0 meaning 0% and 1 is 100%. Values will be clamped if necessary.";
        public string TaggerThresholdDescription { get => _taggerThresholdDescription.Replace("#", ""); }

        private string _discardedFolderDescription = "#Folder for Discarded Images.";
        public string DiscardedFolderDescription { get => _discardedFolderDescription.Replace("#", ""); }

        private string _selectedFolderDescription = "#Folder for Selected Images. This is also the folder the Resize page will use as Input.";
        public string SelectedFolderDescription { get => _selectedFolderDescription.Replace("#", ""); }

        private string _backupFolderDescription = "#Folder for Backup.";
        public string BackupFolderDescription { get => _backupFolderDescription.Replace("#", ""); }

        private string _resizedFolderDescription = "#Folder for Resized images output. This is also the folder the Generate page will use as Input.";
        public string ResizedFolderDescription { get => _resizedFolderDescription.Replace("#", ""); }

        private string _combinedFolderDescription = "#Folder for Generated Tags and their corresponding Images. This is also the folder the Processor and Tag Editor pages will use as Input.";
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

            IEnumerable<string> configLines = file.Where(x => !x.StartsWith("#"));

            foreach (string line in configLines)
            {
                if (line.StartsWith("TaggerThreshold"))
                {
                    Configurations.TaggerThreshold = GetFloatConfig(line);
                }
                else if (line.StartsWith("DiscardedFolder"))
                {
                    string folder = GetStringConfig(line);
                    if (Path.Exists(folder))
                    {
                        Configurations.DiscardedFolder = folder;
                    }
                    else
                    {
                        Configurations.DiscardedFolder = Path.Combine(AppContext.BaseDirectory, "discarded-images-output");
                    }
                }
                else if (line.StartsWith("SortedFolder"))
                {
                    string folder = GetStringConfig(line);
                    if (Path.Exists(folder))
                    {
                        Configurations.SelectedFolder = folder;
                    }
                    else
                    {
                        Configurations.SelectedFolder = Path.Combine(AppContext.BaseDirectory, "sorted-images-output");
                    }
                }
                else if (line.StartsWith("BackupFolder"))
                {
                    string folder = GetStringConfig(line);
                    if (Path.Exists(folder))
                    {
                        Configurations.BackupFolder = folder;
                    }
                    else
                    {
                        Configurations.BackupFolder = Path.Combine(AppContext.BaseDirectory, "images-backup");
                    }
                }
                else if (line.StartsWith("ResizedFolder"))
                {
                    string folder = GetStringConfig(line);
                    if (Path.Exists(folder))
                    {
                        Configurations.ResizedFolder = folder;
                    }
                    else
                    {
                        Configurations.ResizedFolder = Path.Combine(AppContext.BaseDirectory, "resized-images-output");
                    }
                }
                else if (line.StartsWith("CombinedFolder"))
                {
                    string folder = GetStringConfig(line);
                    if (Path.Exists(folder))
                    {
                        Configurations.CombinedOutputFolder = folder;
                    }
                    else
                    {
                        Configurations.CombinedOutputFolder = Path.Combine(AppContext.BaseDirectory, "combined-images-output");
                    }
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

        public virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}