using Interfaces;

using Models.Configurations;
using Models.ModelManager;

using SmartData.Lib.Enums;

using System.Diagnostics;

namespace SmartData.Lib.Services
{
    public class ModelManagerService : IModelManagerService
    {
        private readonly string _modelsFolder;

        public event EventHandler<DownloadNotification>? DownloadMessageEvent;

        private bool _isDownloading = false;
        public bool IsDownloading => _isDownloading;

        public ModelManagerService()
        {
            _modelsFolder = GetModelsFolder();
            Directory.CreateDirectory(_modelsFolder);
        }

        /// <summary>
        /// Determines whether the specified model files need to be downloaded based on the provided model type.
        /// </summary>
        public bool FileNeedsToBeDownloaded(AvailableModels model)
        {
            if (!ModelRegistry.RequiredFiles.TryGetValue(model, out (Models.ModelManager.ModelFileInfo Model, Models.ModelManager.ModelFileInfo? Csv) entry))
            {
                return false;
            }

            // Check ONNX file
            string modelPath = Path.Combine(_modelsFolder, entry.Model.Filename);
            if (!File.Exists(modelPath))
            {
                return true;
            }

            // Check CSV if present
            if (entry.Csv != null)
            {
                string csvPath = Path.Combine(_modelsFolder, entry.Csv.Filename);
                if (!File.Exists(csvPath))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Downloads the specified model file and associated CSV file if needed.
        /// </summary>
        /// <summary>
        /// Downloads the specified model file and associated CSV file (if applicable) based on the provided model type.
        /// </summary>
        public async Task DownloadModelFileAsync(AvailableModels model)
        {
            string modelsFolder = GetModelsFolder();

            if (!Directory.Exists(modelsFolder))
            {
                Directory.CreateDirectory(modelsFolder);
            }

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                // Lookup in registry instead of switch.
                if (!ModelRegistry.RequiredFiles.TryGetValue(model, out (Models.ModelManager.ModelFileInfo Model, Models.ModelManager.ModelFileInfo? Csv) entry))
                {
                    return;
                }

                string modelUrl = entry.Model.DownloadUrl;
                string modelFilename = entry.Model.Filename;
                string? csvUrl = entry.Csv?.DownloadUrl;
                string? csvFilename = entry.Csv?.Filename;

                try
                {
                    DownloadNotification downloadNotification = new DownloadNotification(string.Empty, true);

                    // Download CSV file
                    if (csvUrl is not null && csvFilename is not null)
                    {
                        string csvPath = Path.Combine(modelsFolder, csvFilename);
                        if (!File.Exists(csvPath))
                        {
                            downloadNotification.NotificationMessage = $"Downloading {csvFilename} file...";
                            downloadNotification.PlayNotificationSound = false;
                            DownloadMessageEvent?.Invoke(this, downloadNotification);
                            await DownloadFile(client, csvUrl, csvPath);
                        }
                    }

                    // Download Model file
                    string modelPath = Path.Combine(modelsFolder, modelFilename);
                    if (!File.Exists(modelPath))
                    {
                        downloadNotification.NotificationMessage = $"Downloading {modelFilename} file...";
                        downloadNotification.PlayNotificationSound = true;
                        DownloadMessageEvent?.Invoke(this, downloadNotification);
                        IProgress<double> progress = new Progress<double>(percent =>
                        {
                            downloadNotification.NotificationMessage = $"Downloaded {percent:F2}% of the file {modelFilename}...";
                            downloadNotification.PlayNotificationSound = false;
                            DownloadMessageEvent?.Invoke(this, downloadNotification);
                        });
                        await DownloadFile(client, modelUrl, modelPath, progress);

                        downloadNotification.NotificationMessage = $"Finished downloading {modelFilename} file!";
                        downloadNotification.PlayNotificationSound = true;
                        DownloadMessageEvent?.Invoke(this, downloadNotification);
                    }
                }
                catch (Exception exception)
                {
                    DownloadMessageEvent?.Invoke(this, new DownloadNotification($"Error while trying to download CSV or Model file. {exception.Message}.", true));
                    throw;
                }
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL and saves it to the specified file path,
        /// with optional progress reporting.
        /// </summary>
        /// <param name="client">The HttpClient instance used to perform the download.</param>
        /// <param name="fileUrl">The URL of the file to download.</param>
        /// <param name="filePath">The local file path where the downloaded file will be saved.</param>
        /// <param name="progress">
        /// An optional progress reporter that receives updates on the download progress as a percentage (0 to 100).
        /// </param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// If the file already exists at the specified path, the method will return without downloading.
        /// Progress is reported periodically as the file is downloaded.
        /// </remarks>
        private async Task DownloadFile(HttpClient client, string fileUrl, string filePath, IProgress<double>? progress = null)
        {
            _isDownloading = true;

            using (HttpResponseMessage response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                Stopwatch downloadReportTimer = new Stopwatch();
                downloadReportTimer.Start();

                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;

                using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[81920];
                    int downloadedBytes = 0;
                    int bytesRead = 0;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                        downloadedBytes += bytesRead;

                        if (totalBytes > 0 && progress != null && downloadReportTimer.Elapsed.TotalSeconds >= 0.5f)
                        {
                            double percentage = (double)downloadedBytes / totalBytes * 100.0f;
                            progress.Report(percentage);
                            downloadReportTimer.Restart();
                        }
                    }
                }
            }

            _isDownloading = false;
        }

        /// <summary>
        /// Gets the folder path for storing model files within the current application's directory.
        /// </summary>
        /// <returns>The folder path for model storage.</returns>
        private static string GetModelsFolder() => Path.Combine(Environment.CurrentDirectory, "models");
    }
}
