using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Interfaces;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    protected readonly ILoggerService _logger;
    protected readonly IConfigsService _configs;
    protected IClipboard _clipboard;
    protected IStorageProvider _storageProvider;

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

    public bool IsActive { get; set; } = false;

    public ViewModelBase(ILoggerService logger,
                         IConfigsService configs)
    {
        _logger = logger;
        _configs = configs;
    }

    public void Initialize(IClipboard clipboard, IStorageProvider storageProvider)
    {
        _clipboard = clipboard;
        _storageProvider = storageProvider;
    }

    protected void OpenFolderInExplorer(string folderPath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", folderPath);
            }
        }
        catch
        {
            _logger.LatestLogMessage = "Unable to open the folder!";
        }
    }

    protected async Task CopyToClipboard(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await _clipboard.SetTextAsync(text);
        }
    }
}
