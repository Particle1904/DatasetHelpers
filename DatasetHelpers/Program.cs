using DatasetHelpers.Services;

namespace DatasetHelpers
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FileManipulatorService fileService = new FileManipulatorService();

            string _mainPath = Environment.CurrentDirectory;

            string _imagesFolder = "ImagesInput";
            string _imagesOutput = "ImagesOutput";

            string _imagesDiscardedOutput = $"{_imagesOutput}/Discarded";
            string _imagesSelectedOutput = $"{_imagesOutput}/Selected";

            string _imagesResizedOutput = $"{_imagesOutput}/Resized";

            fileService.CreateFolder(_imagesFolder);
            fileService.CreateFolder(_imagesOutput);
            fileService.CreateFolder(_imagesDiscardedOutput);
            fileService.CreateFolder(_imagesSelectedOutput);
            fileService.CreateFolder(_imagesResizedOutput);

            fileService.SortImages(_imagesFolder, _imagesDiscardedOutput, _imagesSelectedOutput);
            fileService.ResizeImages(_imagesSelectedOutput, _imagesResizedOutput);
        }
    }
}