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
using System.Reflection;
using System.Runtime.InteropServices;

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

        NativeLibrary.SetDllImportResolver(Assembly.Load("Microsoft.ML.OnnxRuntime"), OnnxRuntimeImportResolver);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        var fileManipulator = _servicesProvider.GetRequiredService<IFileManipulatorService>();
        var imageProcessor = _servicesProvider.GetRequiredService<IImageProcessorService>();
        var wDautoTagger = _servicesProvider.GetRequiredService<WDAutoTaggerService>();
        var wDv3autoTagger = _servicesProvider.GetRequiredService<WDV3AutoTaggerService>();
        var joyTagautoTagger = _servicesProvider.GetRequiredService<JoyTagAutoTaggerService>();
        var e621autoTagger = _servicesProvider.GetRequiredService<E621AutoTaggerService>();
        var tagProcessor = _servicesProvider.GetRequiredService<ITagProcessorService>();
        var contentAwareCrop = _servicesProvider.GetRequiredService<IContentAwareCropService>();
        var inputHooks = _servicesProvider.GetRequiredService<IInputHooksService>();
        var promptGenerator = _servicesProvider.GetRequiredService<IPromptGeneratorService>();
        var clipTokenizer = _servicesProvider.GetRequiredService<ICLIPTokenizerService>();
        var upscaler = _servicesProvider.GetRequiredService<IUpscalerService>();
        var inpaint = _servicesProvider.GetRequiredService<IInpaintService>();

        var logger = _servicesProvider.GetRequiredService<ILoggerService>();
        var configs = _servicesProvider.GetRequiredService<IConfigsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(fileManipulator, imageProcessor, wDautoTagger, wDv3autoTagger, joyTagautoTagger,
                    e621autoTagger, tagProcessor, contentAwareCrop, inputHooks, promptGenerator, clipTokenizer, upscaler,
                    inpaint, logger, configs)
            };

            IClipboard clipboard = desktop.MainWindow.Clipboard;
            IStorageProvider storageProvider = desktop.MainWindow.StorageProvider;
            (desktop.MainWindow.DataContext as MainViewModel).InitializeClipboardAndStorageProvider(clipboard, storageProvider);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = new MainViewModel(fileManipulator, imageProcessor, wDautoTagger, wDv3autoTagger, joyTagautoTagger,
                    e621autoTagger, tagProcessor, contentAwareCrop, inputHooks, promptGenerator, clipTokenizer, upscaler,
                    inpaint, logger, configs)
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
        services.AddSingleton<IConfigsService, ConfigurationsService>();
        services.AddSingleton<IContentAwareCropService>(service =>
            new ContentAwareCropService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, Filenames.YoloV4OnnxFilename)
        ));
        services.AddSingleton<WDAutoTaggerService>(service =>
            new WDAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, Filenames.WDOnnxFilename),
                Path.Combine(_modelsPath, Filenames.WDCsvFilename)
        ));
        services.AddSingleton<WDV3AutoTaggerService>(service =>
            new WDV3AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, Filenames.WDV3OnnxFilename),
                Path.Combine(_modelsPath, Filenames.WDV3CsvFilename)
        ));
        services.AddSingleton<E621AutoTaggerService>(service =>
            new E621AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, Filenames.E621OnnxFilename),
                Path.Combine(_modelsPath, Filenames.E621CsvFilename)
        ));
        services.AddSingleton<JoyTagAutoTaggerService>(service =>
            new JoyTagAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, Filenames.JoyTagOnnxFilename),
                Path.Combine(_modelsPath, Filenames.JoyTagCsvFilename)
        ));
        services.AddSingleton<ICLIPTokenizerService>(service =>
            new CLIPTokenizerService(Path.Combine(_modelsPath, Filenames.CLIPTokenixerFilename)
        ));
        services.AddSingleton<IInputHooksService, InputHooksService>();
        services.AddSingleton<IPromptGeneratorService>(service => new
            PromptGeneratorService(service.GetRequiredService<ITagProcessorService>(),
                service.GetRequiredService<IFileManipulatorService>()));
        services.AddSingleton<IUpscalerService>(service =>
            new UpscalerService(service.GetRequiredService<IImageProcessorService>(), string.Empty));
        services.AddSingleton<IInpaintService>(service =>
            new InpaintService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, Filenames.LaMaFilename)
        ));
    }

    /// <summary>
    /// Resolves the OnnxRuntime library import based on the current platform and process architecture.
    /// </summary>
    /// <param name="libraryName">The name of the library to resolve. This should be "onnxruntime".</param>
    /// <param name="assembly">The assembly requesting the import. This parameter is not used in this method.</param>
    /// <param name="searchPath">The search path for the library. This parameter is not used in this method.</param>
    /// <returns>
    /// A handle to the loaded library if the libraryName is "onnxruntime" and the library is successfully loaded;
    /// otherwise, returns <see cref="IntPtr.Zero"/>.
    /// </returns>
    /// <remarks>
    /// The method determines the current platform (Windows, Linux, or macOS) and process architecture (x86 or x64),
    /// and constructs the appropriate path to the OnnxRuntime library. It then attempts to load the library from this path.
    /// </remarks>
    private IntPtr OnnxRuntimeImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "onnxruntime")
        {
            return IntPtr.Zero;
        }

        string location = Path.Combine(Environment.CurrentDirectory, "runtimes");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string processArchitecture = string.Empty;
            if (Environment.Is64BitProcess)
            {
                processArchitecture = "x64";
            }
            else
            {
                processArchitecture = "x86";
            }
            location = Path.Combine(location, $"win-{processArchitecture}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            location = Path.Combine(location, "linux-x64");
        }
        else
        {
            location = Path.Combine(location, "osx-x64");
        }

        IntPtr libHandle = IntPtr.Zero;
        NativeLibrary.TryLoad(Path.Combine(location, "native", "onnxruntime.dll"), out libHandle);

        return libHandle;
    }
}