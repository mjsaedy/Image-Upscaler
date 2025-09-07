using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: program.exe input_file scaling_factor output_file");
            return;
        }

        string inputFile = args[0];
        string scalingStr = args[1];
        string outputFile = args[2];

        if (!File.Exists(inputFile))
        {
            Console.WriteLine("Input file does not exist.");
            return;
        }

        if (!double.TryParse(scalingStr, out double scale) || scale <= 0)
        {
            Console.WriteLine("Scaling factor must be a positive number.");
            return;
        }

        try
        {
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

                    // Choose output format based on extension
                    ImageFormat format = GetImageFormatFromExtension(outputFile);
                    resized.Save(outputFile, format);
                }
            }

            Console.WriteLine("Upscaling complete.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing image: " + ex.Message);
        }
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
            _ => ImageFormat.Png, // default fallback
        };
    }
}
