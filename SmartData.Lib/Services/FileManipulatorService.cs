using SixLabors.ImageSharp;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;
using SmartData.Lib.Models;
using SmartData.Lib.Services.Base;

using System.Text.RegularExpressions;

namespace SmartData.Lib.Services
{
    public class FileManipulatorService : CancellableServiceBase, IFileManipulatorService, INotifyProgress
    {
        private readonly string _imageSearchPattern = Utilities.GetSupportedImagesExtension;

        private readonly string _wdModelLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdModel.onnx?download=true";
        private readonly string _wdCsvLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdTags.csv?download=true";

        private readonly string _wdv3ModelLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdV3Model.onnx?download=true";
        private readonly string _wdv3CsvLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/wdV3Tags.csv?download=true";

        private readonly string _jtModelLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/jtModel.onnx?download=true";
        private readonly string _jtCsvLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/jtTags.csv?download=true";

        private readonly string _e621ModelLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/e621Model.onnx?download=true";
        private readonly string _e621CsvLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/e621Tags.csv?download=true";

        private readonly string _yolov4ModelLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/yolov4.onnx?download=true";

        private readonly string _clipTokenizerLink = @"https://huggingface.co/Crowlley/DatasetToolsModels/resolve/main/cliptokenizer.onnx?download=true";

        private readonly string _parimgCompactModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/ParimgCompact.onnx?download=true";
        private readonly string _swinIRModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/swinIR.onnx?download=true";
        private readonly string _swin2SRModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/swin2SR.onnx?download=true";
        private readonly string _HFA2kCompactModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/HFA2kCompact.onnx?download=true";
        private readonly string _HFA2kAVCSRFormerLightModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/HFA2kAVCSRFormerLight.onnx?download=true";
        private readonly string _HFA2kx4ModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/HFA2k.onnx?download=true";
        private readonly string _nomos8kSCSRFormerModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/Nomos8kSCSRFormer.onnx?download=true";
        private readonly string _nomos8kSCModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/Nomos8kSC.onnx?download=true";
        private readonly string _LSDIRplusRealModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRplusReal.onnx?download=true";
        private readonly string _LSDIRplusNoneModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRplusNone.onnx?download=true";
        private readonly string _LSDIRplusCompressionModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRplusCompression.onnx?download=true";
        private readonly string _LSDIRCompact3ModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIRCompact3.onnx?download=true";
        private readonly string _LSDIRModelLink = @"https://huggingface.co/Crowlley/DatasetToolsUpscalerModels/resolve/main/LSDIR.onnx?download=true";

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;
        public event EventHandler<string> DownloadMessageEvent;

        public FileManipulatorService() : base()
        {
        }

        /// <summary>
        /// Renames all image files and their corresponding caption and text files in the specified directory to have a number in ascending order appended to their names.
        /// Only files with extensions ".jpg", ".jpeg", ".png", ".gif", and ".webp" are considered to be image files.
        /// </summary>
        /// <param name="path">The path to the directory containing the files to rename.</param>
        /// /// <remarks>
        /// This method scans the specified directory for image files with the extensions ".jpg", ".jpeg", ".png", ".gif", and ".webp". It renames each image file and its corresponding caption and text files by appending a number in ascending order to their names. The renaming process is performed in two steps:
        /// 1. Each file is temporarily renamed by adding the suffix "_temp" before the extension.
        /// 2. The files are then renamed with a number in ascending order starting from 1.
        /// </remarks>
        public async Task RenameAllToCrescentAsync(string inputPath, int startingNumberForFileNames = 1)
        {
            string[] imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            if (imageFiles.Length > 0)
            {
                TotalFilesChanged?.Invoke(this, imageFiles.Length);

                foreach (string file in imageFiles)
                {
                    await RenameFileToTemporaryName(inputPath, file);
                    ProgressUpdated?.Invoke(this, EventArgs.Empty);
                }

                TotalFilesChanged?.Invoke(this, imageFiles.Length);

                imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

                for (int i = 0; i < imageFiles.Length; i++)
                {
                    await RenameFileToCrescentName(inputPath, imageFiles, i, startingNumberForFileNames);
                    ProgressUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Sorts images from a specified input directory into separate output directories based on their size.
        /// Images with a width or height less than or equal to the specified minimum size are moved to the discarded output directory,
        /// while larger images are moved to the selected output directory.
        /// </summary>
        /// <param name="inputPath">The path of the directory containing the input images.</param>
        /// <param name="discardedOutputPath">The path of the directory where images that are smaller than or equal to the minimum size will be moved.</param>
        /// <param name="selectedOutputPath">The path of the directory where images that are larger than the minimum size will be moved.</param>
        /// <param name="minimumSize">The minimum size (in pixels) for images to be considered for the selected output directory. Defaults to 512.</param>
        public async Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, SupportedDimensions dimension = SupportedDimensions.Resolution512x512)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Length);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await SortImageAsync(discardedOutputPath, selectedOutputPath, dimension, file);
                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Creates a folder with the specified name if it does not already exist at the current directory path.
        /// </summary>
        /// <param name="folderName">The name of the folder to create.</param>
        public void CreateFolderIfNotExist(string folderName)
        {
            string path = Path.Combine(Environment.CurrentDirectory, folderName);
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Copies all image files in the specified input directory to the specified backup directory.
        /// </summary>
        /// <param name="inputPath">The full path of the directory containing the image files to backup.</param>
        /// <param name="backupPath">The full path of the directory to copy the image files to.</param>
        /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs during file copying.</exception>
        public async Task BackupFilesAsync(string inputPath, string backupPath)
        {
            string[] imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);
            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            foreach (var image in imageFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string finalPath = Path.Combine(Path.GetFileName(image), backupPath);
                await Task.Run(() => File.Copy(image, finalPath), cancellationToken);
            }
        }

        /// <summary>
        /// Creates a subset of files based on the provided list by copying them to the specified output path. 
        /// If accompanying '.txt' and '.caption' files exist, they are also copied.
        /// </summary>
        /// <param name="files">A list of file paths to create a subset from.</param>
        /// <param name="outputPath">The path to the output folder where the subset files will be copied.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task CreateSubsetAsync(List<string> files, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentNullException($"Please select an output folder!");
            }

            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, files.Count);

            foreach (string file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string folderPath = Path.GetDirectoryName(file);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                string outputImageFile = Path.Combine(outputPath, $"{Path.GetFileName(file)}");
                await Task.Run(() => File.Copy(file, outputImageFile, true));

                await CopyOptionalFileAsync(folderPath, $"{fileNameWithoutExtension}.txt", outputPath);
                await CopyOptionalFileAsync(folderPath, $"{fileNameWithoutExtension}.caption", outputPath);

                ProgressUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Returns an array of image files from the given input path based on the defined image search pattern.
        /// </summary>
        /// <param name="inputPath">The path of the directory to search for image files.</param>
        /// <returns>An array of image file names.</returns>
        public List<string> GetImageFiles(string inputPath)
        {
            List<string> imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern).ToList();
            if (imageFiles.Count > 0)
            {
                TotalFilesChanged?.Invoke(this, imageFiles.Count);
            }
            imageFiles.Sort((a, b) => string.Compare(a, b));
            return imageFiles;
        }

        /// <summary>
        /// Retrieves a list of image files from the specified input path that contain any of the specified words in their captions.
        /// </summary>
        /// <param name="inputPath">The directory path to search for image files.</param>
        /// <param name="txtFileExtension">The file extension for text files containing captions (must be either ".txt" or ".caption").</param>
        /// <param name="wordsToFilter">A comma-separated string of words to filter the image files by.</param>
        /// <returns>A sorted list of image files whose captions contain any of the specified words.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided txtFileExtension is not ".txt" or ".caption".</exception>
        public List<string> GetFilteredImageFiles(string inputPath, string txtFileExtension, string wordsToFilter, bool exactMatchesOnly)
        {
            if (!txtFileExtension.Equals(".txt") && !txtFileExtension.Equals(".caption"))
            {
                throw new ArgumentException("File extension must be either .txt or .caption.");
            }

            FilterSettings filterSettings = ParseFilterString(wordsToFilter);
            List<string> imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern).ToList();

            List<string> filteredImageFiles = FilterImageFiles(txtFileExtension, imageFiles, filterSettings.IncludeTags, exactMatchesOnly);
            List<string> filteredImageFilesByAnd = FilterImageFilesByMultiple(txtFileExtension, imageFiles, filterSettings.AndTags);

            List<string> resultUnion = filteredImageFiles.Union(filteredImageFilesByAnd).ToList();

            List<string> unwantedImageFiles = FilterImageFiles(txtFileExtension, resultUnion, filterSettings.ExcludeTags, true);

            return resultUnion.Except(unwantedImageFiles).ToList();
        }

        /// <summary>
        /// Filters a list of image files based on multiple tags, ensuring that all tags in a tag group are present in the image's caption.
        /// </summary>
        /// <param name="txtFileExtension">The file extension for the text files containing captions.</param>
        /// <param name="imageFiles">The list of image files to filter.</param>
        /// <param name="wordsSplit">An array of tags to filter the image files by, where compound tags are specified using "AND".</param>
        /// <returns>A list of filtered image files that match the specified tag conditions.</returns>
        private List<string> FilterImageFilesByMultiple(string txtFileExtension, List<string> imageFiles, string[] wordsSplit)
        {
            List<string> filteredImageFiles = new List<string>();

            foreach (string image in imageFiles)
            {
                try
                {
                    string caption = GetTextFromFile(image, txtFileExtension);
                    foreach (string tag in wordsSplit)
                    {
                        string[] andSplit = tag.Replace(" AND ", "AND").Split("AND");
                        if (andSplit.Length <= 1)
                        {
                            throw new ArgumentException($"AND syntax needs 2 or more tags separated by AND. Example: \"tag1 and tag2\".");
                        }

                        string[] captionSplit = caption.Replace(", ", ",").Split(",");
                        if (andSplit.Intersect(captionSplit).Count() == andSplit.Length)
                        {
                            filteredImageFiles.Add(image);
                            break;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }

            return filteredImageFiles;
        }

        /// <summary>
        /// Filters a list of image files based on specified criteria.
        /// </summary>
        /// <param name="txtFileExtension">The file extension to filter image files by (e.g., ".txt", ".caption").</param>
        /// <param name="imageFiles">The list of image file paths to be filtered.</param>
        /// <param name="wordsSplit">An array of tags or keywords to filter by.</param>
        /// <param name="exactMatch">A flag indicating whether to perform an exact tag match or partial match.</param>
        /// <returns>A list of filtered image file paths that match the specified criteria.</returns>
        private List<string> FilterImageFiles(string txtFileExtension, List<string> imageFiles, string[] wordsSplit, bool exactMatch)
        {
            List<string> filteredImageFiles = new List<string>();

            foreach (string image in imageFiles)
            {
                try
                {
                    string caption = GetTextFromFile(image, txtFileExtension);
                    foreach (string tag in wordsSplit)
                    {
                        if (exactMatch)
                        {
                            string[] captionSplit = caption.Replace(", ", ",").Split(",");
                            foreach (string keyword in captionSplit)
                            {
                                if (tag.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                                {
                                    filteredImageFiles.Add(image);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            string wordBoundaryPattern = $@"\b{Regex.Escape(tag)}\b";
                            if (Regex.IsMatch(caption, wordBoundaryPattern, RegexOptions.IgnoreCase, Utilities.RegexTimeout))
                            {
                                filteredImageFiles.Add(image);
                                break;
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
            }

            return filteredImageFiles;
        }

        /// <summary>
        /// Retrieves a list of image files from the specified input path that contain any of the specified words in their captions.
        /// </summary>
        /// <param name="inputPath">The directory path to search for image files.</param>
        /// <param name="txtFileExtension">The file extension for text files containing captions (must be either ".txt" or ".caption").</param>
        /// <param name="wordsToFilter">A comma-separated string of words to filter the image files by.</param>
        /// <returns>A sorted list of image files whose captions contain any of the specified words.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided txtFileExtension is not ".txt" or ".caption".</exception>
        public List<string> GetFilteredImageFiles(string inputPath, string txtFileExtension, string wordsToFilter)
        {
            if (!txtFileExtension.Equals(".txt") && !txtFileExtension.Equals(".caption"))
            {
                throw new ArgumentException("File extension must be either .txt or .caption.");
            }

            List<string> imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern).ToList();

            TotalFilesChanged?.Invoke(this, imageFiles.Count);

            List<string> filteredImageFiles = new List<string>();
            string[] wordsSplit = wordsToFilter.Replace(", ", ",").Split(",");

            foreach (string image in imageFiles)
            {
                try
                {
                    string caption = GetTextFromFile(image, txtFileExtension);
                    foreach (string tag in wordsSplit)
                    {
                        string wordBoundaryPattern = $@"\b{Regex.Escape(tag)}\b";
                        if (Regex.IsMatch(caption, wordBoundaryPattern, RegexOptions.IgnoreCase, Utilities.RegexTimeout))
                        {
                            filteredImageFiles.Add(image);
                            break;
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
                finally
                {
                    ProgressUpdated?.Invoke(this, EventArgs.Empty);
                }
            }

            filteredImageFiles.Sort((a, b) => string.Compare(a, b));
            return filteredImageFiles;
        }

        /// <summary>
        /// Retrieves the tags associated with an image located at the specified file path.
        /// </summary>
        /// <param name="imageFilePath">The file path of the image to retrieve tags for.</param>
        /// <returns>A string containing the tags associated with the specified image.</returns>
        public string GetTextFromFile(string imageFilePath, string txtFileExtension)
        {
            string txtFilePath = Path.ChangeExtension(imageFilePath, txtFileExtension);

            string result = File.ReadAllText(txtFilePath);

            return result;
        }

        /// <summary>
        /// Saves the specified text to a file located at the specified file path.
        /// </summary>
        /// <param name="filePath">The file path where the text will be saved.</param>
        /// <param name="textToSave">The text to save to the file.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public void SaveTextToFile(string filePath, string textToSave)
        {
            File.WriteAllText(filePath, textToSave);
        }

        /// <summary>
        /// Determines whether the specified model files need to be downloaded based on the provided model type.
        /// </summary>
        /// <param name="model">The type of model to check.</param>
        /// <returns>True if the model files need to be downloaded; otherwise, false.</returns>
        public bool FileNeedsToBeDownloaded(AvailableModels model)
        {
            bool result = false;

            switch (model)
            {
                case AvailableModels.JoyTag:
                    if (!Path.Exists(FileNames.JoyTagOnnxFileName) || !Path.Exists(FileNames.JoyTagCsvFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.WD14v2:
                    if (!Path.Exists(FileNames.WDOnnxFileName) || !Path.Exists(FileNames.WDCsvFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.WDv3:
                    if (!Path.Exists(FileNames.WDV3OnnxFileName) || !Path.Exists(FileNames.WDV3CsvFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.Z3DE621:
                    if (!Path.Exists(FileNames.E621OnnxFileName) || !Path.Exists(FileNames.E621CsvFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.Yolov4:
                    if (!Path.Exists(FileNames.YoloV4OnnxFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.ParimgCompact_x2:
                    if (!Path.Exists(FileNames.ParimgCompactFileName))
                    { 
                        result = true; 
                    }
                    break;
                case AvailableModels.HFA2kCompact_x2:
                    if (!Path.Exists(FileNames.HFA2kCompactFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.HFA2kAVCSRFormerLight_x2:
                    if (!Path.Exists(FileNames.HFA2kAVCSRFormerLightFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.HFA2k_x4:
                    if (!Path.Exists(FileNames.HFA2kFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.SwinIR_x4:
                    if (!Path.Exists(FileNames.SwinIRFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.Swin2SR_x4:
                    if (!Path.Exists(FileNames.Swin2SRFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.Nomos8kSCSRFormer_x4:
                    if (!Path.Exists(FileNames.Nomos8kSCSRFormerFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.Nomos8kSC_x4:
                    if (!Path.Exists(FileNames.Nomos8kSCFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.LSDIRplusReal_x4:
                    if (!Path.Exists(FileNames.LSDIRplusRealFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.LSDIRplusNone_x4:
                    if (!Path.Exists(FileNames.LSDIRplusNoneFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.LSDIRplusCompression_x4:
                    if (!Path.Exists(FileNames.LSDIRplusCompressionFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.LSDIRCompact3_x4:
                    if (!Path.Exists(FileNames.LSDIRCompact3FileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.LSDIR_x4:
                    if (!Path.Exists(FileNames.LSDIRFileName))
                    {
                        result = true;
                    }
                    break;
                case AvailableModels.CLIPTokenizer:
                    if (!Path.Exists(FileNames.CLIPTokenixerOnnxFileName))
                    {
                        result = true;
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Downloads the specified model file and associated CSV file (if applicable) based on the provided model type.
        /// </summary>
        /// <param name="model">The type of model to download.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DownloadModelFile(AvailableModels model)
        {
            if (!Path.Exists(GetModelsFolder()))
            {
                Directory.CreateDirectory(GetModelsFolder());
            }

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                string modelUrl = string.Empty;
                string modelFileName = string.Empty;
                string csvUrl = string.Empty;
                string csvFileName = string.Empty;

                switch (model)
                {
                    case AvailableModels.JoyTag:
                        modelUrl = _jtModelLink;
                        modelFileName = FileNames.JoyTagOnnxFileName;
                        csvUrl = _jtCsvLink;
                        csvFileName = FileNames.JoyTagCsvFileName;
                        break;
                    case AvailableModels.WD14v2:
                        modelUrl = _wdModelLink;
                        modelFileName = FileNames.WDOnnxFileName;
                        csvUrl = _wdCsvLink;
                        csvFileName = FileNames.WDCsvFileName;
                        break;
                    case AvailableModels.WDv3:
                        modelUrl = _wdv3ModelLink;
                        modelFileName = FileNames.WDV3OnnxFileName;
                        csvUrl = _wdv3CsvLink;
                        csvFileName = FileNames.WDV3CsvFileName;
                        break;
                    case AvailableModels.Z3DE621:
                        modelUrl = _e621ModelLink;
                        modelFileName = FileNames.E621OnnxFileName;
                        csvUrl = _e621CsvLink;
                        csvFileName = FileNames.E621CsvFileName;
                        break;
                    case AvailableModels.Yolov4:
                        modelUrl = _yolov4ModelLink;
                        modelFileName = FileNames.YoloV4OnnxFileName;
                        break;
                    case AvailableModels.ParimgCompact_x2:
                        modelUrl = _parimgCompactModelLink;
                        modelFileName = FileNames.ParimgCompactFileName;
                        break;
                    case AvailableModels.HFA2kCompact_x2:
                        modelUrl = _HFA2kCompactModelLink;
                        modelFileName = FileNames.HFA2kCompactFileName;
                        break;
                    case AvailableModels.HFA2kAVCSRFormerLight_x2:
                        modelUrl = _HFA2kAVCSRFormerLightModelLink;
                        modelFileName = FileNames.HFA2kAVCSRFormerLightFileName;
                        break;
                    case AvailableModels.HFA2k_x4:
                        modelUrl = _HFA2kx4ModelLink;
                        modelFileName = FileNames.HFA2kFileName;
                        break;
                    case AvailableModels.SwinIR_x4:
                        modelUrl = _swinIRModelLink;
                        modelFileName = FileNames.SwinIRFileName;
                        break;
                    case AvailableModels.Swin2SR_x4:
                        modelUrl = _swin2SRModelLink;
                        modelFileName = FileNames.Swin2SRFileName;
                        break;
                    case AvailableModels.Nomos8kSCSRFormer_x4:
                        modelUrl = _nomos8kSCSRFormerModelLink;
                        modelFileName = FileNames.Nomos8kSCSRFormerFileName;
                        break;
                    case AvailableModels.Nomos8kSC_x4:
                        modelUrl = _nomos8kSCModelLink;
                        modelFileName = FileNames.Nomos8kSCFileName;
                        break;
                    case AvailableModels.LSDIRplusReal_x4:
                        modelUrl = _LSDIRplusRealModelLink;
                        modelFileName = FileNames.LSDIRplusRealFileName;
                        break;
                    case AvailableModels.LSDIRplusNone_x4:
                        modelUrl = _LSDIRplusNoneModelLink;
                        modelFileName = FileNames.LSDIRplusNoneFileName;
                        break;
                    case AvailableModels.LSDIRplusCompression_x4:
                        modelUrl = _LSDIRplusCompressionModelLink;
                        modelFileName = FileNames.LSDIRplusCompressionFileName;
                        break;
                    case AvailableModels.LSDIRCompact3_x4:
                        modelUrl = _LSDIRCompact3ModelLink;
                        modelFileName = FileNames.LSDIRCompact3FileName;
                        break;
                    case AvailableModels.LSDIR_x4:
                        modelUrl = _LSDIRModelLink;
                        modelFileName = FileNames.LSDIRFileName;
                        break;
                    case AvailableModels.CLIPTokenizer:
                        modelUrl = _clipTokenizerLink;
                        modelFileName = FileNames.CLIPTokenixerOnnxFileName;
                        break;
                    default:
                        break;
                }

                try
                {
                    if (!string.IsNullOrEmpty(csvUrl))
                    {
                        DownloadMessageEvent?.Invoke(this, $"Downloading {csvFileName} file...");
                        await DownloadFile(client, csvUrl, Path.Combine(GetModelsFolder(), csvFileName));
                    }

                    DownloadMessageEvent?.Invoke(this, $"Downloading {modelFileName} file...");
                    await DownloadFile(client, modelUrl, Path.Combine(GetModelsFolder(), modelFileName));
                    DownloadMessageEvent?.Invoke(this, $"Finished downloading {modelFileName} file!");
                }
                catch (Exception exception)
                {
                    DownloadMessageEvent?.Invoke(this, $"Error while trying to download CSV or Model file. {exception.Message}.");
                    throw;
                }
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL and saves it to the specified file path.
        /// </summary>
        /// <param name="client">The HttpClient instance used to perform the download.</param>
        /// <param name="fileUrl">The URL of the file to download.</param>
        /// <param name="filePath">The local file path where the downloaded file will be saved.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task DownloadFile(HttpClient client, string fileUrl, string filePath)
        {
            if (Path.Exists(filePath))
            {
                return;
            }

            using (HttpResponseMessage response = await client.GetAsync(fileUrl))
            {
                response.EnsureSuccessStatusCode();
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create,
                    FileAccess.Write, FileShare.None))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        /// <summary>
        /// Sorts an image file into separate output directories based on its size.
        /// Images with a width or height less than or equal to the specified minimum size are moved to the discarded output directory,
        /// while larger images are moved to the selected output directory.
        /// </summary>
        /// <param name="discardedOutputPath">The path of the directory where images that are smaller than or equal to the minimum size will be moved.</param>
        /// <param name="selectedOutputPath">The path of the directory where images that are larger than the minimum size will be moved.</param>
        /// <param name="dimension">The supported dimension to compare the image size against.</param>
        /// <param name="file">The path to the image file to be sorted.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous sorting operation.</returns>
        private static async Task SortImageAsync(string discardedOutputPath, string selectedOutputPath, SupportedDimensions dimension, string file)
        {
            string fileName = Path.GetFileName(file);
            using (Image image = Image.Load(file))
            {
                if (image.Bounds.Width < (int)dimension && image.Bounds.Height < (int)dimension)
                {
                    string path = Path.Combine(discardedOutputPath, fileName);
                    using (FileStream readStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (FileStream writeStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await readStream.CopyToAsync(writeStream);
                    }
                }
                else
                {
                    string path = Path.Combine(selectedOutputPath, fileName);
                    using (FileStream readStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (FileStream writeStream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await readStream.CopyToAsync(writeStream);
                    }
                }
            }
        }

        /// <summary>
        /// Renames a file to a temporary name by appending "_temp" to the file name.
        /// If associated .txt or .caption files exist, they are also renamed to temporary names.
        /// </summary>
        /// <param name="inputPath">The path of the directory where the file and associated files exist.</param>
        /// <param name="file">The path of the file to be renamed.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous renaming operation.</returns>
        private static async Task RenameFileToTemporaryName(string inputPath, string file)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string fileExtension = Path.GetExtension(file);
            string newFileName = Path.Combine(inputPath, $"{fileName}_temp{fileExtension}");

            await Task.Run(() => File.Move(file, newFileName));

            string[] supportedFileExtensions = new string[] { ".txt", ".caption" };

            foreach (string extension in supportedFileExtensions)
            {
                string filePath = Path.Combine(inputPath, $"{fileName}{extension}");
                if (File.Exists(filePath))
                {
                    string txtExtension = Path.GetExtension(filePath);
                    string newTxtName = Path.Combine(inputPath, $"{fileName}_temp{txtExtension}");
                    await Task.Run(() => File.Move(filePath, newTxtName));
                }
            }
        }

        /// <summary>
        /// Renames a file and associated .txt and .caption files to the crescent order name.
        /// The file and associated files are renamed based on their position in the imageFiles array.
        /// </summary>
        /// <param name="inputPath">The path of the directory where the files exist.</param>
        /// <param name="imageFiles">An array of image file paths.</param>
        /// <param name="i">The index of the current file in the imageFiles array.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous renaming operation.</returns>
        private static async Task RenameFileToCrescentName(string inputPath, string[] imageFiles, int i, int startingNumberForFileNames)
        {
            string imageFileName = Path.GetFileNameWithoutExtension(imageFiles[i]);

            string[] supportedFileExtensions = new string[] { ".txt", ".caption" };

            foreach (string extension in supportedFileExtensions)
            {
                string filePath = Path.Combine(inputPath, $"{imageFileName}{extension}");
                if (File.Exists(filePath))
                {
                    string txtExtension = Path.GetExtension(filePath);
                    string newTxtName = Path.Combine(inputPath, $"{i + startingNumberForFileNames}{txtExtension}");
                    await Task.Run(() => File.Move(filePath, newTxtName));
                }
            }

            string imageExtension = Path.GetExtension(imageFiles[i]);
            string newImageName = Path.Combine(inputPath, $"{i + startingNumberForFileNames}{imageExtension}");
            await Task.Run(() => File.Move(imageFiles[i], newImageName));
        }

        /// <summary>
        /// Copies an optional file from the source directory to the target directory if it exists.
        /// </summary>
        /// <param name="sourceDirectory">The source directory containing the optional file.</param>
        /// <param name="fileName">The name of the optional file.</param>
        /// <param name="targetDirectory">The target directory where the optional file will be copied.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task CopyOptionalFileAsync(string sourceDirectory, string fileName, string targetDirectory)
        {
            string sourceFilePath = Path.Combine(sourceDirectory, fileName);
            if (File.Exists(sourceFilePath))
            {
                string targetFilePath = Path.Combine(targetDirectory, fileName);
                await Task.Run(() => File.Copy(sourceFilePath, targetFilePath, true));
            }
        }

        /// <summary>
        /// Parses a comma-separated string of tags to create a <see cref="TagFilterSettings"/> instance.
        /// Tags can be included or excluded using a "-" prefix. Returns the settings with parsed tags.
        /// </summary>
        /// <param name="wordsToFilter">The comma-separated string of tags/keywords to parse.</param>
        /// <returns>A <see cref="TagFilterSettings"/> instance containing parsed include and exclude tags.</returns>
        private static FilterSettings ParseFilterString(string wordsToFilter)
        {
            string[] tagsSplit = wordsToFilter.Replace(", ", ",").Split(",");

            List<string> includeTags = new List<string>();
            List<string> excludeTags = new List<string>();
            List<string> andTags = new List<string>();

            for (int i = 0; i < tagsSplit.Length; i++)
            {
                if (tagsSplit[i].Contains("!"))
                {
                    excludeTags.Add(tagsSplit[i].Replace("!", ""));

                }
                else if (tagsSplit[i].Contains("AND"))
                {
                    andTags.Add(tagsSplit[i]);
                }
                else
                {
                    includeTags.Add(tagsSplit[i]);
                }
            }

            return new FilterSettings()
            {
                IncludeTags = includeTags.ToArray(),
                ExcludeTags = excludeTags.ToArray(),
                AndTags = andTags.ToArray()
            };
        }

        /// <summary>
        /// Gets the folder path for storing model files within the current application's directory.
        /// </summary>
        /// <returns>The folder path for model storage.</returns>
        private static string GetModelsFolder() => Path.Combine(Environment.CurrentDirectory, "models");

        /// <summary>
        /// Asynchronously deletes the specified image files from the input folder.
        /// </summary>
        /// <param name="inputPath">The path of the input folder containing the image files.</param>
        /// <param name="imageFiles">The list of image file names to delete.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when no image files are selected for deletion.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the input folder path is null or empty.</exception>
        public async Task DeleteFilesAsync(string inputPath, List<string> imageFiles)
        {
            if (imageFiles.Count <= 0)
            {
                throw new ArgumentException($"Please select at least one image for deletion!");
            }

            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException($"Please select an input folder!");
            }

            CancellationToken cancellationToken = _cancellationTokenSource.Token;

            TotalFilesChanged?.Invoke(this, imageFiles.Count);
            foreach (string image in imageFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string fullPath = Path.Combine(inputPath, image);

                if (File.Exists(fullPath))
                {
                    await Task.Run(() => File.Delete(fullPath));

                    // Also try to delete any related .txt
                    string txtFile = Path.ChangeExtension(fullPath, ".txt");
                    if (File.Exists(txtFile))
                    {
                        await Task.Run(() => File.Delete(txtFile));
                    }
                    // And try to delete any related .caption
                    string captionFile = Path.ChangeExtension(fullPath, ".caption");
                    if (File.Exists(captionFile))
                    {
                        await Task.Run(() => File.Delete(captionFile));
                    }

                    ProgressUpdated?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}