// Ignore Spelling: App

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
            string _WDOnnxFilename = "wdModel.onnx";
            string _csvFilename = "wdTags.csv";
            string _YoloV4OnnxFilename = "yolov4.onnx";

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
            builder.Services.AddSingleton<ILoggerService, LoggerService>();
            builder.Services.AddSingleton<IConfigsService, ConfigsService>();
            builder.Services.AddSingleton<IContentAwareCropService>(service =>
                new ContentAwareCropService(service.GetRequiredService<IImageProcessorService>(),
                    Path.Combine(_modelsPath, _YoloV4OnnxFilename)

            ));
            builder.Services.AddSingleton<IAutoTaggerService>(service =>
                new AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                    service.GetRequiredService<ITagProcessorService>(),
                    Path.Combine(_modelsPath, _WDOnnxFilename),
                    Path.Combine(_modelsPath, _csvFilename)
            ));

            return builder.Build();
        }
    }
}