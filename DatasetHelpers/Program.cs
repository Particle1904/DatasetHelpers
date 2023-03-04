using DatasetHelpers.Services;

namespace DatasetHelpers
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            FileManipulatorService fileService = new FileManipulatorService();

            string _mainPath = Environment.CurrentDirectory;

            string _imagesFolder = "ImagesInput";
            string _imagesOutput = "ImagesOutput";

            string _imagesDiscardedOutput = $"{_imagesOutput}/Discarded";
            string _imagesSelectedOutput = $"{_imagesOutput}/Selected";
            string _imagesResizedOutput = $"{_imagesOutput}/Resized";

            string _modelPath = $"{_mainPath}/wdTaggerModel/model.onnx";
            string _tagsPath = $"{_mainPath}/wdTaggerModel/tags.csv";

            AutoTaggerService _taggerService = new AutoTaggerService(_modelPath, _tagsPath);

            fileService.CreateFolder(_imagesFolder);
            fileService.CreateFolder(_imagesOutput);
            fileService.CreateFolder(_imagesDiscardedOutput);
            fileService.CreateFolder(_imagesSelectedOutput);
            fileService.CreateFolder(_imagesResizedOutput);

            fileService.SortImages(_imagesFolder, _imagesDiscardedOutput, _imagesSelectedOutput);
            fileService.ResizeImages(_imagesSelectedOutput, _imagesResizedOutput);

            var predictionsOrdered = _taggerService.GenerateTags("I:\\dev\\DatasetHelpers\\DatasetHelpers\\bin\\Debug\\net6.0\\ImagesOutput\\Resized\\1.png");
            foreach (var prediction in predictionsOrdered)
            {
                Console.WriteLine(prediction);
            }
        }
    }
}