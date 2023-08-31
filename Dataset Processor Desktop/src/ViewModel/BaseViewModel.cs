using Dataset_Processor_Desktop.src.Enums;
using Dataset_Processor_Desktop.src.Interfaces;

using SmartData.Lib.Interfaces;

using System.ComponentModel;

using TextCopy;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        protected readonly IFolderPickerService _folderPickerService;
        protected readonly ILoggerService _loggerService;
        protected readonly IConfigsService _configsService;

        public IConfigsService ConfigsService
        {
            get => _configsService;
        }

        private bool _isUiLocked = false;
        public bool IsUiLocked
        {
            get => _isUiLocked;
            set
            {
                _isUiLocked = value;
                OnPropertyChanged(nameof(IsUiLocked));
            }
        }

        public string TaskStatusString
        {
            get
            {
                switch (TaskStatus)
                {
                    case ProcessingStatus.Idle:
                        return "Task status: Idle. Waiting for user input.";
                    case ProcessingStatus.Running:
                        return "Task status: Processing. Please wait.";
                    case ProcessingStatus.Finished:
                        return "Task status: Finished.";
                    case ProcessingStatus.BackingUp:
                        return "Backing up files before the sorting process.";
                    case ProcessingStatus.LoadingModel:
                        return "Loading Model for tag generation.";
                    default:
                        return "Task status: Idle. Waiting for user input.";
                }
            }
        }

        protected ProcessingStatus _taskStatus;
        public ProcessingStatus TaskStatus
        {
            get => _taskStatus;
            set
            {
                _taskStatus = value;
                OnPropertyChanged(nameof(TaskStatus));
                OnPropertyChanged(nameof(TaskStatusString));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsActive { get; set; } = false;

        public BaseViewModel()
        {
            _folderPickerService = Application.Current.Handler.MauiContext.Services.GetService<IFolderPickerService>();
            _loggerService = Application.Current.Handler.MauiContext.Services.GetService<ILoggerService>();
            _configsService = Application.Current.Handler.MauiContext.Services.GetService<IConfigsService>();
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            MainThread.InvokeOnMainThreadAsync(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        protected async Task OpenFolderAsync(string folderPath)
        {
            try
            {
                await _folderPickerService.OpenFolderInExplorerAsync(folderPath);
            }
            catch
            {
                _loggerService.LatestLogMessage = "Unable to open the folder!";
            }
        }

        protected async Task CopyToClipboard(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                await ClipboardService.SetTextAsync(text);
            }
        }
    }
}
