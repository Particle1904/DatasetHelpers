using Python.Runtime;

using SmartData.Lib.Interfaces;

using System.Diagnostics;

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
            Runtime.PythonDLL = "python310.dll";
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
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
        /// <param name="promtp">The text prompt to guide content generation.</param>
        /// <param name="geminiApiKey">The API key for authentication with the Generative AI service.</param>
        /// <param name="systemInstructions">System-level instructions for the model configuration.</param>
        /// <returns>A string containing the generated content.</returns>
        /// <exception cref="Exception">Thrown if content generation fails.</exception>
        public async Task<string> GenerateContent(string base64Image, string prompt, string geminiApiKey, string systemInstructions)
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
        model_name='gemini-2.0-flash-exp',
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
            if (OperatingSystem.IsWindows())
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
    }
}