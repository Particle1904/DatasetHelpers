using Avalonia.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using System.IO;

namespace DatasetProcessor.src.Models
{
    public partial class ImageItem : ObservableObject
    {
        public string FileName { get; set; }
        public string FilePath { get; private set; }

        [ObservableProperty]
        private Bitmap? _bitmap;

        [ObservableProperty]
        private bool _isLoading;

        public ImageItem(string filePath)
        {
            FileName = Path.GetFileName(filePath);
            FilePath = filePath;
            IsLoading = true;
        }
    }
}
