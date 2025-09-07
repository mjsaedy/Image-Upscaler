using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 3 || args.Length > 4)
        {
            Console.WriteLine("Usage: program.exe input_file scaling_factor output_file [jpeg_quality=90]");
            Console.WriteLine("Example: program.exe input.jpg 2 output.jpg 85");
            return;
        }

        string inputFile = args[0];
        string outputFile = args[2];

        // Parse scaling factor
        if (!float.TryParse(args[1], out float scalingFactor) || scalingFactor <= 0)
        {
            Console.WriteLine("Error: scaling_factor must be a positive number.");
            return;
        }

        // Parse JPEG quality (default: 90 if not specified)
        int jpegQuality = 90;
        if (args.Length == 4 && (!int.TryParse(args[3], out jpegQuality) || jpegQuality < 1 || jpegQuality > 100))
        {
            Console.WriteLine("Error: jpeg_quality must be an integer between 1 and 100.");
            return;
        }

        try
        {
            // Validate input file
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Error: Input file '{inputFile}' does not exist.");
                return;
            }

            // Generate a unique output filename if it already exists
            outputFile = GetUniqueFilename(outputFile);

            // Load the original image
            using (Bitmap originalImage = new Bitmap(inputFile))
            {
                int newWidth = (int)(originalImage.Width * scalingFactor);
                int newHeight = (int)(originalImage.Height * scalingFactor);

                // Create upscaled image
                using (Bitmap upscaledImage = new Bitmap(newWidth, newHeight))
                {
                    using (Graphics g = Graphics.FromImage(upscaledImage))
                    {
                        // === BEST QUALITY SETTINGS ===
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic; // Best for enlargement
                        g.SmoothingMode = SmoothingMode.HighQuality;               // Anti-aliasing
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;            // Subpixel precision
                        g.CompositingQuality = CompositingQuality.HighQuality;      // Blending quality

                        g.DrawImage(
                            originalImage,
                            new Rectangle(0, 0, newWidth, newHeight),
                            new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                            GraphicsUnit.Pixel
                        );
                    }

                    // Save with JPEG quality setting (if output is JPEG)
                    ImageFormat format = GetImageFormat(outputFile);
                    if (format == ImageFormat.Jpeg)
                    {
                        EncoderParameters encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, jpegQuality);
                        ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");
                        upscaledImage.Save(outputFile, jpegCodec, encoderParams);
                    }
                    else
                    {
                        upscaledImage.Save(outputFile, format);
                    }

                    Console.WriteLine($"Successfully upscaled and saved to: {outputFile}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    // Helper: Generate a unique filename if the original exists
    private static string GetUniqueFilename(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        string directory = Path.GetDirectoryName(filePath);
        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        int counter = 1;

        string newFilePath;
        do
        {
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
            counter++;
        } while (File.Exists(newFilePath));

        return newFilePath;
    }

    // Helper: Get ImageFormat from file extension
    private static ImageFormat GetImageFormat(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            _ => ImageFormat.Png, // Default to PNG if unknown
        };
    }

    // Helper: Get encoder info for JPEG quality
    private static ImageCodecInfo GetEncoderInfo(string mimeType)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.MimeType == mimeType)
                return codec;
        }
        return null;
    }
}