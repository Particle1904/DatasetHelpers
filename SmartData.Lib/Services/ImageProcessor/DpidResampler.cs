using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SmartData.Lib.Services.ImageProcessor
{
    public class DpidResampler
    {
        private const float Epsilon = 1.17549435E-38f;

        public Image<Rgba32> DpidResampleRgb(Image<Rgba32> sourceImage, int outputWidth, int outputHeight, float lambda)
        {
            int inputWidth = sourceImage.Width;
            int inputHeight = sourceImage.Height;

            float[] inputPixels = new float[inputWidth * inputHeight * 3];
            sourceImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        int baseIndex = (y * inputWidth + x) * 3;
                        inputPixels[baseIndex] = pixelRow[x].R / 255.0f;
                        inputPixels[baseIndex + 1] = pixelRow[x].G / 255.0f;
                        inputPixels[baseIndex + 2] = pixelRow[x].B / 255.0f;
                    }
                }
            });

            float[] outputPixels = new float[outputWidth * outputHeight * 3];
            float patchWidth = (float)inputWidth / outputWidth;
            float patchHeight = (float)inputHeight / outputHeight;

            float[,] k = { { 1.0f, 2.0f, 1.0f }, { 2.0f, 4.0f, 2.0f }, { 1.0f, 2.0f, 1.0f } };

            for (int py = 0; py < outputHeight; py++)
            {
                for (int px = 0; px < outputWidth; px++)
                {
                    float s0 = 0.0f, s1 = 0.0f, s2 = 0.0f, sw = 0.0f;
                    for (int ky = 0; ky < 3; ky++)
                    {
                        int ny = Math.Min(outputHeight - 1, Math.Max(0, py + ky - 1));
                        for (int kx = 0; kx < 3; kx++)
                        {
                            int nx = Math.Min(outputWidth - 1, Math.Max(0, px + kx - 1));
                            float w_k = k[ky, kx];

                            float sx = nx * patchWidth;
                            float ex = sx + patchWidth;
                            float sy = ny * patchHeight;
                            float ey = sy + patchHeight;

                            int sxr = (int)Math.Floor(sx);
                            int syr = (int)Math.Floor(sy);
                            int exr = (int)Math.Min(inputWidth, Math.Ceiling(ex));
                            int eyr = (int)Math.Min(inputHeight, Math.Ceiling(ey));

                            float a0 = 0.0f, a1 = 0.0f, a2 = 0.0f, aw = 0.0f;
                            for (int iy = syr; iy < eyr; iy++)
                            {
                                for (int ix = sxr; ix < exr; ix++)
                                {
                                    float cov = CalculateCoverage(sx, ex, sy, ey, ix, iy);
                                    if (cov <= 0) continue;
                                    int baseIndex = (iy * inputWidth + ix) * 3;
                                    a0 += inputPixels[baseIndex] * cov;
                                    a1 += inputPixels[baseIndex + 1] * cov;
                                    a2 += inputPixels[baseIndex + 2] * cov;
                                    aw += cov;
                                }
                            }

                            if (aw > 0)
                            {
                                float inv_aw = 1.0f / aw;
                                s0 += (a0 * inv_aw) * w_k;
                                s1 += (a1 * inv_aw) * w_k;
                                s2 += (a2 * inv_aw) * w_k;
                                sw += w_k;
                            }
                        }
                    }

                    float m0 = 0, m1 = 0, m2 = 0;
                    if (sw > 0)
                    {
                        float inv_sw = 1.0f / sw;
                        m0 = s0 * inv_sw;
                        m1 = s1 * inv_sw;
                        m2 = s2 * inv_sw;
                    }

                    float sx_o = px * patchWidth;
                    float ex_o = sx_o + patchWidth;
                    float sy_o = py * patchHeight;
                    float ey_o = sy_o + patchHeight;

                    int sxr_o = (int)Math.Floor(sx_o);
                    int syr_o = (int)Math.Floor(sy_o);
                    int exr_o = (int)Math.Min(inputWidth, Math.Ceiling(ex_o));
                    int eyr_o = (int)Math.Min(inputHeight, Math.Ceiling(ey_o));

                    float o0 = 0.0f, o1 = 0.0f, o2 = 0.0f, ow = 0.0f;
                    for (int iy = syr_o; iy < eyr_o; iy++)
                    {
                        for (int ix = sxr_o; ix < exr_o; ix++)
                        {
                            float cov = CalculateCoverage(sx_o, ex_o, sy_o, ey_o, ix, iy);
                            if (cov <= 0) continue;

                            int baseIndex = (iy * inputWidth + ix) * 3;
                            float d0 = m0 - inputPixels[baseIndex];
                            float d1 = m1 - inputPixels[baseIndex + 1];
                            float d2 = m2 - inputPixels[baseIndex + 2];

                            float f = (lambda == 0.0f)
                                ? cov
                                : (float)Math.Pow(Math.Sqrt(d0 * d0 + d1 * d1 + d2 * d2), lambda) * cov;

                            o0 += inputPixels[baseIndex] * f;
                            o1 += inputPixels[baseIndex + 1] * f;
                            o2 += inputPixels[baseIndex + 2] * f;
                            ow += f;
                        }
                    }

                    int outBaseIndex = (py * outputWidth + px) * 3;
                    if (ow > 0.0f)
                    {
                        outputPixels[outBaseIndex] = o0 / ow;
                        outputPixels[outBaseIndex + 1] = o1 / ow;
                        outputPixels[outBaseIndex + 2] = o2 / ow;
                    }
                    else
                    {
                        outputPixels[outBaseIndex] = m0;
                        outputPixels[outBaseIndex + 1] = m1;
                        outputPixels[outBaseIndex + 2] = m2;
                    }
                }
            }

            Image<Rgba32> outputImage = new Image<Rgba32>(outputWidth, outputHeight);
            outputImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        int baseIndex = (y * outputWidth + x) * 3;
                        pixelRow[x] = new Rgba32(
                            (byte)Math.Clamp(outputPixels[baseIndex] * 255.0f, 0, 255),
                            (byte)Math.Clamp(outputPixels[baseIndex + 1] * 255.0f, 0, 255),
                            (byte)Math.Clamp(outputPixels[baseIndex + 2] * 255.0f, 0, 255)
                        );
                    }
                }
            });

            return outputImage;
        }

        private float CalculateCoverage(float sx, float ex, float sy, float ey, int ix, int iy)
        {
            float fx1 = Math.Min(1.0f, Math.Max(0.0f, 1.0f - (sx - ix)));
            float fx2 = Math.Min(1.0f, Math.Max(0.0f, 1.0f - ((ix + 1) - ex)));
            float fy1 = Math.Min(1.0f, Math.Max(0.0f, 1.0f - (sy - iy)));
            float fy2 = Math.Min(1.0f, Math.Max(0.0f, 1.0f - ((iy + 1) - ey)));
            return fx1 * fx2 * fy1 * fy2;
        }
    }
}