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
        private readonly IFileManipulatorService _fileManipulator;

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

        private readonly Stopwatch _timer;
        public TimeSpan ElapsedTime => _timer.Elapsed;

        public SortImagesViewModel(IFileManipulatorService fileManipulator, ILoggerService logger, IConfigsService configs) : base(logger, configs)
        {
            _fileManipulator = fileManipulator;

            DiscardedFolderPath = _configs.Configurations.DiscardedFolder;
            _fileManipulator.CreateFolderIfNotExist(DiscardedFolderPath);
            OutputFolderPath = _configs.Configurations.SelectedFolder;
            _fileManipulator.CreateFolderIfNotExist(OutputFolderPath);
            BackupFolderPath = _configs.Configurations.BackupFolder;
            _fileManipulator.CreateFolderIfNotExist(BackupFolderPath);

            (_fileManipulator as INotifyProgress).TotalFilesChanged += (sender, args) =>
            {
                SortProgress = ResetProgress(SortProgress);
                SortProgress.TotalFiles = args;
            };
            (_fileManipulator as INotifyProgress).ProgressUpdated += (sender, args) => SortProgress.UpdateProgress();

            Dimension = SupportedDimensions.Resolution512x512;
            BackupImages = false;

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
                TaskStatus = ProcessingStatus.BackingUp;
                await _fileManipulator.BackupFilesAsync(InputFolderPath, BackupFolderPath);
            }

            TaskStatus = ProcessingStatus.Running;

            try
            {
                await _fileManipulator.SortImagesAsync(InputFolderPath, DiscardedFolderPath, OutputFolderPath, Dimension);
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
    }
}
