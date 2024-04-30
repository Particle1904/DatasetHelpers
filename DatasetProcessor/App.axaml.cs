using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using DatasetProcessor.ViewModels;
using DatasetProcessor.Views;

using Microsoft.Extensions.DependencyInjection;

using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services;
using SmartData.Lib.Services.MachineLearning;

using System;
using System.IO;

namespace DatasetProcessor;

public partial class App : Application
{
    private IServiceProvider _servicesProvider;

    public App()
    {
        ServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _servicesProvider = services.BuildServiceProvider(new ServiceProviderOptions()
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var fileManipulator = _servicesProvider.GetRequiredService<IFileManipulatorService>();
        var imageProcessor = _servicesProvider.GetRequiredService<IImageProcessorService>();
        var wDAutoTagger = _servicesProvider.GetRequiredService<WDAutoTaggerService>();
        var wDv3AutoTagger = _servicesProvider.GetRequiredService<WDV3AutoTaggerService>();
        var joyTagAutoTagger = _servicesProvider.GetRequiredService<JoyTagAutoTaggerService>();
        var e621AutoTagger = _servicesProvider.GetRequiredService<E621AutoTaggerService>();
        var tagProcessor = _servicesProvider.GetRequiredService<ITagProcessorService>();
        var contentAwareCrop = _servicesProvider.GetRequiredService<IContentAwareCropService>();
        var inputHooks = _servicesProvider.GetRequiredService<IInputHooksService>();
        var promptGenerator = _servicesProvider.GetRequiredService<IPromptGeneratorService>();
        var clipTokenizer = _servicesProvider.GetRequiredService<ICLIPTokenizerService>();

        var logger = _servicesProvider.GetRequiredService<ILoggerService>();
        var configs = _servicesProvider.GetRequiredService<IConfigsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(fileManipulator, imageProcessor, wDAutoTagger, wDv3AutoTagger, joyTagAutoTagger,
                    e621AutoTagger, tagProcessor, contentAwareCrop, inputHooks, promptGenerator, clipTokenizer, logger, configs)
            };

            IClipboard clipboard = desktop.MainWindow.Clipboard;
            IStorageProvider storageProvider = desktop.MainWindow.StorageProvider;
            (desktop.MainWindow.DataContext as MainViewModel).InitializeClipboardAndStorageProvider(clipboard, storageProvider);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = new MainViewModel(fileManipulator, imageProcessor, wDAutoTagger, wDv3AutoTagger, joyTagAutoTagger,
                    e621AutoTagger, tagProcessor, contentAwareCrop, inputHooks, promptGenerator, clipTokenizer, logger, configs)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string _modelsPath = Path.Combine(AppContext.BaseDirectory, "models");

        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<IFileManipulatorService, FileManipulatorService>();
        services.AddSingleton<IImageProcessorService, ImageProcessorService>();
        services.AddSingleton<ITagProcessorService, TagProcessorService>();
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<IConfigsService, ConfigsService>();
        services.AddSingleton<IContentAwareCropService>(service =>
            new ContentAwareCropService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, FileNames.YoloV4OnnxFileName)
        ));
        services.AddSingleton<WDAutoTaggerService>(service =>
            new WDAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, FileNames.WDOnnxFileName),
                Path.Combine(_modelsPath, FileNames.WDCsvFileName)
        ));
        services.AddSingleton<WDV3AutoTaggerService>(service =>
            new WDV3AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, FileNames.WDV3OnnxFileName),
                Path.Combine(_modelsPath, FileNames.WDV3CsvFileName)
        ));
        services.AddSingleton<E621AutoTaggerService>(service =>
            new E621AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, FileNames.E621OnnxFileName),
                Path.Combine(_modelsPath, FileNames.E621CsvFileName)
        ));
        services.AddSingleton<JoyTagAutoTaggerService>(service =>
            new JoyTagAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, FileNames.JoyTagOnnxFileName),
                Path.Combine(_modelsPath, FileNames.JoyTagCsvFileName)
        ));
        services.AddSingleton<ICLIPTokenizerService>(service =>
            new CLIPTokenizerService(Path.Combine(_modelsPath, FileNames.CLIPTokenixerOnnxFileName)
        ));
        services.AddSingleton<IInputHooksService, InputHooksService>();
        services.AddSingleton<IPromptGeneratorService>(service => new
            PromptGeneratorService(service.GetRequiredService<ITagProcessorService>(),
                service.GetRequiredService<IFileManipulatorService>()));
    }
}