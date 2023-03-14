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
            set => _totalFiles = value;
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

        public void UpdateProgress()
        {
            _filesProcessed++;
            _percentComplete = (int)((float)_filesProcessed / _totalFiles * 100);
            OnPropertyChanged(nameof(PercentComplete));
            OnPropertyChanged(nameof(PercentFloat));
        }

        public virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
