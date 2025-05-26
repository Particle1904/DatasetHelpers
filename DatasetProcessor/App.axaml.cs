using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using DatasetProcessor.ViewModels;
using DatasetProcessor.Views;

using Interfaces;
using Interfaces.MachineLearning;
using Interfaces.MachineLearning.SAM2;

using Microsoft.Extensions.DependencyInjection;

using Models.ModelManager;

using Services;
using Services.MachineLearning;

using SmartData.Lib.Enums;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services;
using SmartData.Lib.Services.MachineLearning;
using SmartData.Lib.Services.MachineLearning.SAM2;

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

        var fileManager = _servicesProvider.GetRequiredService<IFileManagerService>();
        var modelManager = _servicesProvider.GetRequiredService<IModelManagerService>();
        var imageProcessor = _servicesProvider.GetRequiredService<IImageProcessorService>();
        var wDautoTagger = _servicesProvider.GetRequiredService<WDAutoTaggerService>();
        var wDv3autoTagger = _servicesProvider.GetRequiredService<WDV3AutoTaggerService>();
        var wDv3largeAutoTagger = _servicesProvider.GetRequiredService<WDV3LargeAutoTaggerService>();
        var joyTagautoTagger = _servicesProvider.GetRequiredService<JoyTagAutoTaggerService>();
        var e621autoTagger = _servicesProvider.GetRequiredService<E621AutoTaggerService>();
        var tagProcessor = _servicesProvider.GetRequiredService<ITagProcessorService>();
        var contentAwareCrop = _servicesProvider.GetRequiredService<IContentAwareCropService>();
        var inputHooks = _servicesProvider.GetRequiredService<IInputHooksService>();
        var promptGenerator = _servicesProvider.GetRequiredService<IPromptGeneratorService>();
        var clipTokenizer = _servicesProvider.GetRequiredService<ICLIPTokenizerService>();
        var upscaler = _servicesProvider.GetRequiredService<IUpscalerService>();
        var inpaint = _servicesProvider.GetRequiredService<IInpaintService>();
        var gemini = _servicesProvider.GetRequiredService<IGeminiService>();
        var florence2 = _servicesProvider.GetRequiredService<IFlorence2Service>();
        var textRemover = _servicesProvider.GetRequiredService<ITextRemoverService>();
        var python = _servicesProvider.GetRequiredService<IPythonService>();

        var logger = _servicesProvider.GetRequiredService<ILoggerService>();
        var configs = _servicesProvider.GetRequiredService<IConfigsService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(fileManager, modelManager, imageProcessor, wDautoTagger, wDv3autoTagger, wDv3largeAutoTagger, joyTagautoTagger,
                    e621autoTagger, tagProcessor, contentAwareCrop, inputHooks, promptGenerator, clipTokenizer, upscaler,
                    inpaint, gemini, florence2, textRemover, python, logger, configs)
            };

            IClipboard clipboard = desktop.MainWindow.Clipboard;
            IStorageProvider storageProvider = desktop.MainWindow.StorageProvider;
            (desktop.MainWindow.DataContext as MainViewModel).InitializeClipboardAndStorageProvider(clipboard, storageProvider);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView()
            {
                DataContext = new MainViewModel(fileManager, modelManager, imageProcessor, wDautoTagger, wDv3autoTagger, wDv3largeAutoTagger, joyTagautoTagger,
                    e621autoTagger, tagProcessor, contentAwareCrop, inputHooks, promptGenerator, clipTokenizer, upscaler,
                    inpaint, gemini, florence2, textRemover, python, logger, configs)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string _modelsPath = Path.Combine(AppContext.BaseDirectory, "models");

        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<IConfigsService, ConfigurationsService>();
        services.AddSingleton<IFileManagerService, FileManagerService>();
        services.AddSingleton<IModelManagerService, ModelManagerService>();
        services.AddSingleton<IImageProcessorService, ImageProcessorService>();
        services.AddSingleton<ITagProcessorService, TagProcessorService>();
        services.AddSingleton<IPythonService, PythonService>();
        services.AddSingleton<IInputHooksService, InputHooksService>();

        services.AddSingleton<IContentAwareCropService>(service =>
            new ContentAwareCropService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.Yolov4].Model.Filename)
        ));

        services.AddSingleton<WDAutoTaggerService>(service =>
            new WDAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.WD14v2].Model.Filename),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.WD14v2].Csv.Filename)
        ));

        services.AddSingleton<WDV3AutoTaggerService>(service =>
            new WDV3AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.WDv3].Model.Filename),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.WDv3].Csv.Filename)
        ));

        services.AddSingleton<WDV3LargeAutoTaggerService>(service =>
            new WDV3LargeAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.WDv3Large].Model.Filename),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.WDv3Large].Csv.Filename)
        ));

        services.AddSingleton<E621AutoTaggerService>(service =>
            new E621AutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.Z3DE621].Model.Filename),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.Z3DE621].Csv.Filename)
        ));

        services.AddSingleton<JoyTagAutoTaggerService>(service =>
            new JoyTagAutoTaggerService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<ITagProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.JoyTag].Model.Filename),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.JoyTag].Csv.Filename)
        ));

        services.AddSingleton<ICLIPTokenizerService>(service =>
            new CLIPTokenizerService(Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.CLIPTokenizer].Model.Filename)
        ));

        services.AddSingleton<IPromptGeneratorService>(service => new
            PromptGeneratorService(service.GetRequiredService<ITagProcessorService>(),
                service.GetRequiredService<IFileManagerService>()));

        services.AddSingleton<IUpscalerService>(service =>
            new UpscalerService(service.GetRequiredService<IImageProcessorService>(), string.Empty));

        services.AddSingleton<IInpaintService>(service =>
            new InpaintService(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.LaMa].Model.Filename)
        ));

        services.AddSingleton<IGeminiService>(service =>
            new GeminiService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<IFileManagerService>(),
                service.GetRequiredService<IPythonService>()
        ));

        services.AddSingleton<IFlorence2Service>(service => new Florence2Service(service.GetRequiredService<IFileManagerService>(),
            _modelsPath));

        services.AddSingleton<ISAM2Service>(service =>
            new SAM2Service(service.GetRequiredService<IImageProcessorService>(),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.SAM2Encoder].Model.Filename),
                Path.Combine(_modelsPath, ModelRegistry.RequiredFiles[AvailableModels.SAM2Decoder].Model.Filename)
        ));

        services.AddSingleton<ITextRemoverService>(service =>
            new TextRemoverService(service.GetRequiredService<IImageProcessorService>(),
                service.GetRequiredService<IFlorence2Service>(),
                service.GetRequiredService<ISAM2Service>(),
                service.GetRequiredService<IInpaintService>()
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