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
        private readonly IFileManipulatorService _fileManipulator;

        private const int ItemsPerPage = 200;

        [ObservableProperty]
        private string _inputFolderPath;

        [ObservableProperty]
        private List<string> _imageFilesPath;
        [ObservableProperty]
        private ObservableCollection<ImageItem> _imageCollection;
        [ObservableProperty]
        private ObservableCollection<ImageItem> _selectedImageItems;

        [ObservableProperty]
        private int _currentPage;
        [ObservableProperty]
        private string _currentPageString;

        [ObservableProperty]
        private int _maxImageSize;
        [ObservableProperty]
        private string _maxImageSizeString;

        [ObservableProperty]
        Progress _galleryProcessingProgress;
        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

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

            CurrentPage = 0;
            CurrentPageString = string.Empty;
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
            IsCancelEnabled = false;

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

                if (imageFiles.Count <= 0)
                {
                    // Stop dispatcher timer
                    timer.Stop();
                    // Stop elapsed timer
                    _timer.Stop();
                    return;
                }
                // Order the list of images with path.
                ImageFilesPath = imageFiles.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x))).ToList();

                int startIndex = CurrentPage * ItemsPerPage;
                int endIndex = Math.Min(startIndex + ItemsPerPage, imageFiles.Count);
                List<string> visibleImages = ImageFilesPath.GetRange(Math.Clamp(startIndex, 0, imageFiles.Count),
                    Math.Clamp(endIndex - startIndex, 0, imageFiles.Count));

                GalleryProcessingProgress.TotalFiles = visibleImages.Count;

                // Load the images into a temporary List<ImageItem> so we can sort it.
                List<ImageItem> imageItems = new List<ImageItem>(visibleImages.Count);
                Logger.SetLatestLogMessage("Loading image files...", LogMessageColor.Informational);
                await Task.Run(() =>
                {
                    for (int i = 0; i < visibleImages.Count; i++)
                    {
                        ImageItem item = new ImageItem()
                        {
                            FileName = Path.GetFileName(visibleImages[i]),
                            Bitmap = new Bitmap(visibleImages[i])
                        };
                        imageItems.Add(item);
                        GalleryProcessingProgress.UpdateProgress();
                    }
                });

                ImageCollection = new ObservableCollection<ImageItem>(imageItems);
            }
            catch (NotSupportedException exception)
            {
                Logger.SetLatestLogMessage(exception.Message, LogMessageColor.Error);
                return;
            }
            catch (Exception)
            {
                Logger.SetLatestLogMessage("No image files were found in the directory.", LogMessageColor.Error);
            }
            finally
            {
                TaskStatus = ProcessingStatus.Finished;
                IsUiEnabled = true;
                IsCancelEnabled = false;
            }

            Logger.SetLatestLogMessage("Finished loading image files.", LogMessageColor.Informational);

            // Stop dispatcher timer
            timer.Stop();
            // Stop elapsed timer
            _timer.Stop();
        }

        /// <summary>
        /// Navigates to a specific page in the image list.
        /// </summary>
        /// <param name="parameter">The navigation parameter indicating the page index.</param>
        [RelayCommand]
        private async Task GoToItem(string parameter)
        {
            try
            {
                int.TryParse(parameter, out int parameterInt);

                if (ImageFilesPath?.Count != 0 && ImageFilesPath != null)
                {
                    CurrentPage += parameterInt;
                    await LoadImagesFromInputFolder();
                }
            }
            catch
            {
                Logger.SetLatestLogMessage("Couldn't load the image.", LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Deletes the selected images asynchronously.
        /// </summary>
        /// <returns>A Task representing the asynchronous operation.</returns>
        [RelayCommand]
        public async Task DeleteSelectedImagesCommand()
        {
            IsUiEnabled = false;

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
            catch (OperationCanceledException)
            {
                IsCancelEnabled = false;
                Logger.SetLatestLogMessage($"Cancelled the current operation!", LogMessageColor.Informational);
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

        partial void OnCurrentPageChanged(int value)
        {
            CurrentPageString = $"Current page: {CurrentPage + 1}";
            CurrentPage = Math.Clamp(CurrentPage, 0, Math.Abs(ImageFilesPath.Count / ItemsPerPage));
        }

        partial void OnImageCollectionChanged(ObservableCollection<ImageItem> value)
        {
            CurrentPageString = $"Current page: {CurrentPage + 1}";
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManipulator as ICancellableService)?.CancelCurrentTask();
        }

        partial void OnIsUiEnabledChanged(bool value)
        {
            if (value == true)
            {
                IsCancelEnabled = false;
            }
            else
            {
                IsCancelEnabled = true;
            }
        }
    }
}