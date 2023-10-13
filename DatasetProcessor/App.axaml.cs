using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using DatasetProcessor.ViewModels;
using DatasetProcessor.Views;

using Microsoft.Extensions.DependencyInjection;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Services;

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
        var autoTagger = _servicesProvider.GetRequiredService<IAutoTaggerService>();
        var tagProcessor = _servicesProvider.GetRequiredService<ITagProcessorService>();
        var contentAwareCrop = _servicesProvider.GetRequiredService<IContentAwareCropService>();
        var inputHooks = _servicesProvider.GetRequiredService<IInputHooksService>();
        var promptGenerator = _servicesProvider.GetRequiredService<IPromptGeneratorService>();

        var logger = _servicesProvider.GetRequiredService<ILoggerService>();
        var configs = _servicesProvider.GetRequiredService<IConfigsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(fileManipulator, imageProcessor, autoTagger, tagProcessor, contentAwareCrop,
                    inputHooks, promptGenerator, logger, configs)
            };

            IClipboard clipboard = desktop.MainWindow.Clipboard;
            IStorageProvider storageProvider = desktop.MainWindow.StorageProvider;
            (desktop.MainWindow.DataContext as MainViewModel).InitializeClipboardAndStorageProvider(clipboard, storageProvider);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = new MainViewModel(fileManipulator, imageProcessor, autoTagger, tagProcessor, contentAwareCrop,
                    inputHooks, promptGenerator, logger, configs)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string _modelsPath = Path.Combine(AppContext.BaseDirectory, "models");
        string _WDOnnxFilename = "wdModel.onnx";
        string _csvFilename = "wdTags.csv";
        string _YoloV4OnnxFilename = "yolov4.onnx";

        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<IFileManipulatorService, FileManipulatorService>();
        services.AddSingleton<IImageProcessorService, ImageProcessorService>();
        services.AddSingleton<ITagProcessorService, TagProcessorService>();
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<IConfigsService, ConfigsService>();
        services.AddSingleton<IContentAwareCropService>(service =>
            new ContentAwareCropService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, _YoloV4OnnxFilename)

        ));
        services.AddSingleton<IAutoTaggerService>(service =>
            new AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, _WDOnnxFilename),
                Path.Combine(_modelsPath, _csvFilename)
        ));
        services.AddSingleton<IInputHooksService, InputHooksService>();
        services.AddSingleton<IPromptGeneratorService>(service => new
            PromptGeneratorService(service.GetRequiredService<ITagProcessorService>(),
                service.GetRequiredService<IFileManipulatorService>()));
    }
}