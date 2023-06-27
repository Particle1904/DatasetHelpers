using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services
{
    public class FileManipulatorService : IFileManipulatorService
    {
        private string _imageSearchPattern = "*.jpg,*.jpeg,*.png,*.gif,*.webp,";

        /// <summary>
        /// Renames all image files and their corresponding caption and text files in the specified directory to have a number in ascending order appended to their names.
        /// Only files with extensions ".jpg", ".jpeg", ".png", ".gif", and ".webp" are considered to be image files.
        /// </summary>
        /// <param name="path">The path to the directory containing the files to rename.</param>
        public async Task RenameAllToCrescentAsync(string inputPath)
        {
            string[] imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            if (imageFiles.Length > 0)
            {
                for (int i = 0; i < imageFiles.Length; i++)
                {
                    string imageFileName = Path.GetFileNameWithoutExtension(imageFiles[i]);

                    string? txtFile = Directory.GetFiles(inputPath, $"{imageFileName}.txt").FirstOrDefault(file => File.Exists(file));
                    if (txtFile != null)
                    {
                        string txtExtension = Path.GetExtension(txtFile);
                        string newTxtName = Path.Combine(inputPath, $"{i + 1}{txtExtension}");
                        await Task.Run(() => File.Move(txtFile, newTxtName));
                    }

                    string? captionFile = Directory.GetFiles(inputPath, $"{imageFileName}.caption").FirstOrDefault(file => File.Exists(file));
                    if (captionFile != null)
                    {
                        string captionExtension = Path.GetExtension(captionFile);
                        string newCaptionName = Path.Combine(inputPath, $"{i + 1}{captionExtension}");
                        await Task.Run(() => File.Move(captionFile, newCaptionName));
                    }

                    string imageExtension = Path.GetExtension(imageFiles[i]);
                    string newImageName = Path.Combine(inputPath, $"{i + 1}{imageExtension}");
                    await Task.Run(() => File.Move(imageFiles[i], newImageName));
                }
            }
        }

        /// <summary>
        /// Renames all image files and their corresponding caption and text files in the specified directory to have a number in ascending order appended to their names.
        /// Only files with extensions ".jpg", ".jpeg", ".png", ".gif", and ".webp" are considered to be image files.
        /// </summary>
        /// <param name="path">The path to the directory containing the files to rename.</param>
        /// <param name="progress">An instance of the Progress class to track the progress of the renaming operation.</param>
        public async Task RenameAllToCrescentAsync(string inputPath, Progress progress)
        {
            string[] imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            if (imageFiles.Length > 0)
            {
                progress.TotalFiles = imageFiles.Length;

                foreach (string file in imageFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string fileExtension = Path.GetExtension(file);
                    string newFileName = Path.Combine(inputPath, $"{fileName}_temp{fileExtension}");

                    await Task.Run(() => File.Move(file, newFileName));

                    string? txtFile = Directory.GetFiles(inputPath, $"{fileName}.txt").FirstOrDefault(file => File.Exists(file));
                    if (txtFile != null)
                    {
                        string txtExtension = Path.GetExtension(txtFile);
                        string newTxtName = Path.Combine(inputPath, $"{fileName}_temp{txtExtension}");
                        await Task.Run(() => File.Move(txtFile, newTxtName));
                    }

                    string? captionFile = Directory.GetFiles(inputPath, $"{fileName}.caption").FirstOrDefault(file => File.Exists(file));
                    if (captionFile != null)
                    {
                        string captionExtension = Path.GetExtension(captionFile);
                        string newCaptionName = Path.Combine(inputPath, $"{fileName}_temp{captionExtension}");
                        await Task.Run(() => File.Move(captionFile, newCaptionName));
                    }

                    progress.UpdateProgress();
                }

                progress.Reset();

                imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

                for (int i = 0; i < imageFiles.Length; i++)
                {
                    string imageFileName = Path.GetFileNameWithoutExtension(imageFiles[i]);

                    string? txtFile = Directory.GetFiles(inputPath, $"{imageFileName}.txt").FirstOrDefault(file => File.Exists(file));
                    if (txtFile != null)
                    {
                        string txtExtension = Path.GetExtension(txtFile);
                        string newTxtName = Path.Combine(inputPath, $"{i + 1}{txtExtension}");
                        await Task.Run(() => File.Move(txtFile, newTxtName));
                    }

                    string? captionFile = Directory.GetFiles(inputPath, $"{imageFileName}.caption").FirstOrDefault(file => File.Exists(file));
                    if (captionFile != null)
                    {
                        string captionExtension = Path.GetExtension(captionFile);
                        string newCaptionName = Path.Combine(inputPath, $"{i + 1}{captionExtension}");
                        await Task.Run(() => File.Move(captionFile, newCaptionName));
                    }

                    string imageExtension = Path.GetExtension(imageFiles[i]);
                    string newImageName = Path.Combine(inputPath, $"{i + 1}{imageExtension}");
                    await Task.Run(() => File.Move(imageFiles[i], newImageName));

                    progress.UpdateProgress();
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
        public async Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, int minimumSize = 512)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                using (Image image = Image.Load(file))
                {
                    if (image.Bounds.Width <= minimumSize && image.Bounds.Height <= minimumSize)
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
        }

        /// <summary>
        /// Sorts images from a specified input directory into separate output directories based on their size.
        /// Images with a width or height less than or equal to the specified minimum size are moved to the discarded output directory,
        /// while larger images are moved to the selected output directory.
        /// </summary>
        /// <param name="inputPath">The path of the directory containing the input images.</param>
        /// <param name="discardedOutputPath">The path of the directory where images that are smaller than or equal to the minimum size will be moved.</param>
        /// <param name="selectedOutputPath">The path of the directory where images that are larger than the minimum size will be moved.</param>
        /// <param name="progress">An instance of the Progress class to track the progress of the image sorting operation.</param>
        /// <param name="minimumSize">The minimum size (in pixels) for images to be considered for the selected output directory. Defaults to 512.</param>
        public async Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, Progress progress, int minimumSize = 512)
        {
            string[] files = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            progress.TotalFiles = files.Length;

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                using (Image image = Image.Load(file))
                {
                    if (image.Bounds.Width <= minimumSize && image.Bounds.Height <= minimumSize)
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
                progress.UpdateProgress();
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
        public async Task BackupFiles(string inputPath, string backupPath)
        {
            string[] imageFiles = Utilities.GetFilesByMultipleExtensions(inputPath, _imageSearchPattern);

            foreach (var image in imageFiles)
            {
                string finalPath = Path.Combine(Path.GetFileName(image), backupPath);
                await Task.Run(() => File.Copy(image, finalPath));
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
            imageFiles.Sort((a, b) => string.Compare(a, b));
            return imageFiles;
        }

        /// <summary>
        /// Retrieves the tags associated with an image located at the specified file path.
        /// </summary>
        /// <param name="imageFilePath">The file path of the image to retrieve tags for.</param>
        /// <returns>A string containing the tags associated with the specified image.</returns>
        public string GetTextFromFile(string imageFilePath, string textFileExtension)
        {
            string txtFilePath = Path.ChangeExtension(imageFilePath, textFileExtension);

            string result = File.ReadAllText(txtFilePath);

            return result;
        }

        /// <summary>
        /// Saves the specified text to a file located at the specified file path.
        /// </summary>
        /// <param name="filePath">The file path where the text will be saved.</param>
        /// <param name="textToSave">The text to save to the file.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public void SaveTextForImage(string filePath, string textToSave)
        {
            File.WriteAllText(filePath, textToSave);
        }
    }
}