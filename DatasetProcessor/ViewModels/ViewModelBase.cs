using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using SmartData.Lib.Interfaces;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    protected ILoggerService _logger;
    protected readonly IConfigsService _configs;
    protected IClipboard _clipboard;
    protected IStorageProvider _storageProvider;
    private FolderPickerOpenOptions _folderPickerOptions;

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

        _folderPickerOptions = new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = "Select folder with Dataset files"
        };
    }

    /// <summary>
    /// Opens a folder in the default file explorer.
    /// </summary>
    [RelayCommand]
    protected void OpenFolderInExplorer(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Logger.LatestLogMessage = $"A folder needs to be selected before opening it in the explorer.";
            return;
        }

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Logger.LatestLogMessage = $"Folder does not exist: {folderPath}.";
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sh",
                        Arguments = $"-c 'xdg-open {folderPath}'",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();
                int exitCode = process.ExitCode;
                if (exitCode != 0)
                {
                    Logger.LatestLogMessage = "Unable to open the folder!";
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", folderPath);
            }
        }
        catch
        {
            Logger.LatestLogMessage = "Unable to open the folder!";
        }
    }

    protected async Task<string> SelectFolderPath()
    {
        string resultFolder = string.Empty;

        IReadOnlyList<IStorageFolder> result = await _storageProvider.OpenFolderPickerAsync(_folderPickerOptions);
        if (result.Count > 0)
        {
            resultFolder = result[0].Path.LocalPath;
        }

        return resultFolder;
    }

    protected async Task CopyToClipboard(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await _clipboard.SetTextAsync(text);
        }
    }

    public void Initialize(IClipboard clipboard, IStorageProvider storageProvider)
    {
        _clipboard = clipboard;
        _storageProvider = storageProvider;
    }
}
