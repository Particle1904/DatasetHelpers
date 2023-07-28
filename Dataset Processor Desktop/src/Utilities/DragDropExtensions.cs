using Microsoft.UI.Xaml;

using System.Runtime.InteropServices.WindowsRuntime;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Dataset_Processor_Desktop.src.Utilities
{
    public static class DragDropExtensions
    {
        public static void RegisterDragDrop(this UIElement element, Func<Stream, Task>? content)
        {
            element.AllowDrop = true;
            element.Drop += async (s, e) =>
            {
                if (e.DataView.Contains(StandardDataFormats.StorageItems) && content is not null)
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    foreach (var item in items)
                    {
                        if (item is StorageFile file)
                        {
                            var buffer = await FileIO.ReadBufferAsync(file);
                            var stream = buffer.AsStream();
                            await content.Invoke(stream);
                        }
                    }
                }
            };
            element.DragOver += OnDragOver;
        }

        public static void UnRegisterDragDrop(this UIElement element)
        {
            element.AllowDrop = false;
            element.DragOver -= OnDragOver;
        }

        private static async void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var deferral = e.GetDeferral();
                var extensions = new List<string> { ".png", ".jpeg", ".jpg" };
                var isAllowed = false;
                var items = await e.DataView.GetStorageItemsAsync();
                foreach (var item in items)
                {
                    if (item is StorageFile file && extensions.Contains(file.FileType))
                    {
                        isAllowed = true;
                        break;
                    }
                }

                e.AcceptedOperation = isAllowed ? Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy : Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                deferral.Complete();
            }

            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }
}
