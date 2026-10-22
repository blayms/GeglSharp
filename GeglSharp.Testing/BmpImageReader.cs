using System;
using System.IO;

internal static class BmpImageReader
{
    public static byte[] Read(string path, out int width, out int height)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("An input BMP path is required.", "path");
        }

        byte[] fileData = File.ReadAllBytes(path);

        if (fileData.Length < 54)
        {
            throw new InvalidDataException("The BMP file is too small or has an invalid header.");
        }

        if (fileData[0] != (byte)'B' || fileData[1] != (byte)'M')
        {
            throw new InvalidDataException("The file does not have a BMP signature.");
        }

        int pixelOffset = ReadInt32(fileData, 10);
        int headerSize = ReadInt32(fileData, 14);
        int widthValue = ReadInt32(fileData, 18);
        int heightValue = ReadInt32(fileData, 22);
        int planes = ReadUInt16(fileData, 26);
        int bitsPerPixel = ReadUInt16(fileData, 28);
        uint compression = ReadUInt32(fileData, 30);

        if (headerSize < 40 || 14L + headerSize > fileData.Length)
        {
            throw new InvalidDataException("Only BMP files with a BITMAPINFOHEADER-compatible header are supported.");
        }

        if (widthValue <= 0 || heightValue == 0 || heightValue == Int32.MinValue)
        {
            throw new InvalidDataException("The BMP image dimensions are invalid.");
        }

        if (planes != 1)
        {
            throw new InvalidDataException("The BMP file must have exactly one color plane.");
        }

        if (bitsPerPixel != 24 && bitsPerPixel != 32)
        {
            throw new InvalidDataException("Only 24-bit and 32-bit BMP images are supported.");
        }

        if (compression != 0)
        {
            throw new InvalidDataException("Compressed BMP files and bitfield-compressed BMP files are not supported. Use uncompressed 24-bit or 32-bit BMP.");
        }

        if (pixelOffset < 14 + headerSize || pixelOffset > fileData.Length)
        {
            throw new InvalidDataException("The BMP pixel-data offset is invalid.");
        }

        width = widthValue;
        height = Math.Abs(heightValue);

        long pixelCount = (long)width * height;
        long outputLength = pixelCount * 4L;
        long rowStride = (((long)width * bitsPerPixel + 31L) / 32L) * 4L;
        long pixelDataEnd = (long)pixelOffset + rowStride * height;

        if (outputLength > Int32.MaxValue)
        {
            throw new InvalidDataException("The BMP image is too large to fit into a managed byte array.");
        }

        if (pixelDataEnd > fileData.Length)
        {
            throw new InvalidDataException("The BMP pixel data is incomplete or truncated.");
        }

        byte[] colors = new byte[(int)outputLength];
        int sourceBytesPerPixel = bitsPerPixel / 8;
        int outputRowBytes = width * 4;

        for (int y = 0; y < height; y++)
        {
            int sourceY = heightValue > 0 ? height - 1 - y : y;
            int sourceRowOffset = pixelOffset + (int)(sourceY * rowStride);
            int outputRowOffset = y * outputRowBytes;

            for (int x = 0; x < width; x++)
            {
                int sourceOffset = sourceRowOffset + x * sourceBytesPerPixel;
                int outputOffset = outputRowOffset + x * 4;

                colors[outputOffset] = fileData[sourceOffset + 2];
                colors[outputOffset + 1] = fileData[sourceOffset + 1];
                colors[outputOffset + 2] = fileData[sourceOffset];
                colors[outputOffset + 3] = sourceBytesPerPixel == 4 ? fileData[sourceOffset + 3] : (byte)255;
            }
        }

        return colors;
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return unchecked((int)ReadUInt32(data, offset));
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)data[offset] |
               ((uint)data[offset + 1] << 8) |
               ((uint)data[offset + 2] << 16) |
               ((uint)data[offset + 3] << 24);
    }

    private static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8);
    }
}
