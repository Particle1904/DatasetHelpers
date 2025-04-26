namespace Interfaces.MachineLearning.SAM2
{
    public interface ISAM2Encoder
    {
        public Task EncodeImageEmbeds(string inputImagePath);
    }
}
