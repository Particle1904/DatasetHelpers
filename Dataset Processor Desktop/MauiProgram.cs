using CommunityToolkit.Maui;

using Dataset_Processor_Desktop.src.Interfaces;
using Dataset_Processor_Desktop.src.Services;

namespace Dataset_Processor_Desktop
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
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

            return builder.Build();
        }
    }
}