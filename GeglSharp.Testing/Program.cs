using System;
using System.IO;

namespace GeglSharp.Testing
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string inputPath = GetInputPath(args);

                if (string.IsNullOrWhiteSpace(inputPath))
                {
                    Console.WriteLine("An input BMP path is required.");
                    return 1;
                }

                inputPath = Path.GetFullPath(inputPath);

                if (!File.Exists(inputPath))
                {
                    Console.WriteLine("File not found: " + inputPath);
                    return 1;
                }

                if (!string.Equals(Path.GetExtension(inputPath), ".bmp", stringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("The input file must have a .bmp extension.");
                    return 1;
                }

                int operation = GetOperation(args);
                int width;
                int height;
                byte[] colors = BmpImageReader.Read(inputPath, out width, out height);
                string outputPath = CreateOutputPath(inputPath, operation);

                if (operation == 1)
                {
                    int levels = GetPosterizeLevels(args);
                    GeglSharpTest.SavePosterizeResult(colors, width, height, outputPath, levels);
                }
                else
                {
                    DitherSettings settings = new DitherSettings();
                    settings.RedLevels = 12;
                    settings.GreenLevels = 12;
                    settings.BlueLevels = 12;
                    settings.AlphaLevels = 8;
                    settings.Method = GetDitherMethod(args);
                    GeglSharpTest.SaveDitherResult(colors, width, height, outputPath, settings);
                }

                Console.WriteLine("Input:  " + inputPath);
                Console.WriteLine("Output: " + outputPath);
                Console.WriteLine("Image:  " + width + " x " + height);
                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
                return 1;
            }
        }

        private static string GetInputPath(string[] args)
        {
            if (args.Length > 0)
            {
                return TrimQuotes(args[0]);
            }

            Console.Write("Path to BMP image: ");
            return TrimQuotes(Console.ReadLine());
        }

        private static int GetOperation(string[] args)
        {
            if (args.Length > 1)
            {
                return ParseOperation(args[1]);
            }

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Choose operation:");
                Console.WriteLine("1. Posterize");
                Console.WriteLine("2. Dither");
                Console.Write("Operation: ");

                string input = Console.ReadLine();
                int operation;

                if (Int32.TryParse(input, out operation) && (operation == 1 || operation == 2))
                {
                    return operation;
                }

                Console.WriteLine("Enter 1 or 2.");
            }
        }

        private static int ParseOperation(string input)
        {
            if (string.Equals(input, "1", stringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "posterize", stringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(input, "2", stringComparison.OrdinalIgnoreCase) ||
                string.Equals(input, "dither", stringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            throw new ArgumentException("Operation must be Posterize, Dither, 1, or 2.");
        }

        private static int GetPosterizeLevels(string[] args)
        {
            if (args.Length > 2)
            {
                return ParsePosterizeLevels(args[2]);
            }

            while (true)
            {
                Console.Write("Posterize levels (1-64, Enter = 8): ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    return 8;
                }

                int levels;

                if (Int32.TryParse(input, out levels) && levels >= 1 && levels <= 64)
                {
                    return levels;
                }

                Console.WriteLine("Enter an integer from 1 to 64.");
            }
        }

        private static int ParsePosterizeLevels(string input)
        {
            int levels;

            if (Int32.TryParse(input, out levels) && levels >= 1 && levels <= 64)
            {
                return levels;
            }

            throw new ArgumentException("Posterize levels must be an integer from 1 to 64.");
        }

        private static DitherMethod GetDitherMethod(string[] args)
        {
            if (args.Length > 2)
            {
                DitherMethod parsedMethod;

                if (Enum.TryParse<DitherMethod>(args[2], true, out parsedMethod) &&
                    Enum.IsDefined(typeof(DitherMethod), parsedMethod))
                {
                    return parsedMethod;
                }

                throw new ArgumentException("Unknown dither method: " + args[2]);
            }

            Array methods = Enum.GetValues(typeof(DitherMethod));

            Console.WriteLine();
            Console.WriteLine("Choose dithering method:");

            for (int i = 0; i < methods.Length; i++)
            {
                Console.WriteLine((i + 1) + ". " + methods.GetValue(i));
            }

            while (true)
            {
                Console.Write("Method (Enter = FloydSteinberg): ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    return DitherMethod.FloydSteinberg;
                }

                int methodIndex;

                if (Int32.TryParse(input, out methodIndex) && methodIndex >= 1 && methodIndex <= methods.Length)
                {
                    return (DitherMethod)methods.GetValue(methodIndex - 1);
                }

                DitherMethod parsedMethod;

                if (Enum.TryParse<DitherMethod>(input, true, out parsedMethod) &&
                    Enum.IsDefined(typeof(DitherMethod), parsedMethod))
                {
                    return parsedMethod;
                }

                Console.WriteLine("Enter a method number or a method name from the list.");
            }
        }

        private static string CreateOutputPath(string inputPath, int operation)
        {
            string directory = Path.GetDirectoryName(inputPath);
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            string suffix = operation == 1 ? "_Posterize" : "_Dither";

            return Path.Combine(directory, fileName + suffix + extension);
        }

        private static string TrimQuotes(string path)
        {
            if (path == null)
            {
                return null;
            }

            path = path.Trim();

            if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
            {
                path = path.Substring(1, path.Length - 2);
            }

            return path;
        }
    }
}
