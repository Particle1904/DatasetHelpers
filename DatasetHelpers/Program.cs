using DatasetHelpers.Services;

using System.Diagnostics;

namespace DatasetHelpers
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ConfigsService _configs = new ConfigsService();
            await _configs.LoadConfigurations();

            Stopwatch _stopWatch = new Stopwatch();
            _stopWatch.Start();
            string _mainPath = Environment.CurrentDirectory;

            string _imagesFolder = "ImagesInput";
            string _imagesOutput = "ImagesOutput";

            string _imagesDiscardedOutput = Path.Combine(_imagesOutput, "Discarded");
            string _imagesSelectedOutput = Path.Combine(_imagesOutput, "Selected");
            string _imagesResizedOutput = Path.Combine(_imagesOutput, "Resized");

            string _modelPath = Path.Combine("wdTaggerModel", "model.onnx");
            string _tagsPath = Path.Combine("wdTaggerModel", "tags.csv");

            string _textInput = $"TextInput";
            string _textOutput = $"TextOutput";

            string _combinedOutput = $"CombinedOutput";

            FileManipulatorService _fileService = new FileManipulatorService();
            AutoTaggerService _taggerService = new AutoTaggerService(_modelPath, _tagsPath);
            _taggerService.LoadConfigs(_configs.Configurations);
            TagHelper _tagHelper = new TagHelper(_textInput, _combinedOutput);

            _fileService.CreateFolderIfNotExist(_imagesFolder);
            _fileService.CreateFolderIfNotExist(_imagesOutput);
            _fileService.CreateFolderIfNotExist(_imagesDiscardedOutput);
            _fileService.CreateFolderIfNotExist(_imagesSelectedOutput);
            _fileService.CreateFolderIfNotExist(_imagesResizedOutput);
            _fileService.CreateFolderIfNotExist(_combinedOutput);

            _fileService.SortImages(_imagesFolder, _imagesDiscardedOutput, _imagesSelectedOutput);
            _fileService.ResizeImages(_imagesSelectedOutput, _imagesResizedOutput);

            string[] treatedDataset = Directory.GetFiles(_imagesResizedOutput);

            //Console.WriteLine($"Starting process of generating tags using WD Tagger Model!");
            //for (int i = 0; i < treatedDataset.Length; i++)
            //{
            //    Console.WriteLine($"Predicting for file {i + 1}...");

            //    List<string> prediction = _taggerService.GetOrderedByScoreListOfTags(treatedDataset[i]);
            //    string tags = _tagHelper.ProcessListOfTags(prediction);
            //    File.WriteAllText($"{_combinedOutput}/{i + 1}.txt", tags);
            //    File.Move(treatedDataset[i], $"{_combinedOutput}/{i + 1}.png");

            //    Console.WriteLine($"Finished processing file {i + 1}! File moved to the appropriate folder and tags file created.");
            //}
            //Console.WriteLine($"Finished process of tag generation and processing.");

            _tagHelper.CalculateListOfMostUsedTags();

            _stopWatch.Stop();
            Console.WriteLine($"Time taken {_stopWatch.Elapsed.Minutes}:{_stopWatch.Elapsed.Seconds} minutes.");
        }
    }
}