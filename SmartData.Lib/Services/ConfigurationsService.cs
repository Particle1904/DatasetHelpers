using Models.Configurations;

using Services;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Models.Configurations;

using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SmartData.Lib.Services
{
    /// <summary>
    /// Service for managing configurations and handling configuration file operations.
    /// Implements the <see cref="IConfigsService"/> and <see cref="INotifyPropertyChanged"/> interfaces.
    /// </summary>
    public class ConfigurationsService : IConfigsService, INotifyPropertyChanged
    {
        public Config Configurations { get; private set; }

        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };

        private readonly string _configsFilePath = Path.Combine(AppContext.BaseDirectory, "config.json");

        /// <summary>
        /// Event raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationsService"/> class.
        /// Creates a new <see cref="Config"/> object and asynchronously loads configuration values from the configuration file.
        /// </summary>
        public ConfigurationsService()
        {
            Configurations = new Config()
            {
                GalleryConfigs = new GalleryConfigs(),
                SortImagesConfigs = new SortImagesConfigs(),
                TextRemoverConfigs = new TextRemoverConfigs(),
                ContentAwareCropConfigs = new ContentAwareCropConfigs(),
                ManualCropConfigs = new ManualCropConfigs(),
                ResizeImagesConfigs = new ResizeImagesConfigs(),
                UpscaleImagesConfigs = new UpscaleImagesConfigs(),
                GenerateTagsConfigs = new GenerateTagsConfigs(),
                GeminiCaptionConfigs = new GeminiCaptionConfigs()
                {
                    Prompt = GeminiService.BASE_PROMPT,
                    SystemInstructions = GeminiService.CreateBaseSystemInstruction()
                },
                Florence2CaptionConfigs = new Florence2CaptionConfigs(),
                ProcessCaptionsConfigs = new ProcessCaptionsConfigs(),
                ProcessTagsConfigs = new ProcessTagsConfigs(),
                TagEditorConfigs = new TagEditorConfigs(),
                ExtractSubsetConfigs = new ExtractSubsetConfigs(),
                PromptGeneratorConfigs = new PromptGeneratorConfigs(),
                MetadataViewerConfigs = new MetadataViewerConfigs()
            };
            Task.Run(() => LoadConfigurationsAsync());
        }

        /// <summary>
        /// Asynchronously loads configuration values from the configuration file.
        /// </summary>
        /// <remarks>
        /// This method first ensures that the configuration file exists by calling the <see cref="CreateConfigFileIfNotExistAsync"/> method.
        /// It then reads all lines from the configuration file and filters out any lines starting with '#' (comments).
        /// Each remaining line represents a configuration option in the format "ConfigurationDescription=ConfigurationValue".
        /// The method parses each line and assigns the corresponding configuration value to the appropriate property in the <see cref="Configurations"/> object.
        /// If a folder path is specified in the configuration file, the method checks if the folder exists, and if not, assigns a default folder path.
        /// </remarks>
        public async Task LoadConfigurationsAsync()
        {
            await CreateConfigFileIfNotExistAsync();
            string file = await File.ReadAllTextAsync(_configsFilePath);
            Configurations = JsonSerializer.Deserialize<Config>(file);

            foreach (PropertyInfo property in Configurations.GetType().GetProperties())
            {
                if (property.GetValue(Configurations) == null)
                {
                    if (property.PropertyType.GetConstructor(Type.EmptyTypes) != null)
                    {
                        object defaultInstance = Activator.CreateInstance(property.PropertyType);
                        property.SetValue(Configurations, defaultInstance);
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously saves the current configuration values to the configuration file.
        /// </summary>
        /// <remarks>
        /// This method writes the current configuration values, including the tagger threshold, discarded folder, selected folder,
        /// backup folder, resized folder, and combined folder, to the configuration file. Each configuration option is written as a
        /// separate line in the file, following the format "ConfigurationDescription=ConfigurationValue". The file is overwritten
        /// with the new configuration values.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveConfigurationsAsync()
        {
            string json = JsonSerializer.Serialize<Config>(Configurations, _jsonOptions);
            await File.WriteAllTextAsync(_configsFilePath, json, Encoding.UTF8);
        }

        /// <summary>
        /// Asynchronously creates the configuration file if it doesn't already exist and writes the default configuration values to the file.
        /// </summary>
        /// <remarks>
        /// This method checks if the configuration file exists. If it doesn't, it creates the file and writes the default
        /// configuration values to it. The default values include the descriptions and paths of various configuration options,
        /// such as the tagger threshold, discarded folder, selected folder, backup folder, resized folder, and combined folder.
        /// </remarks>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreateConfigFileIfNotExistAsync()
        {
            if (!File.Exists(_configsFilePath))
            {
                string json = JsonSerializer.Serialize<Config>(Configurations, _jsonOptions);
                await File.WriteAllTextAsync(_configsFilePath, json, Encoding.UTF8);
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event for a specific property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed (optional).</param>
        public virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}