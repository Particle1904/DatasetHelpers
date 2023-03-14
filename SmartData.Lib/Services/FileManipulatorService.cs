using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services
{
    public class FileManipulatorService : IFileManipulatorService
    {
        /// <summary>
        /// Renames all image files and their corresponding caption and text files in the specified directory to have a number in ascending order appended to their names.
        /// Only files with extensions ".jpg", ".jpeg", ".png", ".gif", and ".webp" are considered to be image files.
        /// </summary>
        /// <param name="path">The path to the directory containing the files to rename.</param>
        public void RenameAllToCrescent(string path)
        {
            string[] imageFiles = Directory.GetFiles(path, "*.{jpg,jpeg,png,gif,webp}");

            if (imageFiles.Length > 0)
            {
                for (int i = 0; i < imageFiles.Length; i++)
                {
                    string imageFileName = Path.GetFileNameWithoutExtension(imageFiles[i]);

                    string? txtFile = Directory.GetFiles(path, $"{imageFileName}.txt").FirstOrDefault(file => File.Exists(file));
                    if (txtFile != null)
                    {
                        string txtExtension = Path.GetExtension(txtFile);
                        string newTxtName = Path.Combine(path, $"{i + 1}{txtExtension}");
                        File.Move(txtFile, newTxtName);
                    }

                    string? captionFile = Directory.GetFiles(path, $"{imageFileName}.caption").FirstOrDefault(file => File.Exists(file));
                    if (captionFile != null)
                    {
                        string captionExtension = Path.GetExtension(captionFile);
                        string newCaptionName = Path.Combine(path, $"{i + 1}{captionExtension}");
                        File.Move(captionFile, newCaptionName);
                    }

                    string imageExtension = Path.GetExtension(imageFiles[i]);
                    string newImageName = Path.Combine(path, $"{i + 1}{imageExtension}");
                    File.Move(imageFiles[i], newImageName);
                }
            }
        }

        /// <summary>
        /// Renames all image files and their corresponding caption and text files in the specified directory to have a number in ascending order appended to their names.
        /// Only files with extensions ".jpg", ".jpeg", ".png", ".gif", and ".webp" are considered to be image files.
        /// </summary>
        /// <param name="path">The path to the directory containing the files to rename.</param>
        /// <param name="progress">An instance of the Progress class to track the progress of the renaming operation.</param>
        public void RenameAllToCrescent(string path, Progress progress)
        {
            string[] imageFiles = Directory.GetFiles(path, "*.{jpg,jpeg,png,gif,webp}");

            if (imageFiles.Length > 0)
            {
                progress.TotalFiles = imageFiles.Length;

                for (int i = 0; i < imageFiles.Length; i++)
                {
                    string imageFileName = Path.GetFileNameWithoutExtension(imageFiles[i]);

                    string? txtFile = Directory.GetFiles(path, $"{imageFileName}.txt").FirstOrDefault(file => File.Exists(file));
                    if (txtFile != null)
                    {
                        string txtExtension = Path.GetExtension(txtFile);
                        string newTxtName = Path.Combine(path, $"{i + 1}{txtExtension}");
                        File.Move(txtFile, newTxtName);
                    }

                    string? captionFile = Directory.GetFiles(path, $"{imageFileName}.caption").FirstOrDefault(file => File.Exists(file));
                    if (captionFile != null)
                    {
                        string captionExtension = Path.GetExtension(captionFile);
                        string newCaptionName = Path.Combine(path, $"{i + 1}{captionExtension}");
                        File.Move(captionFile, newCaptionName);
                    }

                    string imageExtension = Path.GetExtension(imageFiles[i]);
                    string newImageName = Path.Combine(path, $"{i + 1}{imageExtension}");
                    File.Move(imageFiles[i], newImageName);

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
            string[] files = Directory.GetFiles(inputPath);
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
            string[] files = Directory.GetFiles(inputPath);

            if (files.Length > 0)
            {
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
        public void BackupFiles(string inputPath, string backupPath)
        {
            string[] imageFiles = Directory.GetFiles(inputPath, "*.{jpg,jpeg,png,gif,webp}");

            foreach (var image in imageFiles)
            {
                File.Copy(image, backupPath);
            }
        }
    }
}