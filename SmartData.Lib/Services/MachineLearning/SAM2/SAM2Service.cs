using Interfaces.MachineLearning;

using SmartData.Lib.Interfaces;

namespace SmartData.Lib.Services.MachineLearning.SAM2
{
    class SAM2Service : /*ISAM2Service,*/ INotifyProgress, IUnloadModel
    {
        private readonly IImageProcessorService _imageProcessor;

        public event EventHandler<int> TotalFilesChanged;
        public event EventHandler ProgressUpdated;

        private SAM2Encoder _encoder;
        private SAM2Decoder _decoder;

        public SAM2Service(IImageProcessorService imageProcessor, string encoderModelPath, string decoderModelPath)
        {
            _imageProcessor = imageProcessor;

            _encoder = new SAM2Encoder(_imageProcessor, encoderModelPath);
            _decoder = new SAM2Decoder(decoderModelPath);
        }

        public void UnloadAIModel()
        {
            throw new NotImplementedException();
        }
    }
}
