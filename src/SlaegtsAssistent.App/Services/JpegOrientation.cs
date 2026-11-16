using System;
using System.Buffers.Binary;

namespace SlaegtsAssistent.App.Services;

internal static class JpegOrientation
{
    public static int Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xff || bytes[1] != 0xd8) return 1;
        var offset = 2;
        while (offset + 4 <= bytes.Length && bytes[offset] == 0xff)
        {
            var marker = bytes[offset + 1];
            if (marker is 0xda or 0xd9) break;
            var length = BinaryPrimitives.ReadUInt16BigEndian(bytes[(offset + 2)..]);
            if (length < 2 || offset + 2 + length > bytes.Length) return 1;
            var segment = bytes.Slice(offset + 4, length - 2);
            if (marker == 0xe1 && segment.Length >= 14 && segment[..6].SequenceEqual("Exif\0\0"u8))
                return ReadTiff(segment[6..]);
            offset += length + 2;
        }
        return 1;
    }

    private static int ReadTiff(ReadOnlySpan<byte> tiff)
    {
        if (tiff.Length < 8) return 1;
        var little = tiff[0] == 'I' && tiff[1] == 'I';
        if (!little && !(tiff[0] == 'M' && tiff[1] == 'M')) return 1;
        ushort Read16(ReadOnlySpan<byte> data) => little ? BinaryPrimitives.ReadUInt16LittleEndian(data) : BinaryPrimitives.ReadUInt16BigEndian(data);
        uint Read32(ReadOnlySpan<byte> data) => little ? BinaryPrimitives.ReadUInt32LittleEndian(data) : BinaryPrimitives.ReadUInt32BigEndian(data);
        if (Read16(tiff[2..]) != 42) return 1;
        var directoryOffset = Read32(tiff[4..]);
        if (directoryOffset > tiff.Length - 2) return 1;
        var directory = tiff[(int)directoryOffset..];
        var count = Read16(directory);
        for (var i = 0; i < count; i++)
        {
            var position = 2 + i * 12;
            if (position > directory.Length - 12) return 1;
            var entry = directory.Slice(position, 12);
            if (Read16(entry) != 0x0112 || Read16(entry[2..]) != 3 || Read32(entry[4..]) != 1) continue;
            var value = Read16(entry[8..]);
            return value is >= 1 and <= 8 ? value : 1;
        }
        return 1;
    }
}
