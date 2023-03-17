using System.ComponentModel;

namespace SmartData.Lib.Helpers
{
    public class Progress : INotifyPropertyChanged
    {
        private int _totalFiles;
        private int _filesProcessed;
        private int _percentComplete;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int TotalFiles
        {
            get => _totalFiles;
            set
            {
                _totalFiles = value;
                OnPropertyChanged(nameof(TotalFiles));
            }
        }

        public int PercentComplete
        {
            get => _percentComplete / 100;
            set
            {
                _percentComplete = value;
                OnPropertyChanged(nameof(PercentComplete));
            }
        }

        public float PercentFloat
        {
            get => (float)_percentComplete / 100;
        }

        public Progress()
        {
            _filesProcessed = 0;
            _percentComplete = 0;
        }

        /// <summary>
        /// Updates the progress tracker to reflect the completion of one file.
        /// </summary>
        public void UpdateProgress()
        {
            _filesProcessed++;
            _percentComplete = (int)((float)_filesProcessed / _totalFiles * 100);
            OnPropertyChanged(nameof(PercentComplete));
            OnPropertyChanged(nameof(PercentFloat));
        }

        public void Reset()
        {
            _totalFiles = 0;
            TotalFiles = 0;
            PercentComplete = 0;

        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        public virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
