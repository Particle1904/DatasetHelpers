using System.ComponentModel;

namespace SmartData.Lib.Helpers
{
    /// <summary>
    /// Represents a progress tracker for tracking the completion of a set of tasks.
    /// </summary>
    public class Progress : INotifyPropertyChanged
    {
        private int _totalFiles;
        private int _filesProcessed;
        private int _percentComplete;

        /// <summary>
        /// Event raised when a property of the Progress object changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the total number of files to process.
        /// </summary>
        public int TotalFiles
        {
            get => _totalFiles;
            set
            {
                _totalFiles = value;
                OnPropertyChanged(nameof(TotalFiles));
            }
        }

        /// <summary>
        /// Gets or sets the percentage of completion as an integer (0-100).
        /// </summary>
        public int PercentComplete
        {
            get => _percentComplete;
            set
            {
                _percentComplete = value;
                OnPropertyChanged(nameof(PercentComplete));
                OnPropertyChanged(nameof(PercentFloat));
            }
        }

        /// <summary>
        /// Gets the percentage of completion as a floating-point number (0.0-1.0).
        /// </summary>
        public float PercentFloat
        {
            get => (float)_percentComplete / 100;
        }

        /// <summary>
        /// Initializes a new instance of the Progress class with default values.
        /// </summary>
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

        /// <summary>
        /// Resets the progress tracker to its initial state.
        /// </summary>
        public void Reset()
        {
            TotalFiles = 0;
            PercentComplete = 0;
            _filesProcessed = 0;
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
