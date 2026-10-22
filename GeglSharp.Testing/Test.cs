using System;
using System.IO;
namespace GeglSharp.Testing
{
    public static class GeglSharpTest
    {
        public static byte[] CreatePosterizeBmp(byte[] colors, int sourceImageWidth, int sourceImageHeight)
        {
            return CreatePosterizeBmp(colors, sourceImageWidth, sourceImageHeight, 8);
        }

        public static byte[] CreatePosterizeBmp(byte[] colors, int sourceImageWidth, int sourceImageHeight, int levels)
        {
            byte[] processedColors = CloneColors(colors, sourceImageWidth, sourceImageHeight);
            Gegl.Posterize(processedColors, sourceImageWidth, sourceImageHeight, levels);

            BmpByteDataProcessor processor = new BmpByteDataProcessor();
            return processor.Process(processedColors, sourceImageWidth, sourceImageHeight);
        }

        public static byte[] CreateDitherBmp(byte[] colors, int sourceImageWidth, int sourceImageHeight)
        {
            return CreateDitherBmp(colors, sourceImageWidth, sourceImageHeight, new DitherSettings());
        }

        public static byte[] CreateDitherBmp(byte[] colors, int sourceImageWidth, int sourceImageHeight, DitherSettings settings)
        {
            byte[] processedColors = CloneColors(colors, sourceImageWidth, sourceImageHeight);
            Gegl.Dither(processedColors, sourceImageWidth, sourceImageHeight, settings);

            BmpByteDataProcessor processor = new BmpByteDataProcessor();
            return processor.Process(processedColors, sourceImageWidth, sourceImageHeight);
        }

        public static void SavePosterizeResult(byte[] colors, int sourceImageWidth, int sourceImageHeight, string outputPath)
        {
            SavePosterizeResult(colors, sourceImageWidth, sourceImageHeight, outputPath, 8);
        }

        public static void SavePosterizeResult(byte[] colors, int sourceImageWidth, int sourceImageHeight, string outputPath, int levels)
        {
            ValidateOutputPath(outputPath);
            byte[] bmp = CreatePosterizeBmp(colors, sourceImageWidth, sourceImageHeight, levels);
            File.WriteAllBytes(outputPath, bmp);
        }

        public static void SaveDitherResult(byte[] colors, int sourceImageWidth, int sourceImageHeight, string outputPath)
        {
            SaveDitherResult(colors, sourceImageWidth, sourceImageHeight, outputPath, new DitherSettings());
        }

        public static void SaveDitherResult(byte[] colors, int sourceImageWidth, int sourceImageHeight, string outputPath, DitherSettings settings)
        {
            ValidateOutputPath(outputPath);
            byte[] bmp = CreateDitherBmp(colors, sourceImageWidth, sourceImageHeight, settings);
            File.WriteAllBytes(outputPath, bmp);
        }

        private static byte[] CloneColors(byte[] colors, int width, int height)
        {
            ValidateColors(colors, width, height);
            byte[] copy = new byte[colors.Length];
            Buffer.BlockCopy(colors, 0, copy, 0, colors.Length);
            return copy;
        }

        private static void ValidateColors(byte[] colors, int width, int height)
        {
            if (colors == null)
            {
                throw new ArgumentNullException("colors");
            }

            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException("sourceImageWidth");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException("sourceImageHeight");
            }

            long expectedLength = (long)width * height * 4L;

            if (expectedLength > int.MaxValue || colors.Length != expectedLength)
            {
                throw new ArgumentException("The byte array must contain exactly width * height * 4 bytes in RGBA32 order.", "colors");
            }
        }

        private static void ValidateOutputPath(string outputPath)
        {
            if (String.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentException("An output file path is required.", "outputPath");
            }
        }
    }

    public sealed class BmpByteDataProcessor : ByteDataProcessor
    {
        private const int FileHeaderSize = 14;
        private const int InfoHeaderSize = 40;
        private const int PixelDataOffset = FileHeaderSize + InfoHeaderSize;
        private const int BitsPerPixel = 24;

        public override byte[] Process(byte[] colors, int sourceImageWidth, int sourceImageHeight)
        {
            ValidateInput(colors, sourceImageWidth, sourceImageHeight);

            long rowStrideLong = ((long)sourceImageWidth * 3L + 3L) & ~3L;
            long imageSizeLong = rowStrideLong * sourceImageHeight;
            long fileSizeLong = PixelDataOffset + imageSizeLong;

            if (rowStrideLong > int.MaxValue || imageSizeLong > int.MaxValue || fileSizeLong > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("sourceImageWidth", "The bitmap is too large to encode in a byte array.");
            }

            int rowStride = (int)rowStrideLong;
            int imageSize = (int)imageSizeLong;
            int fileSize = (int)fileSizeLong;
            byte[] bmp = new byte[fileSize];

            bmp[0] = (byte)'B';
            bmp[1] = (byte)'M';
            WriteInt32(bmp, 2, fileSize);
            WriteInt32(bmp, 10, PixelDataOffset);
            WriteInt32(bmp, 14, InfoHeaderSize);
            WriteInt32(bmp, 18, sourceImageWidth);
            WriteInt32(bmp, 22, sourceImageHeight);
            WriteInt16(bmp, 26, 1);
            WriteInt16(bmp, 28, BitsPerPixel);
            WriteInt32(bmp, 30, 0);
            WriteInt32(bmp, 34, imageSize);
            WriteInt32(bmp, 38, 2835);
            WriteInt32(bmp, 42, 2835);
            WriteInt32(bmp, 46, 0);
            WriteInt32(bmp, 50, 0);

            for (int outputY = 0; outputY < sourceImageHeight; outputY++)
            {
                int sourceY = sourceImageHeight - 1 - outputY;
                int sourceRowOffset = sourceY * sourceImageWidth * 4;
                int outputRowOffset = PixelDataOffset + outputY * rowStride;

                for (int x = 0; x < sourceImageWidth; x++)
                {
                    int sourceOffset = sourceRowOffset + x * 4;
                    int outputOffset = outputRowOffset + x * 3;

                    bmp[outputOffset] = colors[sourceOffset + 2];
                    bmp[outputOffset + 1] = colors[sourceOffset + 1];
                    bmp[outputOffset + 2] = colors[sourceOffset];
                }
            }

            return bmp;
        }

        private static void WriteInt16(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 255);
            data[offset + 1] = (byte)((value >> 8) & 255);
        }

        private static void WriteInt32(byte[] data, int offset, int value)
        {
            data[offset] = (byte)(value & 255);
            data[offset + 1] = (byte)((value >> 8) & 255);
            data[offset + 2] = (byte)((value >> 16) & 255);
            data[offset + 3] = (byte)((value >> 24) & 255);
        }
    }
}
