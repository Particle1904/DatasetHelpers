using CommunityToolkit.Maui;

using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Services;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Services;

namespace Dataset_Processor_Desktop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            string _modelsPath = Path.Combine(AppContext.BaseDirectory, "models");
            string _onnxFilename = "wdModel.onnx";
            string _csvFilename = "wdTags.csv";

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
            builder.Services.AddSingleton<IFileManipulatorService, FileManipulatorService>();
            builder.Services.AddSingleton<IImageProcessorService, ImageProcessorService>();
            builder.Services.AddSingleton<ITagProcessorService, TagProcessor>();
            builder.Services.AddSingleton<IAutoTaggerService>(service =>
            new AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, _onnxFilename),
                Path.Combine(_modelsPath, _csvFilename)
            ));

            return builder.Build();
        }
    }
}