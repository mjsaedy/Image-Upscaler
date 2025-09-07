using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Image Upscaler");

        if (args.Length < 3 || args.Length > 4)
        {
            Console.WriteLine("Usage: program.exe <input_file> <scaling_factor> <output_file> [jpeg_quality]");
            return;
        }

        string inputFile = args[0];
        string scalingStr = args[1];
        string outputFile = args[2];
        int jpegQuality = 85; // default JPEG quality

        if (args.Length == 4)
        {
            if (!int.TryParse(args[3], out jpegQuality) || jpegQuality < 1 || jpegQuality > 100)
            {
                Console.WriteLine("JPEG quality must be an integer between 1 and 100.");
                return;
            }
        }

        if (!File.Exists(inputFile))
        {
            Console.WriteLine("Error: Input file does not exist.");
            return;
        }

        if (!double.TryParse(scalingStr, out double scale) || scale <= 0)
        {
            Console.WriteLine("Error: Scaling factor must be a positive number.");
            return;
        }

        try
        {
            outputFile = GetAvailableFileName(outputFile);

            using (Bitmap original = new Bitmap(inputFile))
            {
                int newWidth = (int)(original.Width * scale);
                int newHeight = (int)(original.Height * scale);

                using (Bitmap resized = new Bitmap(newWidth, newHeight))
                using (Graphics g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;

                    g.DrawImage(original, 0, 0, newWidth, newHeight);

                    ImageFormat format = GetImageFormatFromExtension(outputFile);

                    if (format == ImageFormat.Jpeg)
                    {
                        SaveJpegWithQuality(resized, outputFile, jpegQuality);
                    }
                    else
                    {
                        resized.Save(outputFile, format);
                    }

                    Console.WriteLine($"Upscaled image saved to: {outputFile}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing image: " + ex.Message);
        }
    }

    static string GetAvailableFileName(string basePath)
    {
        if (!File.Exists(basePath))
            return basePath;

        string dir = Path.GetDirectoryName(basePath) ?? "";
        string fileName = Path.GetFileNameWithoutExtension(basePath);
        string ext = Path.GetExtension(basePath);
        int count = 1;

        string newPath;
        do
        {
            newPath = Path.Combine(dir, $"{fileName}_{count}{ext}");
            count++;
        } while (File.Exists(newPath));

        return newPath;
    }

    static ImageFormat GetImageFormatFromExtension(string filename)
    {
        string ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            ".png" => ImageFormat.Png,
            ".gif" => ImageFormat.Gif,
            _ => throw new NotSupportedException("Unsupported output file format: " + ext)
        };
    }

    static void SaveJpegWithQuality(Image image, string outputPath, int quality)
    {
        ImageCodecInfo jpegCodec = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

        if (jpegCodec == null)
        {
            throw new InvalidOperationException("JPEG encoder not found.");
        }

        EncoderParameters encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
        image.Save(outputPath, jpegCodec, encoderParams);
    }
}
