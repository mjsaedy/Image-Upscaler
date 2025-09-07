using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

public static class ImageScaler {
    
    public static void Scale(string inputFile, string outputFile, double scale = 2.0, int jpegQuality = 85) {
        //try {
            using (Bitmap original = new Bitmap(inputFile)) {
                int newWidth = (int)(original.Width * scale);
                int newHeight = (int)(original.Height * scale);
                //Console.WriteLine($"{newWidth}x{newHeight}");
                using (Bitmap resized = new Bitmap(newWidth, newHeight))
                using (Graphics g = Graphics.FromImage(resized)) {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.DrawImage(original, 0, 0, newWidth, newHeight);
                    ImageFormat format = GetImageFormatFromExtension(outputFile);
                    if (format == ImageFormat.Jpeg) {
                        SaveJpegWithQuality(resized, outputFile, jpegQuality);
                    } else {
                        resized.Save(outputFile, format);
                    }
                    Console.WriteLine($"Upscaled image saved to: {outputFile}");
                }
            }
        //} catch (Exception ex) {
        //    Console.WriteLine("Error processing image: " + ex.Message);
        //}
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
        if (jpegCodec == null) {
            throw new InvalidOperationException("JPEG encoder not found.");
        }
        EncoderParameters encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        image.Save(outputPath, jpegCodec, encoderParams);
    }
}
