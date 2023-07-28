using CommunityToolkit.Maui.Storage;

using Dataset_Processor_Desktop.src.Interfaces;

namespace Dataset_Processor_Desktop.src.Services
{
    public class FolderPickerService : IFolderPickerService
    {
        public CancellationToken CancelToken { get; set; }

        public FolderPickerService()
        {
            CancelToken = new CancellationToken();
        }

        public async Task<string> PickFolderAsync()
        {
            FolderPickerResult result = await FolderPicker.Default.PickAsync(CancelToken);
            if (!result.IsSuccessful)
            {
                return "";
            }

            return result.Folder.Path;
        }

        public async Task OpenFolderInExplorerAsync(string folderPath)
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                await Launcher.OpenAsync(folderPath);
            }
        }
    }
}