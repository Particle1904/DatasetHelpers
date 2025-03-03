using Interfaces.MachineLearning;

using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    class SAM2Service : /*ISAM2Service,*/ INotifyProgress, IUnloadModel
    {
        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        public SAM2Service(string modelPath)
        {

        }

        public void UnloadAIModel()
        {
            throw new NotImplementedException();
        }
    }
}
