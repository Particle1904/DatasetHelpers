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
using System.Threading;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class GalleryViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;
        private readonly IImageProcessorService _imageProcessor;

        private const int ItemsPerPage = 200;

        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(Environment.ProcessorCount);
        private CancellationTokenSource _loadingCancellationTokenSource = new CancellationTokenSource();

        [ObservableProperty]
        private string _inputFolderPath;

        [ObservableProperty]
        private List<string> _imageFiles;
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

        public GalleryViewModel(IFileManagerService fileManager, IImageProcessorService imageProcessor, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManager = fileManager;
            _imageProcessor = imageProcessor;

            (_fileManager as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                GalleryProcessingProgress = ResetProgress(GalleryProcessingProgress);
                GalleryProcessingProgress.TotalFiles = args;
            };
            (_fileManager as INotifyProgress).ProgressUpdated += (sender, args) => GalleryProcessingProgress.UpdateProgress();

            InputFolderPath = Configs.Configurations.GalleryConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            MaxImageSize = Math.Clamp(Configs.Configurations.GalleryConfigs.ImageDisplaySize, 256, 576);

            IsUiEnabled = true;
            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
            ImageCollection = new ObservableCollection<ImageItem>();
            SelectedImageItems = new ObservableCollection<ImageItem>();
            CurrentPage = 0;
            CurrentPageString = string.Empty;
        }

        /// <summary>
        /// Selects the input folder for the gallery and loads images from it asynchronously.
        /// </summary>
        /// <returns></returns>
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
            if (_loadingCancellationTokenSource != null)
            {
                _loadingCancellationTokenSource.Cancel();
                _loadingCancellationTokenSource.Dispose();
            }
            _loadingCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _loadingCancellationTokenSource.Token;

            IsUiEnabled = false;
            IsCancelEnabled = true;

            _timer.Restart();
            DispatcherTimer uiTImer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            uiTImer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            uiTImer.Start();

            TaskStatus = ProcessingStatus.Running;
            ImageCollection.Clear();
            SelectedImageItems.Clear();

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        List<string> files = _fileManager.GetImageFiles(InputFolderPath);
                        ImageFiles = files.Where(x => !x.Contains("_mask")).ToList();

                        try
                        {
                            ImageFiles = ImageFiles.OrderBy(x =>
                            {
                                string name = Path.GetFileNameWithoutExtension(x);
                                return int.TryParse(name, out int val) ? val : int.MaxValue;
                            }).ToList();
                        }
                        catch { /* Failed to sort numerically. Ignore sort errors */ }
                    }
                    catch (Exception ex)
                    {
                        Logger.SetLatestLogMessage($"Error listing files: {ex.Message}", LogMessageColor.Error);
                        ImageFiles = new List<string>();
                    }
                }, cancellationToken);

                if (ImageFiles.Count == 0)
                {
                    uiTImer.Stop();
                    _timer.Stop();
                    return;
                }

                int startIndex = CurrentPage * ItemsPerPage;
                int endIndex = Math.Min(startIndex + ItemsPerPage, ImageFiles.Count);

                if (startIndex >= ImageFiles.Count)
                {
                    CurrentPage = 0;
                    startIndex = 0;
                    endIndex = Math.Min(ItemsPerPage, ImageFiles.Count);
                }

                List<string> visibleImages = ImageFiles.GetRange(startIndex, endIndex - startIndex);
                GalleryProcessingProgress.TotalFiles = visibleImages.Count;
                GalleryProcessingProgress.Reset();

                Logger.SetLatestLogMessage("Loading images.", LogMessageColor.Informational);

                List<ImageItem> placeholders = new List<ImageItem>(visibleImages.Count);
                foreach (string file in visibleImages)
                {
                    placeholders.Add(new ImageItem(file));
                }
                ImageCollection = new ObservableCollection<ImageItem>(placeholders);

                List<Task> loadingTasks = new List<Task>();
                foreach (ImageItem item in ImageCollection)
                {
                    loadingTasks.Add(LoadThumbnailAsync(item, cancellationToken));
                }

                await Task.WhenAll(loadingTasks);
                Logger.SetLatestLogMessage("Finished loading images.", LogMessageColor.Informational, false);
            }
            catch (OperationCanceledException)
            {
                Logger.SetLatestLogMessage("Loading cancelled!", LogMessageColor.Informational, false);
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage("Something went wrong while trying to load the images. Error log will be saved inside the logs folder.", LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                TaskStatus = ProcessingStatus.Finished;
                IsUiEnabled = true;
                IsCancelEnabled = false;
            }

            uiTImer.Stop();
            _timer.Stop();
        }

        private async Task LoadThumbnailAsync(ImageItem image, CancellationToken token)
        {
            await _loadingSemaphore.WaitAsync(token);
            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                using (MemoryStream memoryStream = await _imageProcessor.GetThumbnailStreamAsync(image.FilePath, MaxImageSize))
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        image.Bitmap = new Bitmap(memoryStream);
                        image.IsLoading = false;
                        GalleryProcessingProgress.UpdateProgress();
                    });
                }
            }
            catch (Exception exception)
            {
                Logger.SetLatestLogMessage("Something went wrong while trying to load the images. Error log will be saved inside the logs folder.", LogMessageColor.Error);
                await Logger.SaveExceptionStackTrace(exception);
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        /// <summary>
        /// Navigates to a specific page in the image list.
        /// </summary>
        /// <param name="parameter">The navigation parameter indicating the page index.</param>
        [RelayCommand]
        private async Task GoToItem(string parameter)
        {
            if (!int.TryParse(parameter, out int parameterInt))
            {
                return;
            }

            if (ImageFiles?.Count == 0)
            {
                return;
            }

            try
            {
                CurrentPage += parameterInt;
                await LoadImagesFromInputFolder();
            }
            catch
            {
                Logger.SetLatestLogMessage("An error occurred while loading the image.", LogMessageColor.Error);
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

            _timer.Restart();
            DispatcherTimer uiTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            uiTimer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            uiTimer.Start();

            TaskStatus = ProcessingStatus.Running;
            try
            {
                List<string> filesForDeletion = SelectedImageItems.Select(x => x.FileName).ToList();

                await _fileManager.DeleteFilesAsync(InputFolderPath, filesForDeletion);
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

                uiTimer.Stop();
                _timer.Stop();

                // Clear the gallery and load the images again
                ImageCollection.Clear();
                SelectedImageItems.Clear();
                await LoadImagesFromInputFolder();
            }
        }

        /// <summary>
        /// Handles the change in the maximum image size and updates the display string accordingly.
        /// </summary>
        /// <param name="value"></param>
        partial void OnMaxImageSizeChanged(int value)
        {
            MaxImageSizeString = $"{value}px";
        }

        /// <summary>
        /// Handles the change in the current page and updates the current page string accordingly.
        /// </summary>
        /// <param name="value"></param>
        partial void OnCurrentPageChanged(int value)
        {
            CurrentPageString = $"Current page: {CurrentPage + 1}";
            CurrentPage = Math.Clamp(CurrentPage, 0, Math.Abs(ImageFiles.Count / ItemsPerPage));
        }

        /// <summary>
        /// Handles the change in the image collection and updates the current page string accordingly.
        /// </summary>
        /// <param name="value"></param>
        partial void OnImageCollectionChanged(ObservableCollection<ImageItem> value)
        {
            CurrentPageString = $"Current page: {CurrentPage + 1}";
        }

        /// <summary>
        /// Cancels the current task if it is cancellable.
        /// </summary>
        [RelayCommand]
        private void CancelTask()
        {
            (_fileManager as ICancellableService)?.CancelCurrentTask();
        }

        /// <summary>
        /// Handles the change in the UI enabled state and updates the cancel button state accordingly.
        /// </summary>
        /// <param name="value"></param>
        partial void OnIsUiEnabledChanged(bool value)
        {
            if (value)
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