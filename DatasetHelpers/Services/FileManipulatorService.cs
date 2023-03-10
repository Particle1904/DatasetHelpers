using DatasetHelpers.Models;

using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace DatasetHelpers.Services
{
    public class FileManipulatorService
    {
        private const ushort _divisor = 64;
        private int _baseResolution = 512;
        private int _lanczosSamplerRadius = 3;

        public int BlocksPerRow { get; private set; }
        private int _totalBlocks;
        Dictionary<double, int> _aspectRatioToBlocks;

        private int _processedImages;

        public FileManipulatorService()
        {
            int BlocksPerRow = _baseResolution / _divisor;
            _totalBlocks = BlocksPerRow * BlocksPerRow;
            _aspectRatioToBlocks = CalculateBuckets(_totalBlocks);
        }

        public void RenameAllToCrescent(string path)
        {
            string[] files = Directory.GetFiles(path);
            Console.WriteLine($"Path: {path}");

            if (files.Length > 0)
            {
                try
                {
                    Console.WriteLine($"Starting the rename process... Found {files.Length} files.");
                    for (int i = 0; i < files.Length; i++)
                    {
                        string extension = Path.GetExtension(files[i]);
                        Console.WriteLine($"Renaming file from {files[i]} to {i + 1}.{extension}");
                        string newFilePath = Path.Combine(path, $"{i + 1}{extension}");
                        File.Move(files[i], newFilePath);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"An exception of name {exception.GetType} occured!");
                    Console.WriteLine($"Message: {exception.Message}");
                }
            }

            Console.WriteLine($"Rename process finished!");
        }

        public void CreateFolderIfNotExist(string folderName)
        {
            string path = Path.Combine(Environment.CurrentDirectory, folderName);
            Directory.CreateDirectory(path);
        }

        public void CreateFolders(string[] foldersName)
        {
            foreach (string folderName in foldersName)
            {
                CreateFolderIfNotExist(folderName);
            }
        }

        public void SortImages(string inputPath, string discardedOutputPath, string selectedOutputPath, int minimumSize = 512)
        {
            Console.WriteLine($"Starting the sorting process... Images with size lower than {minimumSize} will be marked as discarted!");

            string[] files = Directory.GetFiles(inputPath);
            Console.WriteLine($"{files.Length} images found.");

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                Console.WriteLine($"Current image: {fileName}");
                using (Image image = Image.Load(file))
                {
                    if (image.Bounds.Width <= minimumSize && image.Bounds.Height <= minimumSize)
                    {
                        string path = Path.Combine(discardedOutputPath, fileName);
                        File.Move(file, path);
                        Console.WriteLine($"Image {fileName} with size {image.Width}x{image.Height} is lower than the minimum value of {minimumSize}x{minimumSize}.");
                        Console.WriteLine($"Image will be marked as Discarded and will be moved to the appropriate folder.");
                    }
                    else
                    {
                        string path = Path.Combine(selectedOutputPath, fileName);
                        File.Move(file, path);
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

            Console.WriteLine($"{files.Length} images found.");

            CountdownEvent countdown = new CountdownEvent(files.Length);

            foreach (var file in files)
            {
                ResizeParams parameters = new ResizeParams()
                {
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    FilePath = file,
                    CountdownEvent = countdown
                };

                ThreadPool.QueueUserWorkItem(ResizeImage, parameters);
            }

            countdown.Wait();
        }

        private void ResizeImage(object state)
        {
            ResizeParams parameters = (ResizeParams)state;

            string file = parameters.FilePath;
            string fileName = Path.GetFileName(parameters.FilePath);
            string inputPath = parameters.InputPath;
            string outputPath = parameters.OutputPath;

            using (Image image = Image.Load(file))
            {
                double aspectRatio = Math.Round(image.Width / (double)image.Height, 2);

                int bucket = FindAspectRatioBucket(aspectRatio);

                int blocks = _aspectRatioToBlocks.Values.Sum();

                double bucketFraction = 0.0;
                if (_aspectRatioToBlocks.ContainsKey(bucket))
                {
                    bucketFraction = _aspectRatioToBlocks[bucket] / (double)blocks;
                }
                else
                {
                    bucketFraction = 1.0 / (double)blocks;
                }

                double targetAspectRatio = Math.Sqrt(bucketFraction);

                int targetWidth;
                int targetHeight;

                if (image.Width > image.Height)
                {
                    targetWidth = Math.Min(image.Width, 512);
                    targetHeight = (int)Math.Round(targetWidth / aspectRatio);
                }
                else
                {
                    targetHeight = Math.Min(image.Height, 512);
                    targetWidth = (int)Math.Round(targetHeight * aspectRatio);
                }

                Console.WriteLine($"File: {fileName} | Aspect Ratio: {aspectRatio} | Bucket: {bucket} | TWidth: {targetWidth} | THeight: {targetHeight}");

                ResizeOptions resizeOptions = new ResizeOptions()
                {
                    Mode = ResizeMode.BoxPad,
                    Position = AnchorPositionMode.Center,
                    Sampler = new LanczosResampler(_lanczosSamplerRadius),
                    Compand = true,
                    PadColor = Color.White,
                    Size = new Size(targetWidth, targetHeight)
                };

                Console.WriteLine($"Resizing image of size {image.Width}x{image.Height} to {resizeOptions.Size.Width}x{resizeOptions.Size.Height}");
                image.Mutate(image => image.Resize(resizeOptions));
                image.SaveAsPng(Path.ChangeExtension(Path.Combine(outputPath, fileName), ".png"));
                Console.WriteLine($"Image {fileName} was resized and saved to the appropriate folder.");
            }

            parameters.CountdownEvent.Signal();
        }

        private int FindAspectRatioBucket(double aspectRatio)
        {
            foreach (var aspectRatioRange in _aspectRatioToBlocks.Keys.OrderByDescending(k => k))
            {
                if (aspectRatio >= aspectRatioRange)
                {
                    return _aspectRatioToBlocks[aspectRatioRange];
                }
            }

            return _totalBlocks;
        }

        private Dictionary<double, int> CalculateBuckets(int totalBlocks)
        {
            Dictionary<double, int> aspectRatioToBlocks = new Dictionary<double, int>();

            aspectRatioToBlocks[1.0f] = totalBlocks;
            aspectRatioToBlocks[4.0f / 3.0f] = CalculateBlocksForAspectRatio(totalBlocks, 4.0f, 3.0f);
            aspectRatioToBlocks[3.0f / 2.0f] = CalculateBlocksForAspectRatio(totalBlocks, 3.0f, 2.0f);
            aspectRatioToBlocks[1.0f / 1.5f] = CalculateBlocksForAspectRatio(totalBlocks, 1.0f, 1.5f);
            aspectRatioToBlocks[1.0f / 1.85f] = CalculateBlocksForAspectRatio(totalBlocks, 1.0f, 1.85f);
            aspectRatioToBlocks[2.39f / 1.0f] = CalculateBlocksForAspectRatio(totalBlocks, 2.39f, 1.0f);
            aspectRatioToBlocks[4.0f / 5.0f] = CalculateBlocksForAspectRatio(totalBlocks, 4.0f, 5.0f);
            aspectRatioToBlocks[3.0f / 4.0f] = CalculateBlocksForAspectRatio(totalBlocks, 3.0f, 4.0f);
            aspectRatioToBlocks[2.0f / 3.0f] = CalculateBlocksForAspectRatio(totalBlocks, 2.0f, 3.0f);

            return aspectRatioToBlocks;
        }

        private int CalculateBlocksForAspectRatio(int totalBlocks, float width, float height)
        {
            double aspectRatio = width / (double)height;
            double targetArea = totalBlocks * _baseResolution * _baseResolution;

            int targetWidth = (int)Math.Min(_baseResolution, Math.Sqrt(targetArea * aspectRatio));
            int targetHeight = (int)Math.Min(_baseResolution, Math.Sqrt(targetArea / aspectRatio));

            int blocks = (int)Math.Ceiling(targetWidth * targetHeight / (double)_baseResolution);
            return blocks;
        }
    }
}