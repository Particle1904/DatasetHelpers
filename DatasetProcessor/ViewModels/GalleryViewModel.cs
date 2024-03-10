using Avalonia.Media.Imaging;
using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;
using DatasetProcessor.src.Models;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GalleryViewModel : BaseViewModel
    {
        private const int MaximumNumberOfImages = 2500;

        private readonly IFileManipulatorService _fileManipulator;

        [ObservableProperty]
        private string _inputFolderPath;

        [ObservableProperty]
        private List<string> _imageFilesPath;
        [ObservableProperty]
        private ObservableCollection<ImageItem> _imageCollection;
        [ObservableProperty]
        private ObservableCollection<ImageItem> _selectedImageItems;

        [ObservableProperty]
        private int _maxImageSize;
        [ObservableProperty]
        private string _maxImageSizeString;

        [ObservableProperty]
        Progress _galleryProcessingProgress;
        [ObservableProperty]
        private bool _isUiEnabled;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        public GalleryViewModel(IFileManipulatorService fileManipulator, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;

            InputFolderPath = Configs.Configurations.CombinedOutputFolder;
            _fileManipulator.CreateFolderIfNotExist(InputFolderPath);

            (_fileManipulator as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                GalleryProcessingProgress = ResetProgress(GalleryProcessingProgress);
                GalleryProcessingProgress.TotalFiles = args;
            };
            (_fileManipulator as INotifyProgress).ProgressUpdated += (sender, args) => GalleryProcessingProgress.UpdateProgress();

            MaxImageSize = 380;
            IsUiEnabled = true;
            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;

            ImageCollection = new ObservableCollection<ImageItem>();
            SelectedImageItems = new ObservableCollection<ImageItem>();
        }

        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
                await LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Loads image files from the specified input folder and populate the ObservableCollection
        /// </summary>
        private async Task LoadImagesFromInputFolder()
        {
            IsUiEnabled = false;

            GalleryProcessingProgress = ResetProgress(GalleryProcessingProgress);

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            TaskStatus = ProcessingStatus.Running;
            try
            {
                // Get images with path
                List<string> imageFiles = _fileManipulator.GetImageFiles(InputFolderPath);
                GalleryProcessingProgress.TotalFiles = imageFiles.Count;
                if (imageFiles.Count != 0)
                {
                    if (imageFiles.Count > MaximumNumberOfImages)
                    {
                        throw new NotSupportedException($"Too many images! Only {MaximumNumberOfImages} total images per folder is supported at the moment!{Environment.NewLine}I suggest splitting your dataset into multiple folders.");
                    }

                    // Order the list of images with path.
                    ImageFilesPath = imageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();

                    // Load the images into a temporary List<ImageItem> so we can sort it.
                    List<ImageItem> imageItems = new List<ImageItem>();
                    Logger.SetLatestLogMessage("Loading image files...", LogMessageColor.Informational);
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < ImageFilesPath.Count; i++)
                        {
                            ImageItem item = new ImageItem()
                            {
                                FileName = Path.GetFileName(ImageFilesPath[i]),
                                Bitmap = new Bitmap(ImageFilesPath[i])
                            };
                            imageItems.Add(item);
                            GalleryProcessingProgress.UpdateProgress();
                        }
                    });

                    ImageCollection = new ObservableCollection<ImageItem>(imageItems);
                }
            }
            catch (NotSupportedException ex)
            {
                Logger.SetLatestLogMessage(ex.Message, LogMessageColor.Error);
                return;
            }
            catch
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            finally
            {
                TaskStatus = ProcessingStatus.Finished;
                IsUiEnabled = true;
            }

            Logger.SetLatestLogMessage("Finished loading image files.", LogMessageColor.Informational);

            // Stop dispatcher timer
            timer.Stop();
            // Stop elapsed timer
            _timer.Stop();
        }

        /// <summary>
        /// Deletes the selected images asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [RelayCommand]
        public async Task DeleteSelectedImagesCommand()
        {
            IsUiEnabled = false;

            GalleryProcessingProgress = ResetProgress(GalleryProcessingProgress);

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            TaskStatus = ProcessingStatus.Running;
            try
            {
                List<string> filesForDeletion = SelectedImageItems.Select(x => x.FileName).ToList();

                await _fileManipulator.DeleteFilesAsync(InputFolderPath, filesForDeletion);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                        LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                IsUiEnabled = true;
                TaskStatus = ProcessingStatus.Finished;

                timer.Stop();
                _timer.Stop();

                // Clear the gallery and load the images again
                ImageCollection.Clear();
                SelectedImageItems.Clear();
                await LoadImagesFromInputFolder();
            }
        }

        partial void OnMaxImageSizeChanged(int value)
        {
            MaxImageSizeString = $"{value}px";
        }
    }
}