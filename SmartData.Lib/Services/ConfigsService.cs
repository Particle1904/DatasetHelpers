using SmartData.Lib.Models;

namespace SmartData.Lib.Services
{
    public class ConfigsService
    {
        public Config Configurations { get; private set; }
        private readonly string _cfgFilePath = Path.Combine(Environment.CurrentDirectory, "config.cfg");

        public ConfigsService()
        {
            Configurations = new Config();
        }

        public async Task LoadConfigurations()
        {
            CreateConfigFileIfNotExist();

            string[] file = await File.ReadAllLinesAsync(_cfgFilePath);

            foreach (string line in file)
            {
                if (line.StartsWith("TaggerThreshold"))
                {
                    Configurations.TaggerThreshold = GetFloatConfig(line);
                }
            }
        }

        public float GetFloatConfig(string line)
        {
            string[] splitLine = line.Split("=");
            float.TryParse(splitLine.LastOrDefault(), out float value);
            if (value == 0)
            {
                Console.WriteLine($"Unable to load {splitLine.FirstOrDefault()} from .cfg file!");
            }
            else
            {
                Console.WriteLine($"Loaded config {splitLine.FirstOrDefault()} from .cfg file! Value: {splitLine.LastOrDefault()}");
            }

            return value;
        }

        public void CreateConfigFileIfNotExist()
        {
            List<string> configsList = new List<string>();
            configsList.Add("#Threshold for AutoTagger, must be a decimal value between 0-1.0 | 0 meaning 0% and 1 is 100%.");
            configsList.Add("TaggerThreshold=0.35");


            if (!File.Exists(_cfgFilePath))
            {
                File.AppendAllLines(_cfgFilePath, configsList);
            }
        }
    }
}
