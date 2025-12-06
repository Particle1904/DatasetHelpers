using Microsoft.ML.OnnxRuntime.Tensors;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FlorenceTwoLab.Core;

public class ImageProcessor
{
    private const int ImageSize = 768;
    private const float RescaleFactor = 1.0f / 255.0f;
    private static readonly float[] ImageMean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] ImageStd = [0.229f, 0.224f, 0.225f];

    /// <summary>
    /// Processes the input image by resizing and normalizing it into a tensor format.
    /// </summary>
    /// <param name="image">The input <see cref="Image"/> to process.</param>
    /// <param name="padToSquare">Indicates whether to pad the image to a square aspect ratio. If false, the image is stretched instead.</param>
    /// <returns>
    /// A <see cref="DenseTensor{T}"/> containing the normalized image data
    /// in the [1, 3, height, width] format suitable for ONNX models.
    /// </returns>
    /// <remarks>
    /// The image is cloned internally to avoid modifying the original input. The processed image is always resized to 768x768 pixels.
    /// </remarks>
    public DenseTensor<float> ProcessImage(Image image, bool padToSquare = true)
    {
        // Clone the image to avoid modifying the original
        using Image<Rgb24> processedImage = image.CloneAs<Rgb24>();

        // Resize to square (768x768)
        ResizeImage(processedImage, padToSquare);

        return CreateNormalizedTensor(processedImage);
    }

    /// <summary>
    /// Resizes the given image to a fixed square size of 768x768 pixels.
    /// </summary>
    /// <param name="image">The image to resize. This method modifies the image in place.</param>
    /// <param name="padToSquare">
    /// Indicates whether to preserve the aspect ratio by padding the image.
    /// If false, the image is stretched to fit the target size.
    /// </param>
    /// <remarks>
    /// When padding is enabled, black padding is used to maintain the aspect ratio.
    /// </remarks>
    private void ResizeImage(Image<Rgb24> image, bool padToSquare)
    {
        image.Mutate(ctx =>
        {
            ctx.Resize(new ResizeOptions
            {
                Size = new Size(ImageSize, ImageSize),
                Mode = padToSquare ? ResizeMode.Pad : ResizeMode.Stretch, // Pad to maintain aspect ratio
                PadColor = Color.Black // Use black for padding
            });
        });
    }

    /// <summary>
    /// Creates a normalized tensor from the given image.
    /// </summary>
    /// <param name="image">The RGB image to convert to a tensor. The image must be 768x768 pixels.</param>
    /// <returns>
    /// A <see cref="DenseTensor{T}"/> 
    /// with shape [1, 3, 768, 768], where the pixel values are normalized using standard ImageNet statistics.
    /// </returns>
    /// <remarks>
    /// Pixel values are normalized to the [0,1] range and then standardized using the ImageNet mean and standard deviation.
    /// The tensor is formatted in CHW order (channel, height, width).
    /// </remarks>
    private DenseTensor<float> CreateNormalizedTensor(Image<Rgb24> image)
    {
        // Create tensor with shape [count, channels, height, width]
        DenseTensor<float> tensor = new DenseTensor<float>([1, 3, ImageSize, ImageSize]);

        // Process image pixels and fill tensor
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                for (int x = 0; x < pixelRow.Length; x++)
                {
                    // Get RGB values
                    Rgb24 pixel = pixelRow[x];

                    // Convert to float and normalize [0,1]
                    // Apply mean/std normalization
                    // Store in CHW format
                    tensor[0, 0, y, x] = (pixel.R * RescaleFactor - ImageMean[0]) / ImageStd[0]; // Red channel
                    tensor[0, 1, y, x] = (pixel.G * RescaleFactor - ImageMean[1]) / ImageStd[1]; // Green channel
                    tensor[0, 2, y, x] = (pixel.B * RescaleFactor - ImageMean[2]) / ImageStd[2]; // Blue channel
                }
            }
        });

        return tensor;
    }
}
