using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace DatasetHelpers.Services
{
    public class FileManipulatorService
    {
        public void RenameAllToCrescent(string path)
        {
            string[] files = Directory.GetFiles(path);
            Console.WriteLine($"Path: {path}");

            if (files.Length > 0)
            {
                Console.WriteLine($"Starting the rename process... Found {files.Length} files.");
                for (int i = 0; i < files.Length; i++)
                {
                    string[] extension = files[i].Split(".");
                    Console.WriteLine($"Renaming file from {files[i]} to {i + 1}.{extension.LastOrDefault()}");
                    File.Move(files[i], $"{path}/{i + 1}.{extension.LastOrDefault()}");
                }
                Console.WriteLine($"Rename process finished!");
            }
        }

        public void CreateFolder(string folderName)
        {
            string path = $"{Environment.CurrentDirectory}/{folderName}";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void CreateFolders(string[] foldersName)
        {
            foreach (string folderName in foldersName)
            {
                CreateFolder(folderName);
            }
        }

        public void SortImages(string inputPath, string discardedOutputPath, string selectedOutputPath, int minimumSize = 512)
        {
            Console.WriteLine($"Starting the sorting process... Images with size lower than {minimumSize} will be marked as discarted!");

            string[] files = Directory.GetFiles(inputPath);
            Console.WriteLine($"{files.Length} images found.");

            foreach (string file in files)
            {
                string fileName = GetFilenameAndExtension(file);
                Console.WriteLine($"Current image: {fileName}");
                using (Image image = Image.Load(file))
                {
                    if (image.Bounds.Width <= minimumSize || image.Bounds.Height <= minimumSize)
                    {
                        File.Move(file, $"{discardedOutputPath}/{fileName}");
                        Console.WriteLine($"Image {fileName} with size {image.Width}x{image.Height} is lower than the minimum value of {minimumSize}x{minimumSize}.");
                        Console.WriteLine($"Image will be marked as Discarded and will be moved to the appropriate folder.");
                    }
                    else
                    {
                        File.Move(file, $"{selectedOutputPath}/{fileName}");
                        Console.WriteLine($"Image {fileName} with size {image.Width}x{image.Height} is bigger than the minimum value of {minimumSize}x{minimumSize}.");
                        Console.WriteLine($"Image will be marked as Selected and will be moved to the appropriate folder.");
                    }
                }
            }

            RenameAllToCrescent(discardedOutputPath);
            RenameAllToCrescent(selectedOutputPath);
        }

        public void ResizeImages(string inputPath, string outputPath)
        {
            string[] files = Directory.GetFiles(inputPath);

            ResizeOptions resizeOptions = new ResizeOptions()
            {
                Size = new Size(512),
                Mode = ResizeMode.BoxPad,
                Position = AnchorPositionMode.Center,
                Sampler = new LanczosResampler(9),
                Compand = true,
                PadColor = Color.White
            };

            Console.WriteLine($"Starting the Resize process with sampler: {nameof(resizeOptions.Sampler)}.");
            Console.WriteLine($"{files.Length} images found.");

            foreach (string file in files)
            {
                string fileName = GetFilenameAndExtension(file);
                Console.WriteLine($"Current image: {fileName}");

                using (Image image = Image.Load(file))
                {
                    Console.WriteLine($"Resizing image of size {image.Width}x{image.Height} to {resizeOptions.Size.Width}x{resizeOptions.Size.Height}");
                    image.Mutate(image => image.Resize(resizeOptions));
                    string[] fileNameSplit = fileName.Split(".");
                    image.SaveAsPng($"{outputPath}/{fileNameSplit[0]}.png");
                    Console.WriteLine($"Image {fileName} was resized and saved to the appropriate folder.");

                }
                Console.WriteLine($"Resize process finished!");
            }
        }

        private string GetFilenameAndExtension(string filePath)
        {
            return filePath.Substring(filePath.LastIndexOf(@"\") + 1);
        }
    }
}
