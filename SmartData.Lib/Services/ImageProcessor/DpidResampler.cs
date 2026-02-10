using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using System.Buffers;

namespace SmartData.Lib.Services.ImageProcessor
{
    /// <summary>
    /// Implementation based on https://github.com/Mishini/dpid
    /// </summary>
    public class DpidResampler
    {
        private static readonly float[,] Kernel =
        {
            { 1.0f, 2.0f, 1.0f },
            { 2.0f, 4.0f, 2.0f },
            { 1.0f, 2.0f, 1.0f }
        };

        private readonly ParallelOptions _parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2)
        };

        /// <summary>
        /// Resamples an RGB image to the specified output size using the DPID algorithm with center-crop alignment.
        /// </summary>
        /// <param name="sourceImage">The source image to be resampled.</param>
        /// <param name="outputWidth">The desired width of the output image.</param>
        /// <param name="outputHeight">The desired height of the output image.</param>
        /// <param name="lambda">The DPID sharpness parameter controlling detail preservation.</param>
        /// <returns>A new Image<Rgba32> containing the resampled result.</returns>
        public Image<Rgba32> DpidResampleRgb(Image<Rgba32> sourceImage, int outputWidth, int outputHeight, float lambda)
        {
            int inputWidth = sourceImage.Width;
            int inputHeight = sourceImage.Height;

            int inputBufferLength = inputWidth * inputHeight * 3;
            int outputBufferLength = outputWidth * outputHeight * 3;

            float[] inputPixels = ArrayPool<float>.Shared.Rent(inputBufferLength);
            float[] outputPixels = ArrayPool<float>.Shared.Rent(outputBufferLength);

            try
            {
                sourceImage.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> row = accessor.GetRowSpan(y);
                        int rowOffset = y * inputWidth * 3;

                        for (int x = 0; x < row.Length; x++)
                        {
                            int index = rowOffset + (x * 3);
                            Rgba32 pixel = row[x];
                            inputPixels[index] = pixel.R / 255.0f;
                            inputPixels[index + 1] = pixel.G / 255.0f;
                            inputPixels[index + 2] = pixel.B / 255.0f;
                        }
                    }
                });

                float scaleX = (float)inputWidth / outputWidth;
                float scaleY = (float)inputHeight / outputHeight;

                float patchSize = Math.Min(scaleX, scaleY);

                float visibleSourceWidth = outputWidth * patchSize;
                float visibleSourceHeight = outputHeight * patchSize;

                float offsetX = (inputWidth - visibleSourceWidth) / 2.0f;
                float offsetY = (inputHeight - visibleSourceHeight) / 2.0f;

                Parallel.For(0, outputHeight, _parallelOptions, outputY =>
                {
                    float k00 = Kernel[0, 0], k01 = Kernel[0, 1], k02 = Kernel[0, 2];
                    float k10 = Kernel[1, 0], k11 = Kernel[1, 1], k12 = Kernel[1, 2];
                    float k20 = Kernel[2, 0], k21 = Kernel[2, 1], k22 = Kernel[2, 2];

                    for (int outputX = 0; outputX < outputWidth; outputX++)
                    {
                        ProcessPixel(outputX, outputY, outputWidth, outputHeight, inputWidth, inputHeight, inputPixels, outputPixels,
                                     lambda, patchSize, offsetX, offsetY, k00, k01, k02, k10, k11, k12, k20, k21, k22);
                    }
                });

                Image<Rgba32> outputImage = new Image<Rgba32>(outputWidth, outputHeight);
                outputImage.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                        int rowOffset = y * outputWidth * 3;

                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            int index = rowOffset + (x * 3);

                            byte r = (byte)Math.Clamp(outputPixels[index] * 255.0f + 0.5f, 0, 255);
                            byte g = (byte)Math.Clamp(outputPixels[index + 1] * 255.0f + 0.5f, 0, 255);
                            byte b = (byte)Math.Clamp(outputPixels[index + 2] * 255.0f + 0.5f, 0, 255);

                            pixelRow[x] = new Rgba32(r, g, b, 255);
                        }
                    }
                });

                return outputImage;
            }
            finally
            {
                ArrayPool<float>.Shared.Return(inputPixels);
                ArrayPool<float>.Shared.Return(outputPixels);
            }
        }

        /// <summary>
        /// Processes a single output pixel by applying a weighted convolution over a 3x3 neighborhood and
        /// detail-preserving filtering using the input buffer, writing the result to the output buffer.
        /// </summary>
        /// <param name="outX">The X coordinate of the output pixel.</param>
        /// <param name="outY">The Y coordinate of the output pixel.</param>
        /// <param name="outW">The width of the output image.</param>
        /// <param name="outH">The height of the output image.</param>
        /// <param name="inW">The width of the input image.</param>
        /// <param name="inH">The height of the input image.</param>
        /// <param name="inputBuffer">The input buffer containing source pixel data in RGB float format.</param>
        /// <param name="outputBuffer">The output buffer to store the processed pixel data in RGB float format.</param>
        /// <param name="lambda">The exponent used for detail preservation weighting.</param>
        /// <param name="patchSize">The size of the patch corresponding to the output pixel in input space.</param>
        /// <param name="offsetX">The horizontal offset applied to the patch position.</param>
        /// <param name="offsetY">The vertical offset applied to the patch position.</param>
        /// <param name="k00">The kernel weight for the top-left neighbor.</param>
        /// <param name="k01">The kernel weight for the top-center neighbor.</param>
        /// <param name="k02">The kernel weight for the top-right neighbor.</param>
        /// <param name="k10">The kernel weight for the middle-left neighbor.</param>
        /// <param name="k11">The kernel weight for the center neighbor.</param>
        /// <param name="k12">The kernel weight for the middle-right neighbor.</param>
        /// <param name="k20">The kernel weight for the bottom-left neighbor.</param>
        /// <param name="k21">The kernel weight for the bottom-center neighbor.</param>
        /// <param name="k22">The kernel weight for the bottom-right neighbor.</param>
        private void ProcessPixel(int outX, int outY, int outW, int outH, int inW, int inH, float[] inputBuffer, float[] outputBuffer,
                                  float lambda, float patchSize, float offsetX, float offsetY, float k00, float k01, float k02, float k10,
                                  float k11, float k12, float k20, float k21, float k22)
        {
            float combinedRed = 0.0f;
            float combinedGreen = 0.0f;
            float combinedBlue = 0.0f;
            float combinedWeight = 0.0f;

            for (int kernelY = 0; kernelY < 3; kernelY++)
            {
                int neighborY = Math.Clamp(outY + kernelY - 1, 0, outH - 1);

                for (int kernelX = 0; kernelX < 3; kernelX++)
                {
                    int neighborX = Math.Clamp(outX + kernelX - 1, 0, outW - 1);

                    float kernelWeight = ResolveKernelWeight(kernelX, kernelY, k00, k01, k02, k10, k11, k12, k20, k21, k22);

                    float startX = (neighborX * patchSize) + offsetX;
                    float startY = (neighborY * patchSize) + offsetY;
                    float endX = startX + patchSize;
                    float endY = startY + patchSize;

                    int pixelStartX = Math.Max(0, (int)Math.Floor(startX));
                    int pixelStartY = Math.Max(0, (int)Math.Floor(startY));
                    int pixelEndX = Math.Min(inW, (int)Math.Ceiling(endX));
                    int pixelEndY = Math.Min(inH, (int)Math.Ceiling(endY));

                    float areaRed = 0.0f;
                    float areaGreen = 0.0f;
                    float areaBlue = 0.0f;
                    float areaWeight = 0.0f;

                    for (int iy = pixelStartY; iy < pixelEndY; iy++)
                    {
                        int rowOffset = iy * inW;
                        for (int ix = pixelStartX; ix < pixelEndX; ix++)
                        {
                            float coverage = CalculateCoverage(startX, endX, startY, endY, ix, iy);
                            if (coverage <= 1e-6f)
                            {
                                continue;
                            }

                            int index = (rowOffset + ix) * 3;
                            areaRed += inputBuffer[index] * coverage;
                            areaGreen += inputBuffer[index + 1] * coverage;
                            areaBlue += inputBuffer[index + 2] * coverage;
                            areaWeight += coverage;
                        }
                    }

                    if (areaWeight > 0)
                    {
                        float inverseWeight = 1.0f / areaWeight;
                        combinedRed += (areaRed * inverseWeight) * kernelWeight;
                        combinedGreen += (areaGreen * inverseWeight) * kernelWeight;
                        combinedBlue += (areaBlue * inverseWeight) * kernelWeight;
                        combinedWeight += kernelWeight;
                    }
                }
            }

            float meanRed = 0;
            float meanGreen = 0;
            float meanBlue = 0;

            if (combinedWeight > 0)
            {
                float inverseTotalWeight = 1.0f / combinedWeight;
                meanRed = combinedRed * inverseTotalWeight;
                meanGreen = combinedGreen * inverseTotalWeight;
                meanBlue = combinedBlue * inverseTotalWeight;
            }

            float patchStartX = (outX * patchSize) + offsetX;
            float patchStartY = (outY * patchSize) + offsetY;
            float patchEndX = patchStartX + patchSize;
            float patchEndY = patchStartY + patchSize;

            int minX = Math.Max(0, (int)Math.Floor(patchStartX));
            int minY = Math.Max(0, (int)Math.Floor(patchStartY));
            int maxX = Math.Min(inW, (int)Math.Ceiling(patchEndX));
            int maxY = Math.Min(inH, (int)Math.Ceiling(patchEndY));

            float finalRed = 0.0f;
            float finalGreen = 0.0f;
            float finalBlue = 0.0f;
            float finalWeight = 0.0f;

            for (int iy = minY; iy < maxY; iy++)
            {
                int rowOffset = iy * inW;
                for (int ix = minX; ix < maxX; ix++)
                {
                    float coverage = CalculateCoverage(patchStartX, patchEndX, patchStartY, patchEndY, ix, iy);
                    if (coverage <= 1e-6f)
                    {
                        continue;
                    }

                    int index = (rowOffset + ix) * 3;
                    float diffRed = meanRed - inputBuffer[index];
                    float diffGreen = meanGreen - inputBuffer[index + 1];
                    float diffBlue = meanBlue - inputBuffer[index + 2];

                    float distanceSquared = (diffRed * diffRed) + (diffGreen * diffGreen) + (diffBlue * diffBlue);

                    float weightFactor = coverage;
                    if (lambda != 0.0f && distanceSquared > 1e-9f)
                    {
                        weightFactor *= (float)Math.Pow(Math.Sqrt(distanceSquared), lambda);
                    }

                    finalRed += inputBuffer[index] * weightFactor;
                    finalGreen += inputBuffer[index + 1] * weightFactor;
                    finalBlue += inputBuffer[index + 2] * weightFactor;
                    finalWeight += weightFactor;
                }
            }

            int outputIndex = (outY * outW + outX) * 3;
            if (finalWeight > 0.0f)
            {
                outputBuffer[outputIndex] = finalRed / finalWeight;
                outputBuffer[outputIndex + 1] = finalGreen / finalWeight;
                outputBuffer[outputIndex + 2] = finalBlue / finalWeight;
            }
            else
            {
                outputBuffer[outputIndex] = meanRed;
                outputBuffer[outputIndex + 1] = meanGreen;
                outputBuffer[outputIndex + 2] = meanBlue;
            }
        }

        /// <summary>
        /// Returns the kernel weight at the specified (kx, ky) position from a 3x3 matrix of weights.
        /// </summary>
        /// <param name="kx">The x-coordinate index within the kernel matrix.</param>
        /// <param name="ky">The y-coordinate index within the kernel matrix.</param>
        /// <param name="k00">The kernel weight at position (0, 0).</param>
        /// <param name="k01">The kernel weight at position (1, 0).</param>
        /// <param name="k02">The kernel weight at position (2, 0).</param>
        /// <param name="k10">The kernel weight at position (0, 1).</param>
        /// <param name="k11">The kernel weight at position (1, 1).</param>
        /// <param name="k12">The kernel weight at position (2, 1).</param>
        /// <param name="k20">The kernel weight at position (0, 2).</param>
        /// <param name="k21">The kernel weight at position (1, 2).</param>
        /// <param name="k22">The kernel weight at position (2, 2).</param>
        /// <returns>The kernel weight corresponding to the given (kx, ky) indices.</returns>
        private float ResolveKernelWeight(int kx, int ky, float k00, float k01, float k02, float k10, float k11, float k12,
                                          float k20, float k21, float k22)
        {
            if (ky == 0)
            {
                if (kx == 0)
                {
                    return k00;
                }

                if (kx == 1)
                {
                    return k01;
                }

                return k02;
            }

            if (ky == 1)
            {
                if (kx == 0)
                {
                    return k10;
                }
                if (kx == 1)
                {
                    return k11;
                }

                return k12;
            }

            if (kx == 0)
            {
                return k20;
            }

            if (kx == 1)
            {
                return k21;
            }
            return k22;
        }

        /// <summary>
        /// Calculates the fractional coverage of a pixel by a rectangular area defined by the given coordinates.
        /// </summary>
        /// <param name="startX">The left boundary of the rectangle.</param>
        /// <param name="endX">The right boundary of the rectangle.</param>
        /// <param name="startY">The top boundary of the rectangle.</param>
        /// <param name="endY">The bottom boundary of the rectangle.</param>
        /// <param name="pixelX">The X coordinate of the pixel.</param>
        /// <param name="pixelY">The Y coordinate of the pixel.</param>
        /// <returns>The fraction of the pixel covered by the rectangle, ranging from 0.0 to 1.0.</returns>
        private static float CalculateCoverage(float startX, float endX, float startY, float endY, int pixelX, int pixelY)
        {
            float leftBoundary = startX - pixelX;
            float rightBoundary = (pixelX + 1) - endX;

            float horizontalFactor = 1.0f - Math.Max(0.0f, leftBoundary) - Math.Max(0.0f, rightBoundary);
            float horizontalCoverage = Math.Max(0.0f, horizontalFactor);

            float topBoundary = startY - pixelY;
            float bottomBoundary = (pixelY + 1) - endY;

            float verticalFactor = 1.0f - Math.Max(0.0f, topBoundary) - Math.Max(0.0f, bottomBoundary);
            float verticalCoverage = Math.Max(0.0f, verticalFactor);

            return horizontalCoverage * verticalCoverage;
        }
    }
}