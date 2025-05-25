using System.Drawing;

namespace Interfaces.MachineLearning.SAM2
{
    public interface ISAM2Service
    {
        public Task SegmentObjectFromPointAsync(string inputPath, Point point, string outputPath);
        public Task SegmentObjectFromBoundingBoxAsync(string inputPath, Point topLeft, Point bottomRight, string outputPath);
        public Task<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.L8>> SegmentObjectFromBoundingBoxAsync(string inputPath,
            Point topLeftPoint, Point bottomRightPoint);
    }
}