using System;
//using Cmdline;


class Program {
    
    static void Main(string[] args)     {
        string exe_name = Cmdline.Utils.GetExecutableName();

        var parser = new Cmdline.CommandLineParser(exe_name, "Image Scaler", supportSlashOptions: true);
        parser.AddOption("help", "h", "Show this help text");
        parser.AddOption("input", "i", "Input file path", isRequired: true, hasValue: true);
        parser.AddOption("output", "o", "Output file path", isRequired: true, hasValue: true);
        parser.AddOption("scale", "s", "Scaling Factor", hasValue: true, defaultValue: "2.0");
        parser.AddOption("quality", "q", "JPEG Quality", hasValue: true, defaultValue: "85");
        parser.AddOption("verbose", "v", "Enable verbose output");
        var result = parser.Parse(args);

        if (result.HasErrors || result.HasOption("help")) {
            Console.WriteLine(parser.GetUsageText());
            if (result.HasErrors) {
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors) {
                    Console.WriteLine($"  {error}");
                }
            }
            return;
        }
        //testing
        Console.WriteLine($"{result.Arguments.Count} arguments:");
        foreach (var arg in result.Arguments) {
            Console.WriteLine($"{arg}");
        }

        // Use the parsed values
        string inputFile = result.GetOptionValue("input");
        string outputFile = result.GetOptionValue("output");
        bool verbose = result.HasOption("verbose");
        string sQuality = result.GetOptionValue("quality");
        string sScale = result.GetOptionValue("scale");
        //int jpeg_quality;
        //double scale;
        if (!int.TryParse(sQuality, out int jpeg_quality) || jpeg_quality < 1 || jpeg_quality > 100) {
            Console.WriteLine("JPEG quality must be an integer between 1 and 100.");
            jpeg_quality = 85;
        }
        if (!double.TryParse(sScale, out double scale) || scale <= 0 || scale >= 10) {
            Console.WriteLine("Scaling factor must be between 1 and 10");
            scale = 2;
        }
        Console.WriteLine($"input file: {inputFile}");
        Console.WriteLine($"output file: {outputFile}");
        Console.WriteLine($"quality = {jpeg_quality}");
        Console.WriteLine($"scale = {scale}");
        
        bool input_file_exists = Cmdline.Utils.VerifyFileExists(inputFile, verbose : verbose);
        string actual_output_file = Cmdline.Utils.GetUniqueFilename(outputFile, verbose : verbose);
        
        ImageScaler.Scale(inputFile, actual_output_file, scale, jpeg_quality);
    }
}
