using Python.Runtime;

using SmartData.Lib.Exceptions;
using SmartData.Lib.Interfaces;

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Services
{
    public class PythonService : IPythonService, IDisposable
    {
        public bool IsInitialized
        {
            get => PythonEngine.IsInitialized;
        }

        public PythonService()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonService"/> class.
        /// Automatically initializes the Python runtime.
        /// </summary>
        public void InitializePython()
        {
            if (PythonEngine.IsInitialized)
            {
                return;
            }

            try
            {
                string detectedDll = AutoDetectPythonPath();

                if (!string.IsNullOrEmpty(detectedDll))
                {
                    Runtime.PythonDLL = detectedDll;
                }

                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();
            }
            catch (Exception)
            {
                throw new PythonNotFoundException();
            }
        }

        /// <summary>
        /// Disposes of the service and shuts down the Python runtime if it is initialized.
        /// </summary>
        public void Dispose()
        {
            if (IsInitialized)
            {
                PythonEngine.Shutdown();
            }
        }

        /// <summary>
        /// Downloads and installs required Python packages using pip.
        /// </summary>
        /// <exception cref="Exception">Thrown if the package installation fails.</exception>
        public void DownloadPythonPackages()
        {
            try
            {
                InstallPackage("google.generativeai");
                InstallPackage("Pillow");
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to download Python packages.", exception);
            }
        }

        /// <summary>
        /// Generates content using the configured Python model.
        /// </summary>
        /// <param name="base64Image">The base64-encoded image data to be used as input.</param>
        /// <param name="prompt">The text prompt to guide content generation.</param>
        /// <param name="geminiApiKey">The API key for authentication with the Generative AI service.</param>
        /// <param name="systemInstructions">System-level instructions for the model configuration.</param>
        /// <returns>A string containing the generated content.</returns>
        /// <exception cref="Exception">Thrown if content generation fails.</exception>
        public async Task<string> GenerateContent(string base64Image, string prompt, string geminiApiKey, string systemInstructions, string modelName = "gemini-2.0-flash-lite")
        {
            return await Task.Run(() =>
            {
                Py.GILState gilState = Py.GIL();

                try
                {
                    using (dynamic scope = Py.CreateScope())
                    {
                        scope.Set("api_key", geminiApiKey);
                        scope.Set("system_instructions", systemInstructions);
                        scope.Set("image_base64", base64Image);
                        scope.Set("text_prompt", prompt);
                        scope.Set("model_name", modelName);

                        string setupModelScript = @"
import google.generativeai as genai
from google.generativeai.types import HarmCategory, HarmBlockThreshold

def setup_model(system_instruction: str):
    genai.configure(api_key=api_key)
    
    safety_settings = {
        HarmCategory.HARM_CATEGORY_HARASSMENT: HarmBlockThreshold.BLOCK_NONE,
        HarmCategory.HARM_CATEGORY_HATE_SPEECH: HarmBlockThreshold.BLOCK_NONE,
        HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT: HarmBlockThreshold.BLOCK_NONE,
        HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT: HarmBlockThreshold.BLOCK_NONE
    }
    
    return genai.GenerativeModel(
        model_name=model_name,
        safety_settings=safety_settings,
        system_instruction=system_instruction
    )

model = setup_model(system_instructions)
";
                        scope.Exec(setupModelScript);

                        string makeRequestScript = @"
response = model.generate_content([{'mime_type': 'image/png', 'data': image_base64}, text_prompt])
response_text = response.text
";
                        scope.Exec(makeRequestScript);

                        return scope.Get("response_text").ToString();
                    }
                }
                catch (Exception exception)
                {
                    if (exception.Message.Contains("API_KEY_INVALID"))
                    {
                        return "Invalid API Key!";
                    }
                    else if (exception.Message.Contains("429"))
                    {
                        return "CHECK QUOTA";
                    }
                    else
                    {
                        return "BLOCKED CONTENT";
                    }
                }
                finally
                {
                    gilState.Dispose();
                }
            });
        }

        /// <summary>
        /// Installs a Python package using pip.
        /// </summary>
        /// <param name="pipObject">The pip module imported in Python.</param>
        /// <param name="packageName">The name of the package to be installed.</param>
        /// <exception cref="Exception">Thrown if the package installation fails.</exception>
        public void InstallPackage(string packageName)
        {
            string pythonVersion = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pythonVersion = "python";
            }
            else
            {
                pythonVersion = "python3";
            }

            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = pythonVersion,
                    Arguments = $"-m pip install {packageName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Debug.WriteLine($"Package '{packageName}' installed successfully.");
                        Debug.WriteLine(output);
                    }
                    else
                    {
                        Debug.WriteLine($"Error installing package '{packageName}'.");
                        Debug.WriteLine(error);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Auto-detects the Python DLL path by searching common installation directories.
        /// </summary>
        /// <returns></returns>
        private string AutoDetectPythonPath()
        {
            string[] possibleNames = GetPossiblePythonDllNames();
            IEnumerable<string> searchPaths = GetSearchPaths();

            foreach (string path in searchPaths)
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }

                foreach (string name in possibleNames)
                {
                    string fullPath = Path.Combine(path, name);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets a list of common search paths for Python installations based on the operating system.
        /// </summary>
        private static IEnumerable<string> GetSearchPaths()
        {
            List<string> paths = new List<string>();

            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                paths.AddRange(pathEnv.Split(Path.PathSeparator));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                string[] commonRoots = new[]
                {
                    Path.Combine(localAppData, "Programs", "Python"),
                    Path.Combine(programFiles, "Python3"),
                    "C:\\Python312", "C:\\Python311", "C:\\Python310", "C:\\Python39"
                };

                foreach (string root in commonRoots)
                {
                    if (Directory.Exists(root))
                    {
                        paths.AddRange(Directory.GetDirectories(root, "Python3*"));
                        paths.Add(root);
                    }
                }
            }
            // Linux and macOS paths
            else
            {
                paths.Add("/usr/lib");
                paths.Add("/usr/local/lib");
                paths.Add("/usr/lib/x86_64-linux-gnu");
                paths.Add("/opt/homebrew/lib");
            }

            return paths.Distinct();
        }

        /// <summary>
        /// Gets possible Python DLL names based on the operating system.
        /// </summary>
        private static string[] GetPossiblePythonDllNames()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new[]
                {
                    "python313.dll", "python312.dll", "python311.dll", "python310.dll", "python39.dll", "python38.dll"
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new[]
                {
                    "libpython3.13.so", "libpython3.12.so", "libpython3.11.so", "libpython3.10.so", "libpython3.9.so", "libpython3.8.so",
                    "libpython3.so"
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new[]
                {
                    "libpython3.13.dylib", "libpython3.12.dylib", "libpython3.11.dylib", "libpython3.10.dylib", "libpython3.9.dylib", "libpython3.8.dylib"
                };
            }

            return Array.Empty<string>();
        }
    }
}