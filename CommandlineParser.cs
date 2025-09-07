using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Cmdline {

    public static class Utils {
        public static string GetExecutableName() {
            // Gets the entry assembly (the executable that started the application)
            var assembly = Assembly.GetEntryAssembly();
            // Get the executable name without the extension
            string name = System.IO.Path.GetFileNameWithoutExtension(assembly.Location);
            // Alternatively, get the full name with extension:
            // string name = System.IO.Path.GetFileName(assembly.Location);
            return name;
        }

        public static bool VerifyFileExists(string file, bool verbose = false) {
            if (!File.Exists(file)) {
                if (verbose)
                    Console.WriteLine($"Error: \"{file}\" does not exist.");
                return false;
            }
            return true;
        }

        public static string GetUniqueFilename(string outputFile, bool verbose = false) {
            if (!File.Exists(outputFile)) {
                return outputFile;
            }
            if (verbose)
                Console.WriteLine($"\"{outputFile}\" already exists.");
            string directory = Path.GetDirectoryName(outputFile);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFile);
            string extension = Path.GetExtension(outputFile);
            int counter = 1;
            string newOutputFile;
            do {
                newOutputFile = Path.Combine(directory, $"{fileNameWithoutExtension}_{counter}{extension}");
                counter++;
            } while (File.Exists(newOutputFile));
            if (verbose)
                Console.WriteLine($"Using alternative filename: \"{newOutputFile}\"");
            return newOutputFile;
        }
        
        
    }

    public class CommandLineParser {
        private readonly List<CommandLineOption> _options = new List<CommandLineOption>();
        private readonly Dictionary<string, CommandLineOption> _optionMap = new Dictionary<string, CommandLineOption>();
        private readonly bool _supportSlashOptions;
        private readonly string _applicationName;
        private readonly string _description;

        public CommandLineParser(string applicationName = null, string description = null, bool supportSlashOptions = false)
        {
            _applicationName = applicationName ?? System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            _description = description;
            _supportSlashOptions = supportSlashOptions;
        }

        public void AddOption(string name, string shortName = null, string description = null, bool isRequired = false, bool hasValue = false, string defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Option name cannot be null or whitespace", nameof(name));

            var option = new CommandLineOption
            {
                Name = name,
                ShortName = shortName,
                Description = description,
                IsRequired = isRequired,
                HasValue = hasValue,
                DefaultValue = defaultValue
            };

            _options.Add(option);
            _optionMap[name] = option;

            if (!string.IsNullOrWhiteSpace(shortName))
            {
                _optionMap[shortName] = option;
            }
        }

        public CommandLineParseResult Parse(string[] args) {
            var result = new CommandLineParseResult();
            var requiredOptions = new HashSet<CommandLineOption>(_options.Where(o => o.IsRequired));

            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                string optionName;
                string value = null;

                if (IsOption(arg, out optionName, out value)) {
                    if (!_optionMap.TryGetValue(optionName, out var option)) {
                        result.Errors.Add($"Unknown option: {arg}");
                        continue;
                    }

                    if (option.HasValue) {
                        if (value == null && i + 1 < args.Length && !IsOption(args[i + 1], out _, out _))
                        {
                            value = args[++i];
                        }
                        else if (value == null)
                        {
                            result.Errors.Add($"Option '{arg}' requires a value");
                            continue;
                        }
                    }
                    else if (value != null)
                    {
                        result.Errors.Add($"Option '{arg}' does not accept a value");
                        continue;
                    }

                    result.ParsedOptions[option.Name] = value ?? "true"; // Flag options get "true" as value
                    requiredOptions.Remove(option);
                }
                else
                {
                    result.Arguments.Add(arg);
                }
            }

            foreach (var requiredOption in requiredOptions)
            {
                result.Errors.Add($"Required option '--{requiredOption.Name}' is missing");
            }

            // Apply defaults for non-specified options
            foreach (var option in _options.Where(o => o.DefaultValue != null && !result.ParsedOptions.ContainsKey(o.Name)))
            {
                result.ParsedOptions[option.Name] = option.DefaultValue;
            }

            return result;
        }

        private bool IsOption(string arg, out string optionName, out string value)
        {
            optionName = null;
            value = null;

            if (arg.StartsWith("--"))
            {
                var parts = arg.Substring(2).Split(new[] { '=' }, 2);
                optionName = parts[0];
                if (parts.Length > 1) value = parts[1];
                return true;
            }
            else if (_supportSlashOptions && arg.StartsWith("/"))
            {
                var parts = arg.Substring(1).Split(new[] { '=' }, 2);
                optionName = parts[0];
                if (parts.Length > 1) value = parts[1];
                return true;
            }
            else if (arg.StartsWith("-") && arg.Length > 1)
            {
                // Handle combined short options (-abc)
                if (arg.Length > 2 && !arg.Contains("="))
                {
                    optionName = arg.Substring(1, 1);
                    return true;
                }
                else
                {
                    var parts = arg.Substring(1).Split(new[] { '=' }, 2);
                    optionName = parts[0];
                    if (parts.Length > 1) value = parts[1];
                    return true;
                }
            }

            return false;
        }

        public string GetUsageText(string positional_arguments = "[arguments]") {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_description)) {
                sb.AppendLine();
                sb.AppendLine(_description);
                sb.AppendLine();
            }
            sb.AppendLine($"Usage: {_applicationName} [options] {positional_arguments}");
            sb.AppendLine();
            sb.AppendLine("Options:");
            foreach (var option in _options.OrderBy(o => o.Name)) {
                var line = new StringBuilder();
                line.Append("  ");
                if (!string.IsNullOrEmpty(option.ShortName))
                    line.Append($"-{option.ShortName}, ");
                else
                    line.Append("    ");
                line.Append($"--{option.Name}");
                if (option.HasValue)
                    line.Append($"=VALUE");
                if (option.IsRequired)
                    line.Append(" (required)");
                if (option.DefaultValue != null)
                    line.Append($" [default: {option.DefaultValue}]");
                sb.Append(line.ToString().PadRight(40));
                if (!string.IsNullOrEmpty(option.Description))
                    sb.Append($" {option.Description}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private class CommandLineOption {
            public string Name { get; set; }
            public string ShortName { get; set; }
            public string Description { get; set; }
            public bool IsRequired { get; set; }
            public bool HasValue { get; set; }
            public string DefaultValue { get; set; }
        }
    }

    public class CommandLineParseResult {
        public Dictionary<string, string> ParsedOptions { get; } = new Dictionary<string, string>();
        public List<string> Arguments { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();
        public bool HasErrors => Errors.Count > 0;
        public string GetOptionValue(string name, string defaultValue = null) {
            return ParsedOptions.TryGetValue(name, out var value) ? value : defaultValue;
        }
        public bool HasOption(string name) {
            return ParsedOptions.ContainsKey(name);
        }
    }

}