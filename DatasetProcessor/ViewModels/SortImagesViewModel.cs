using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels
{
    public partial class SortImagesViewModel : BaseViewModel
    {
        private readonly IFileManagerService _fileManager;

        [ObservableProperty]
        private string _inputFolderPath;
        [ObservableProperty]
        private string _outputFolderPath;
        [ObservableProperty]
        private string _discardedFolderPath;
        [ObservableProperty]
        private string _backupFolderPath;
        [ObservableProperty]
        private bool _backupImages;
        [ObservableProperty]
        private SupportedDimensions _dimension;
        [ObservableProperty]
        private Progress _sortProgress;
        [ObservableProperty]
        private bool _isUiEnabled;
        [ObservableProperty]
        private bool _isCancelEnabled;

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        public SortImagesViewModel(IFileManagerService fileManager, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManager = fileManager;

            (_fileManager as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                SortProgress = ResetProgress(SortProgress);
                SortProgress.TotalFiles = args;
            };
            (_fileManager as INotifyProgress).ProgressUpdated += (sender, args) => SortProgress.UpdateProgress();

            InputFolderPath = _configs.Configurations.SortImagesConfigs.InputFolder;
            _fileManager.CreateFolderIfNotExist(InputFolderPath);
            OutputFolderPath = _configs.Configurations.SortImagesConfigs.OutputFolder;
            _fileManager.CreateFolderIfNotExist(OutputFolderPath);
            BackupFolderPath = _configs.Configurations.SortImagesConfigs.BackupFolder;
            _fileManager.CreateFolderIfNotExist(BackupFolderPath);
            DiscardedFolderPath = _configs.Configurations.SortImagesConfigs.DiscardedFolder;
            _fileManager.CreateFolderIfNotExist(DiscardedFolderPath);
            Dimension = _configs.Configurations.SortImagesConfigs.DimensionSizeForDiscarded;
            BackupImages = _configs.Configurations.SortImagesConfigs.BackupBeforeProcessing;

            _timer = new Stopwatch();
            TaskStatus = ProcessingStatus.Idle;
            IsUiEnabled = true;
        }

        [RelayCommand]
        private async Task SelectInputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                InputFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectOutputFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                OutputFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectDiscardedFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                DiscardedFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SelectBackupFolderAsync()
        {
            string result = await SelectFolderPath();
            if (!string.IsNullOrEmpty(result))
            {
                BackupFolderPath = result;
            }
        }

        [RelayCommand]
        private async Task SortImagesAsync()
        {
            IsUiEnabled = false;

            SortProgress = ResetProgress(SortProgress);

            _timer.Reset();
            _timer.Start();
            DispatcherTimer timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (sender, eventArgs) => OnPropertyChanged(nameof(ElapsedTime));
            timer.Start();

            if (BackupImages)
            {
                if (string.IsNullOrEmpty(BackupFolderPath))
                {
                    Logger.SetLatestLogMessage($"Please select the backup folder. Otherwise, uncheck the back box! Backup will be skipped at this time.",
                        LogMessageColor.Warning);
                }
                else
                {
                    TaskStatus = ProcessingStatus.BackingUp;
                    await _fileManager.BackupFilesAsync(InputFolderPath, BackupFolderPath);
                }
            }

            TaskStatus = ProcessingStatus.Running;

            try
            {
                await _fileManager.SortImagesAsync(InputFolderPath, DiscardedFolderPath, OutputFolderPath, Dimension);

            }
            catch (ArgumentNullException)
            {
                Logger.SetLatestLogMessage($"Please select the input, output and discarded folders before sorting!",
                    LogMessageColor.Error);
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
            }

            timer.Stop();
            _timer.Stop();
        }

        [RelayCommand]
        private void CancelTask()
        {
            (_fileManager as ICancellableService)?.CancelCurrentTask();
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
