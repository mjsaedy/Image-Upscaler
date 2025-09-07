using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class ImageScaler {

    /// <summary>
    /// Scales an image with high-quality resampling and optional enhancements.
    /// </summary>
    public static void Scale(
        string inputFile,
        string outputFile,
        double scale = 2.0,
        int jpegQuality = 85,
        bool sharpen = false,
        float sharpenStrength = 1.0f,
        float saturation = 1f,
        float brightness = 0f,  // -1..1
        float contrast = 0f,    // -1..1
        bool mirrorHorizontal = false,
        bool mirrorVertical = false
    ) {
        using (Bitmap original = new Bitmap(inputFile)) {
            Bitmap processed = (Bitmap)original.Clone();

            // Sharpen
            if (sharpen && Math.Abs(sharpenStrength) > 0.01f) {
                Bitmap temp = Sharpen(processed, sharpenStrength);
                processed.Dispose();
                processed = temp;
            }

            // Saturation
            if (Math.Abs(saturation - 1f) > 0.01f) {
                Bitmap temp = AdjustSaturation(processed, saturation);
                processed.Dispose();
                processed = temp;
            }

            // Brightness/Contrast
            if (Math.Abs(brightness) > 0.01f || Math.Abs(contrast) > 0.01f) {
                Bitmap temp = AdjustBrightnessContrast(processed, brightness, contrast);
                processed.Dispose();
                processed = temp;
            }

            // Mirror
            if (mirrorHorizontal) processed.RotateFlip(RotateFlipType.RotateNoneFlipX);
            if (mirrorVertical) processed.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // Scale
            int newWidth = (int)(processed.Width * scale);
            int newHeight = (int)(processed.Height * scale);

            using (Bitmap resized = new Bitmap(newWidth, newHeight))
            using (Graphics g = Graphics.FromImage(resized)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(processed, 0, 0, newWidth, newHeight);

                ImageFormat format = GetImageFormatFromExtension(outputFile);
                if (format == ImageFormat.Jpeg) {
                    SaveJpegWithQuality(resized, outputFile, jpegQuality);
                } else {
                    resized.Save(outputFile, format);
                }
                Console.WriteLine($"Processed image saved to: {outputFile}");
            }

            processed.Dispose();
        }
    }

    private static ImageFormat GetImageFormatFromExtension(string filename) {
        string ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext switch {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            ".png" => ImageFormat.Png,
            ".gif" => ImageFormat.Gif,
            _ => throw new NotSupportedException("Unsupported output file format: " + ext)
        };
    }

    private static void SaveJpegWithQuality(Image image, string outputPath, int quality) {
        ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        if (jpegCodec == null) throw new InvalidOperationException("JPEG encoder not found.");
        EncoderParameters encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        image.Save(outputPath, jpegCodec, encoderParams);
    }

    // -------------------- Sharpen --------------------
    public static Bitmap Sharpen(Bitmap source, float strength = 1.0f) {
        float[,] baseKernel = {
            { -1, -1, -1 },
            { -1,  9, -1 },
            { -1, -1, -1 }
        };

        // Scale kernel by strength
        float[,] kernel = new float[3, 3];
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                kernel[y, x] = baseKernel[y, x] * strength;

        return ConvolutionLockBits(source, kernel);
    }

    private static Bitmap ConvolutionLockBits(Bitmap source, float[,] kernel) {
        int width = source.Width;
        int height = source.Height;
        Bitmap result = new Bitmap(width, height, source.PixelFormat);

        BitmapData srcData = source.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, source.PixelFormat);
        BitmapData dstData = result.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, source.PixelFormat);

        int bytesPerPixel = Image.GetPixelFormatSize(source.PixelFormat) / 8;
        int stride = srcData.Stride;
        byte[] buffer = new byte[stride * height];
        byte[] resultBuffer = new byte[stride * height];
        System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, buffer, 0, buffer.Length);

        int kCenter = kernel.GetLength(0) / 2;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float r = 0, g = 0, b = 0;
                for (int ky = 0; ky < kernel.GetLength(0); ky++) {
                    int py = Math.Min(height - 1, Math.Max(0, y + ky - kCenter));
                    for (int kx = 0; kx < kernel.GetLength(1); kx++) {
                        int px = Math.Min(width - 1, Math.Max(0, x + kx - kCenter));
                        int idx = py * stride + px * bytesPerPixel;
                        float k = kernel[ky, kx];
                        b += buffer[idx + 0] * k;
                        g += buffer[idx + 1] * k;
                        r += buffer[idx + 2] * k;
                    }
                }

                int dstIdx = y * stride + x * bytesPerPixel;
                resultBuffer[dstIdx + 0] = (byte)Math.Min(255, Math.Max(0, (int)b));
                resultBuffer[dstIdx + 1] = (byte)Math.Min(255, Math.Max(0, (int)g));
                resultBuffer[dstIdx + 2] = (byte)Math.Min(255, Math.Max(0, (int)r));
                if (bytesPerPixel == 4) resultBuffer[dstIdx + 3] = buffer[dstIdx + 3]; // alpha
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(resultBuffer, 0, dstData.Scan0, resultBuffer.Length);
        source.UnlockBits(srcData);
        result.UnlockBits(dstData);

        return result;
    }

    // -------------------- Saturation --------------------
    public static Bitmap AdjustSaturation(Bitmap source, float saturation) {
        int width = source.Width;
        int height = source.Height;
        Bitmap result = new Bitmap(width, height, source.PixelFormat);

        BitmapData srcData = source.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, source.PixelFormat);
        BitmapData dstData = result.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, source.PixelFormat);

        int bytesPerPixel = Image.GetPixelFormatSize(source.PixelFormat) / 8;
        int stride = srcData.Stride;
        byte[] buffer = new byte[stride * height];
        byte[] resultBuffer = new byte[stride * height];
        System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, buffer, 0, buffer.Length);

        float lumR = 0.3086f;
        float lumG = 0.6094f;
        float lumB = 0.0820f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int idx = y * stride + x * bytesPerPixel;
                float b = buffer[idx + 0] / 255f;
                float g = buffer[idx + 1] / 255f;
                float r = buffer[idx + 2] / 255f;

                float lum = r * lumR + g * lumG + b * lumB;
                r = lum + (r - lum) * saturation;
                g = lum + (g - lum) * saturation;
                b = lum + (b - lum) * saturation;

                resultBuffer[idx + 0] = (byte)Math.Min(255, Math.Max(0, (int)(b * 255)));
                resultBuffer[idx + 1] = (byte)Math.Min(255, Math.Max(0, (int)(g * 255)));
                resultBuffer[idx + 2] = (byte)Math.Min(255, Math.Max(0, (int)(r * 255)));
                if (bytesPerPixel == 4) resultBuffer[idx + 3] = buffer[idx + 3]; // alpha
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(resultBuffer, 0, dstData.Scan0, resultBuffer.Length);
        source.UnlockBits(srcData);
        result.UnlockBits(dstData);

        return result;
    }

    // -------------------- Brightness/Contrast --------------------
    public static Bitmap AdjustBrightnessContrast(Bitmap source, float brightness = 0f, float contrast = 0f) {
        int width = source.Width;
        int height = source.Height;
        Bitmap result = new Bitmap(width, height, source.PixelFormat);

        BitmapData srcData = source.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly, source.PixelFormat);
        BitmapData dstData = result.LockBits(new Rectangle(0, 0, width, height),
            ImageLockMode.WriteOnly, source.PixelFormat);

        int bytesPerPixel = Image.GetPixelFormatSize(source.PixelFormat) / 8;
        int stride = srcData.Stride;
        byte[] buffer = new byte[stride * height];
        byte[] resultBuffer = new byte[stride * height];
        System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, buffer, 0, buffer.Length);

        float c = 1f + contrast;
        float b = brightness * 255f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int idx = y * stride + x * bytesPerPixel;
                for (int i = 0; i < 3; i++) {
                    float val = buffer[idx + i] * c + b;
                    resultBuffer[idx + i] = (byte)Math.Min(255, Math.Max(0, (int)val));
                }
                if (bytesPerPixel == 4) resultBuffer[idx + 3] = buffer[idx + 3]; // alpha
            }
        }

        System.Runtime.InteropServices.Marshal.Copy(resultBuffer, 0, dstData.Scan0, resultBuffer.Length);
        source.UnlockBits(srcData);
        result.UnlockBits(dstData);

        return result;
    }
}
