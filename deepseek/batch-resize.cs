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
            Console.WriteLine("Usage: program.exe input_path scaling_factor output_directory [jpeg_quality=90]");
            Console.WriteLine("Examples:");
            Console.WriteLine("  Single file: program.exe input.jpg 2 output 85");
            Console.WriteLine("  Wildcards:   program.exe *.jpg 1.5 output");
            Console.WriteLine("  Directory:   program.exe C:\\input 2 C:\\output");
            return;
        }

        string inputPath = args[0];
        float scalingFactor;
        if (!float.TryParse(args[1], out scalingFactor) || scalingFactor <= 0)
        {
            Console.WriteLine("Error: scaling_factor must be a positive number.");
            return;
        }

        string outputDirectory = args[2];
        int jpegQuality = 90;
        if (args.Length == 4 && (!int.TryParse(args[3], out jpegQuality) || jpegQuality < 1 || jpegQuality > 100))
        {
            Console.WriteLine("Error: jpeg_quality must be an integer between 1 and 100.");
            return;
        }

        try
        {
            // Create output directory if it doesn't exist
            Directory.CreateDirectory(outputDirectory);

            // Get all matching input files
            string[] inputFiles;
            
            // Handle wildcard patterns
            if (inputPath.Contains("*") || inputPath.Contains("?"))
            {
                string directory = Path.GetDirectoryName(inputPath);
                if (string.IsNullOrEmpty(directory))
                    directory = Directory.GetCurrentDirectory();
                
                string searchPattern = Path.GetFileName(inputPath);
                inputFiles = Directory.GetFiles(directory, searchPattern);
            }
            // Handle directory input
            else if (Directory.Exists(inputPath))
            {
                inputFiles = Directory.GetFiles(inputPath);
            }
            // Handle single file input
            else if (File.Exists(inputPath))
            {
                inputFiles = new[] { inputPath };
            }
            else
            {
                Console.WriteLine("Error: Input path does not exist.");
                return;
            }

            if (inputFiles.Length == 0)
            {
                Console.WriteLine("Error: No matching files found.");
                return;
            }

            // Process each file
            foreach (string inputFile in inputFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(inputFile);
                    string extension = Path.GetExtension(inputFile).ToLower();
                    string outputFile = Path.Combine(outputDirectory, $"{fileName}_upscaled{extension}");

                    // Generate unique filename if needed
                    outputFile = GetUniqueFilename(outputFile);

                    using (Bitmap originalImage = new Bitmap(inputFile))
                    {
                        int newWidth = (int)(originalImage.Width * scalingFactor);
                        int newHeight = (int)(originalImage.Height * scalingFactor);

                        using (Bitmap upscaledImage = new Bitmap(newWidth, newHeight))
                        {
                            using (Graphics g = Graphics.FromImage(upscaledImage))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.CompositingQuality = CompositingQuality.HighQuality;

                                g.DrawImage(
                                    originalImage,
                                    new Rectangle(0, 0, newWidth, newHeight),
                                    new Rectangle(0, 0, originalImage.Width, originalImage.Height),
                                    GraphicsUnit.Pixel
                                );
                            }

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

                            Console.WriteLine($"Processed: {Path.GetFileName(inputFile)} -> {Path.GetFileName(outputFile)}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {Path.GetFileName(inputFile)}: {ex.Message}");
                }
            }

            Console.WriteLine($"\nCompleted! Processed {inputFiles.Length} files.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
    }

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

    private static ImageFormat GetImageFormat(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            _ => ImageFormat.Png,
        };
    }

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