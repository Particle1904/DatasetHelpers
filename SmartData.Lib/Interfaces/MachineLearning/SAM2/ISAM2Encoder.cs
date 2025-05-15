using SmartData.Lib.Models.MachineLearning.SAM2;

namespace Interfaces.MachineLearning.SAM2
{
    public interface ISAM2Encoder
    {
        public Task<SAM2EncoderOutputData> EncodeImageEmbeds(string inputImagePath);
    }
}
