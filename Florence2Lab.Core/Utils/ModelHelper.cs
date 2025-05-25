namespace FlorenceTwoLab.Core.Utils;

public class ModelHelper
{
    private readonly string _dataDir;
    private readonly HttpClient _http;

    public string ModelDirectory => Path.Combine(_dataDir, "models");

    public ModelHelper()
    {
        _dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "florence2lab");
        if (!Directory.Exists(_dataDir))
        {
            Directory.CreateDirectory(_dataDir);
        }

        _http = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
    }

    /// <summary>
    /// Ensures that the required ONNX model files for the specified model variant exist locally,
    /// downloading them from the remote source if necessary.
    /// </summary>
    /// <param name="modelVariant">
    /// The variant of the model to ensure files for. Valid values are "base", "base-ft", "large", and "large-ft".
    /// Defaults to "base-ft".
    /// </param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when an invalid model variant is specified.
    /// </exception>
    /// <remarks>
    /// If the required model files are already present in the local model directory, no download will occur.
    /// Files are downloaded from Hugging Face and saved to the model directory if missing.
    /// </remarks>
    public async Task EnsureModelFilesAsync(string modelVariant = "base-ft")
    {
        switch (modelVariant)
        {
            case "base":
            case "base-ft":
            case "large":
            case "large-ft":
                break;
            default:
                throw new ArgumentException($"Invalid model variant '{modelVariant}'", nameof(modelVariant));
        }

        await EnsureMetadataFilesAsync(modelVariant);

        string modelDir = ModelDirectory;

        string[] modelFiles =
        [
            "decoder_model.onnx",
            "embed_tokens.onnx",
            "encoder_model.onnx",
            "vision_encoder.onnx"
        ];

        foreach (string? modelFile in modelFiles.Select(modelFile => Path.Combine(modelDir, modelFile)))
        {
            if (!File.Exists(modelFile))
            {
                Directory.CreateDirectory(modelDir);

                Console.WriteLine($"{Environment.NewLine}Downloading {modelFile}...");

                string url = $"https://huggingface.co/onnx-community/Florence-2-{modelVariant}/resolve/main/onnx/{Path.GetFileName(modelFile)}?download=true";
                using (Stream stream = await _http.GetStreamAsync(url))
                {
                    using (FileStream fileStream = File.Open(modelFile, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);

                        Console.WriteLine($"Download of {modelFile} completed.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Ensures that required metadata files for the specified model variant are present locally,
    /// downloading them if they do not exist.
    /// </summary>
    /// <param name="modelVariant">The model variant identifier used to construct the download URL.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method checks for the presence of specific tokenizer-related metadata files
    /// (vocabulary, merges, and additional vocabulary) in the local data directory. If any file is missing,
    /// it is downloaded from the Hugging Face repository corresponding to the specified model variant.
    /// </remarks>
    private async Task EnsureMetadataFilesAsync(string modelVariant)
    {
        string[] metadataFiles =
        [
            BartTokenizer.BaseVocabFileName,
            BartTokenizer.MergesFileName,
            BartTokenizer.AdditionalVocabFileName
        ];

        foreach (string fileName in metadataFiles)
        {
            string filePath = Path.Combine(_dataDir, fileName);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"{Environment.NewLine}Downloading {fileName}...");

                string url = $"https://huggingface.co/onnx-community/Florence-2-{modelVariant}/resolve/main/{fileName}?download=true";

                using (Stream stream = await _http.GetStreamAsync(url))
                {
                    using (FileStream fileStream = File.Open(filePath, FileMode.Create))
                    {
                        await stream.CopyToAsync(fileStream);

                        Console.WriteLine($"Download of {Path.GetFileName(fileName)} completed.");
                    }
                }
            }
        }
    }
}
