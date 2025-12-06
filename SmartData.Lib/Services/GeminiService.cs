using SmartData.Lib.Exceptions;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Services.Base;

using System.Text;

namespace Services
{
    public class GeminiService : CancellableServiceBase, IGeminiService, INotifyProgress
    {
        private readonly IImageProcessorService _imageProcessor;
        private readonly IFileManagerService _fileManager;
        private readonly IPythonService _python;

        public static string BASE_PROMPT = "Create a detailed caption for the image";

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public string ApiKey { get; set; } = string.Empty;
        public string SystemInstructions { get; set; } = string.Empty;
        public bool FreeApi { get; set; } = true;

        public GeminiService(IImageProcessorService imageProcessor, IFileManagerService fileManager, IPythonService python)
        {
            _imageProcessor = imageProcessor;
            _fileManager = fileManager;
            _python = python;
        }

        /// <summary>
        /// Captions a collection of images from the input folder and saves the results in the output folder.
        /// </summary>
        /// <param name="inputFolderPath">The path to the folder containing the images to be captioned.</param>
        /// <param name="outputFolderPath">The path to the folder where the captioned images and text files will be saved.</param>
        /// <param name="userGeminiPrompt">
        /// User-defined prompt to guide the caption generation. If empty, a default prompt will be used. Tags (if file exists) is     
        /// still prioritized over this prompt.
        /// </param>
        /// <remarks>
        /// This method processes all supported image files in the input folder. 
        /// For each image, it optionally reads a prompt from a text file with the same name as the image.
        /// If no text file exists, it uses the provided base prompt or a default prompt.
        /// The method generates captions by making an API request with the image and prompt data.
        /// Results are saved as both a captioned text file and a moved original image in the output folder.
        /// </remarks>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the cancellation token.</exception>
        /// <exception cref="HttpRequestException">Thrown if an HTTP request error occurs during the API call.</exception>
        public async Task CaptionImagesAsync(string inputFolderPath, string outputFolderPath, string failedOutputFolderPath, string userGeminiPrompt)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputFolderPath, Utilities.GetSupportedImagesExtension);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            int imagesThatFailed = 0;

            await Task.Run(() => _python.DownloadPythonPackages());
            if (!_python.IsInitialized)
            {
                try
                {
                    await Task.Run(() => _python.InitializePython());
                }
                catch (PythonNotFoundException)
                {
                    throw;
                }
            }

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string tagsFilePath = Path.ChangeExtension(file, ".txt");
                string captionedImagePath = Path.Combine(outputFolderPath, $"{Path.GetFileNameWithoutExtension(file)}.png");

                if (File.Exists(captionedImagePath))
                {
                    ProgressUpdated?.Invoke(this, EventArgs.Empty);
                    continue;
                }

                try
                {
                    // Try to use existing tags if available.
                    string finalPrompt = string.Empty;

                    // Get tags from the text file if it exists, use the user prompt otherwise.
                    if (File.Exists(tagsFilePath))
                    {
                        finalPrompt = await Task.Run(() => _fileManager.GetTextFromFile(tagsFilePath, ".txt"));
                    }
                    else
                    {
                        finalPrompt = userGeminiPrompt;
                    }


                    // Use the base prompt if no prompt or tags is provided.
                    if (string.IsNullOrEmpty(finalPrompt))
                    {
                        finalPrompt = BASE_PROMPT;
                    }

                    string base64Image = await _imageProcessor.GetBase64ImageAsync(file);
                    string result = await MakeRequestAsync(base64Image, finalPrompt, SystemInstructions);

                    if (result.Equals("Invalid API Key!"))
                    {
                        throw new InvalidGeminiAPIKeyException();
                    }

                    if (result.Contains("BLOCKED CONTENT") || result.Contains("CHECK QUOTA"))
                    {
                        await Task.Run(() => File.Move(file, Path.Combine(failedOutputFolderPath, Path.GetFileName(file))));
                        imagesThatFailed++;
                    }
                    else
                    {
                        string resultPath = Path.Combine(outputFolderPath, Path.GetFileName(file));
                        await Task.Run(() =>
                        {
                            File.Move(file, resultPath);
                            _fileManager.SaveTextToFile(Path.Combine(outputFolderPath, Path.ChangeExtension(Path.GetFileName(file), ".txt")), result.TrimEnd());
                        });
                    }

                    // Sleep for 5 seconds since Gemini API have a 15 requests per minute limitation for free users.
                    if (FreeApi == true)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
                catch (Exception)
                {
                    throw;
                }

                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Sends a request to an external API to generate a caption for an image.
        /// </summary>
        /// <param name="base64image">The base64-encoded string of the image to be captioned.</param>
        /// <param name="prompt">The prompt text to guide the caption generation.</param>
        /// <param name="systemInstructions">System-level instructions to guide the behavior of the API.</param>
        /// <returns>A JSON-formatted string containing the response from the API.</returns>
        /// <remarks>
        /// This method builds a JSON payload containing the image, prompt, and system instructions,
        /// and sends it to an external API endpoint using an HTTP POST request. The response
        /// contains the generated caption and associated metadata.
        /// </remarks>
        /// <exception cref="HttpRequestException">
        /// Thrown when the API request fails, either due to network issues or a non-success status code.
        /// </exception>
        private async Task<string> MakeRequestAsync(string base64image, string prompt, string systemInstructions)
        {
            string fullSystemInstructions = $"{systemInstructions}\nDo NOT include names of real-world people or personalities, or any references to children or teenagers.\nOutput Format: Output a string containing a caption (without double quotes), like:\n<Best caption goes here>";

            // TODO: Expose Model name to settings and UI.
            return await _python.GenerateContent(base64image, prompt, ApiKey, systemInstructions, "gemini-2.0-flash-lite");
        }

        /// <summary>
        /// Creates a base set of system instructions for guiding the caption generation process.
        /// </summary>
        /// <returns>A string containing the base system instructions.</returns>
        /// <remarks>
        /// The generated instructions include detailed guidelines for generating concise, factual, and objective captions.
        /// These captions adhere to a specific format and aim to consolidate tags into descriptive phrases.
        /// </remarks>
        public static string CreateBaseSystemInstruction()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("You are a specialized image analysis system. ");
            stringBuilder.AppendLine("All output must be strictly factual, objective, and devoid of any personal opinions, judgments, or biases.");
            stringBuilder.AppendLine("Your task is to generate concise and factually descriptive captions for each image provided.");
            stringBuilder.AppendLine("Captions must be precise, comprehensive, and meticulously aligned with the visual content depicted in the image and any given tags.");
            stringBuilder.AppendLine("Caption Style: Generate concise captions that are no more than 50 words.");
            stringBuilder.AppendLine("Focus on combining multiple descriptors into small phrases.");
            stringBuilder.AppendLine("Follow this structure: \"A <subject> doing <action>, they are wearing <clothes>. The background is <background description>.");
            stringBuilder.Append(" <Additional camera, lighting, or style information>.");
            stringBuilder.Append("\nIf tags are present, consolidate tags into descriptive phrases where possible, such as \"frilled black dress\" instead of \"dress, frilled dress, black dress\".");
            return stringBuilder.ToString();
        }
    }
}
