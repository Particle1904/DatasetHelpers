using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

using DatasetProcessor.ViewModels;

using SmartData.Lib.Enums;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DatasetProcessor.Views
{
    public partial class MetadataView : UserControl
    {
        private static readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".ico", ".webp" };

        private MetadataViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the MetadataView class.
        /// </summary>
        public MetadataView()
        {
            InitializeComponent();

            AddHandler(DragDrop.DropEvent, OnDrop);
        }

        /// <summary>
        /// Event handler for the Drop event. Handles dropping of files onto the view.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        private async Task OnDrop(object _, DragEventArgs e)
        {
            if (_viewModel.IsGenerating)
            {
                _viewModel.Logger.SetLatestLogMessage("Already reading a file! Try again when the current file is done processing.",
                    LogMessageColor.Warning);
                return;
            }

            string fileOrigin = string.Empty;
            try
            {
                if (e.Data.Contains(DataFormats.Text))
                {
                    fileOrigin = "browser";

                    string text = e.Data.GetText();
                    await HandleFilesFromBrowser(text);
                }
                else
                {
                    fileOrigin = "file explorer";

                    IEnumerable<IStorageItem> files = e.Data.GetFiles();
                    await HandleFilesFromExplorer(files);
                }
            }
            catch
            {
                _viewModel.Logger.SetLatestLogMessage($"An error occured while trying to read the image file from the {fileOrigin}.",
                    LogMessageColor.Error);
            }
        }

        /// <summary>
        /// Handles files dragged from the browser, if they are valid images.
        /// </summary>
        /// <param name="e">The DragEventArgs containing the dropped data.</param>
        private async Task HandleFilesFromBrowser(string text)
        {
            if (IsUrl(text))
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(text);
                    if (response.IsSuccessStatusCode && response.Content.Headers.ContentType.MediaType.StartsWith("image"))
                    {
                        Stream fileStream = await response.Content.ReadAsStreamAsync();
                        await _viewModel.OpenFileAsync(fileStream);
                    }
                }
            }
        }

        /// <summary>
        /// Handles files dragged from the file explorer, if they are valid images.
        /// </summary>
        /// <param name="e">The DragEventArgs containing the dropped data.</param>
        private async Task HandleFilesFromExplorer(IEnumerable<IStorageItem> files)
        {
            if (files != null && files.Any())
            {
                IStorageItem firstItem = files.FirstOrDefault();
                if (IsImage(firstItem.Path.LocalPath))
                {
                    using (Stream fileStream = File.OpenRead(firstItem.Path.LocalPath))
                    {
                        await _viewModel.OpenFileAsync(fileStream);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the given file path represents a valid image.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>True if the file is a valid image; otherwise, false.</returns>
        private bool IsImage(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            if (string.IsNullOrEmpty(extension) || !_imageExtensions.Any(item => item.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the given text represents a valid URL.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <returns>True if the text is a valid URL; otherwise, false.</returns>
        private bool IsUrl(string text)
        {
            return Uri.TryCreate(text, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Overrides the DataContextChanged method to update the associated view model.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = (MetadataViewModel)DataContext;

            base.OnDataContextChanged(e);
        }
    }
}
