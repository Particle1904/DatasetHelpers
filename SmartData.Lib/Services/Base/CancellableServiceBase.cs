using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services.Base
{
    public abstract class CancellableServiceBase : ICancellableService
    {
        protected CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Cancel the current running task.
        /// </summary>
        public void CancelCurrentTask()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }
}