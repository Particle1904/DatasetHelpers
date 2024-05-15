using Avalonia;
using Avalonia.Svg.Skia;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DatasetProcessor.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.

    [STAThread]
    public static void Main(string[] args)
    {
        AppBuilder builder = BuildAvaloniaApp();

        builder.StartWithClassicDesktopLifetime(args);
    }
    // Avalonia configuration, don't remove; also used by visual designer.

    public static AppBuilder BuildAvaloniaApp()
    {
        NativeLibrary.SetDllImportResolver(Assembly.Load("Microsoft.ML.OnnxRuntime"), OnnxRuntimeImportResolver);
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
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
    private static IntPtr OnnxRuntimeImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
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