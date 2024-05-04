using Microsoft.ML.OnnxRuntime;

using SmartData.Lib.Interfaces;
using SmartData.Lib.Interfaces.MachineLearning;
using SmartData.Lib.Services;
using SmartData.Lib.Services.MachineLearning;

using System.Reflection;
using System.Runtime.InteropServices;

namespace ConsoleTestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            NativeLibrary.SetDllImportResolver(Assembly.Load("Microsoft.ML.OnnxRuntime"), OnnxRuntimeImportResolver);

            IImageProcessorService imageProcessor = new ImageProcessorService();
            IUpscalerService upscalerService = new UpscalerService(imageProcessor, @"Downloads\RealSR_BSRGAN_DFO_s64w8_SwinIR-M_x4_GAN.onnx");

            try
            {
                await upscalerService.UpscaleImageAsync(@"Downloads\cat2.png",
                    @"Downloads\cat_2.png");
            }
            catch (OnnxRuntimeException ex)
            {
            }
        }

        public static IntPtr OnnxRuntimeImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != "onnxruntime")
            {
                return IntPtr.Zero;
            }

            string location = Path.Combine(Environment.CurrentDirectory, "runtimes");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                location = Path.Combine(location, "win-x64");
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
}
