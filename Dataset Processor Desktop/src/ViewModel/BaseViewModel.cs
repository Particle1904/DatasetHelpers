using Dataset_Processor_Desktop.src.Enums;

using System.ComponentModel;

namespace Dataset_Processor_Desktop.src.ViewModel
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        private bool _isUiLocked = false;
        public bool IsUiLocked
        {
            get => _isUiLocked;
            set
            {
                _isUiLocked = true;
                OnPropertyChanged(nameof(IsUiLocked));
            }
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
