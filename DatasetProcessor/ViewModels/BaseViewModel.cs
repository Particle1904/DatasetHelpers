using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DatasetProcessor.src.Enums;

using Interfaces;

using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Interfaces;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DatasetProcessor.ViewModels;

/// <summary>
/// The base view model class that provides common functionality for view models.
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    protected ILoggerService _logger;
    [ObservableProperty]
    protected IConfigsService _configs;
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

    /// <summary>
    /// Gets or sets a value indicating whether the view model is active.
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the ViewModelBase class with the provided logger and configuration service.
    /// </summary>
    /// <param name="logger">The logger service for logging messages.</param>
    /// <param name="configs">The configuration service for application settings.</param>
    public BaseViewModel(ILoggerService logger,
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
            Logger.SetLatestLogMessage($"A folder needs to be selected before opening it in the explorer.",
                LogMessageColor.Warning);
            return;
        }

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Logger.SetLatestLogMessage($"Folder does not exist: {folderPath}.", LogMessageColor.Warning);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("open", folderPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", folderPath);
            }
        }
        catch
        {
            Logger.SetLatestLogMessage("Unable to open the folder!", LogMessageColor.Warning);
        }
    }

    /// <summary>
    /// Selects a folder path using the folder picker dialog and returns the selected folder's path.
    /// </summary>
    /// <returns>The path of the selected folder, or an empty string if no folder was selected.</returns>
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

    /// <summary>
    /// Copies the provided text to the clipboard asynchronously.
    /// </summary>
    /// <param name="text">The text to be copied to the clipboard.</param>
    protected async Task CopyToClipboard(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await _clipboard.SetTextAsync(text);
        }
    }

    /// <summary>
    /// Updates the progress tracker to reflect the completion of one file.
    /// </summary>
    protected static Progress ResetProgress(Progress progress)
    {
        if (progress == null)
        {
            return new Progress();
        }
        if (progress.PercentFloat != 0f)
        {
            progress.Reset();
        }

        return progress;
    }

    /// <summary>
    /// Download model files if necessary.
    /// </summary>
    /// <param name="model">The model to be downloaded.</param>
    protected async Task DownloadModelFiles(IModelManagerService modelManager, AvailableModels model)
    {
        if (modelManager.FileNeedsToBeDownloaded(model))
        {
            await modelManager.DownloadModelFileAsync(model);
        }
    }

    [RelayCommand]
    protected async Task OpenWebPage(string webAddress)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = webAddress,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", webAddress);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", webAddress);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system");
            }
        }
        catch (Exception exception)
        {
            Logger.SetLatestLogMessage($"Something went wrong! Error log will be saved inside the logs folder.",
                LogMessageColor.Error);
            await Logger.SaveExceptionStackTrace(exception);
        }
    }


    /// <summary>
    /// Initializes the view model with the clipboard and storage provider.
    /// </summary>
    /// <param name="clipboard">The clipboard provider for managing clipboard operations.</param>
    /// <param name="storageProvider">The storage provider for handling storage-related operations.</param>
    public void Initialize(IClipboard clipboard, IStorageProvider storageProvider)
    {
        _clipboard = clipboard;
        _storageProvider = storageProvider;
    }
}
