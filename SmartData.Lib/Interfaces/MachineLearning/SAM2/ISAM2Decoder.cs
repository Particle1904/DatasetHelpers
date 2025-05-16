using SmartData.Lib.Models.MachineLearning.SAM2;

using System.Drawing;

namespace Interfaces.MachineLearning.SAM2
{
    public interface ISAM2Decoder
    {
        public Task<SAM2DecoderOutputData> GenerateImageMasksAsync(string imagePath, SAM2EncoderOutputData imageEmbeds, Point point);
        public Task<SAM2DecoderOutputData> GenerateImageMasksAsync(string imagePath, SAM2EncoderOutputData imageEmbeds, Point topLeftPoint, Point bottomRightPoint);
    }
}
