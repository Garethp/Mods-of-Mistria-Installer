using System.IO.Compression;
using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Copies unchanged ZIP local records and compressed payloads byte-for-byte.
// Changed files are compressed normally. This is intentionally an experimental
// path and rejects formats it cannot prove safe to rebuild.
internal static class RawZipRebuilder
{
    private const uint Local = 0x04034b50;
    private const uint Central = 0x02014b50;
    private const uint End = 0x06054b50;
    private const uint Zip64End = 0x06064b50;
    private const uint Zip64Locator = 0x07064b50;
    private const uint Descriptor = 0x08074b50;
    private static readonly DateTimeOffset Time = new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed record Entry(string Name, byte[] Central, long LocalOffset, long CompressedSize,
        long UncompressedSize, uint Crc, ushort Flags, ushort Method);

    public static void Rebuild(string sourcePath, string destinationPath, IReadOnlyDictionary<string, string> changes)
    {
        using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var (centralOffset, centralSize, count) = ReadDirectoryLocation(input);
        input.Position = centralOffset;
        var entries = new List<Entry>((int)Math.Min(count, 1_000_000));
        for (var i = 0L; i < count; i++) entries.Add(ReadEntry(input));

        var names = entries.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var directory = new List<byte[]>(entries.Count + changes.Count);
        using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        foreach (var entry in entries)
        {
            var offset = output.Position;
            byte[] centralBytes;
            if (changes.TryGetValue(entry.Name, out var changed))
                WriteChanged(output, entry.Name, changed, out centralBytes);
            else
            {
                CopyRawEntry(input, entry, output);
                centralBytes = PatchOffset(entry.Central, offset, entry.LocalOffset);
            }
            directory.Add(centralBytes);
        }

        foreach (var change in changes.Where(c => !names.Contains(c.Key)))
        {
            WriteChanged(output, change.Key, change.Value, out var centralBytes);
            directory.Add(centralBytes);
        }

        var directoryOffset = output.Position;
        foreach (var bytes in directory) output.Write(bytes);
        var directorySize = output.Position - directoryOffset;
        WriteZip64End(output, directory.Count, directorySize, directoryOffset);
    }

    private static (long Offset, long Size, long Count) ReadDirectoryLocation(FileStream input)
    {
        var scan = (int)Math.Min(input.Length, 1024 * 1024);
        var buffer = new byte[scan];
        input.Position = input.Length - scan;
        input.ReadExactly(buffer);
        var end = -1;
        for (var i = scan - 22; i >= 0; i--)
            if (U32(buffer, i) == End) { end = i; break; }
        if (end < 0) throw new InvalidDataException("ZIP end-of-directory record was not found.");
        var endOffset = input.Length - scan + end;
        var count = U16(buffer, end + 10);
        var size = U32(buffer, end + 12);
        var offset = U32(buffer, end + 16);
        if (count != ushort.MaxValue && size != uint.MaxValue && offset != uint.MaxValue)
            return (offset, size, count);

        input.Position = endOffset - 20;
        if (U32(Read(input, 4), 0) != Zip64Locator) throw new InvalidDataException("ZIP64 locator is missing.");
        input.Position += 4;
        var zip64Offset = (long)U64(Read(input, 8), 0);
        input.Position = zip64Offset;
        if (U32(Read(input, 4), 0) != Zip64End) throw new InvalidDataException("ZIP64 end record is missing.");
        // From the start of the Zip64 EOCD signature, the total-entry-count
        // field begins at offset 32. The signature has already been read, so
        // skip the remaining 28 bytes before reading it.
        input.Position += 28;
        var zip64Count = (long)U64(Read(input, 8), 0);
        var zip64Size = (long)U64(Read(input, 8), 0);
        var zip64DirectoryOffset = (long)U64(Read(input, 8), 0);
        return (zip64DirectoryOffset, zip64Size, zip64Count);
    }

    private static Entry ReadEntry(FileStream input)
    {
        var start = input.Position;
        var fixedHeader = Read(input, 46);
        if (U32(fixedHeader, 0) != Central) throw new InvalidDataException("Invalid ZIP central directory entry.");
        var flags = U16(fixedHeader, 8); var method = U16(fixedHeader, 10);
        var crc = U32(fixedHeader, 16); var comp = U32(fixedHeader, 20); var uncomp = U32(fixedHeader, 24);
        var nameLength = U16(fixedHeader, 28); var extraLength = U16(fixedHeader, 30); var commentLength = U16(fixedHeader, 32);
        var variable = Read(input, nameLength + extraLength + commentLength);
        var nameBytes = variable[..nameLength];
        var name = (flags & 0x800) != 0 ? Encoding.UTF8.GetString(nameBytes) : Encoding.UTF8.GetString(nameBytes);
        var extra = variable.AsSpan(nameLength, extraLength).ToArray();
        var zip64 = ReadZip64Values(extra, comp == uint.MaxValue, uncomp == uint.MaxValue, U32(fixedHeader, 42) == uint.MaxValue);
        var localOffset = U32(fixedHeader, 42) == uint.MaxValue ? zip64.Offset : U32(fixedHeader, 42);
        var compressedSize = comp == uint.MaxValue ? zip64.Compressed : comp;
        var uncompressedSize = uncomp == uint.MaxValue ? zip64.Uncompressed : uncomp;
        input.Position = start + 46 + nameLength + extraLength + commentLength;
        return new Entry(name, fixedHeader.Concat(variable).ToArray(), localOffset, compressedSize, uncompressedSize, crc, flags, method);
    }

    private static void CopyRawEntry(FileStream input, Entry entry, FileStream output)
    {
        input.Position = entry.LocalOffset;
        var header = Read(input, 30);
        if (U32(header, 0) != Local) throw new InvalidDataException($"Invalid local header for {entry.Name}.");
        var variable = Read(input, U16(header, 26) + U16(header, 28));
        output.Write(header); output.Write(variable); CopyBytes(input, output, entry.CompressedSize);
        if ((entry.Flags & 8) != 0) CopyBytes(input, output, DescriptorLength(input, entry));
    }

    private static void WriteChanged(FileStream output, string name, string path, out byte[] central)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name); var offset = output.Position;
        var local = new byte[30]; W32(local, 0, Local); W16(local, 4, 20); W16(local, 6, 0x808); W16(local, 8, 8);
        W16(local, 10, 0); W16(local, 12, 0); W16(local, 26, (ushort)nameBytes.Length); W16(local, 28, 0);
        output.Write(local); output.Write(nameBytes);
        long size = 0, compressed = 0; uint crc;
        var checksum = new System.IO.Hashing.Crc32();
        using (var raw = File.OpenRead(path))
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            var buffer = new byte[1024 * 1024];
            int read;
            while ((read = raw.Read(buffer, 0, buffer.Length)) > 0)
            {
                checksum.Append(buffer.AsSpan(0, read));
                size += read;
                deflate.Write(buffer, 0, read);
            }
        }
        crc = BitConverter.ToUInt32(checksum.GetCurrentHash());
        compressed = output.Position - offset - 30 - nameBytes.Length;
        WriteU32(output, Descriptor); WriteU32(output, crc); WriteU32(output, (uint)compressed); WriteU32(output, (uint)size);
        central = CentralRecord(nameBytes, crc, compressed, size, offset);
    }

    private static byte[] CentralRecord(byte[] name, uint crc, long comp, long size, long offset)
    {
        var b = new byte[46 + name.Length]; W32(b, 0, Central); W16(b, 4, 20); W16(b, 6, 20); W16(b, 8, 0x808); W16(b, 10, 8);
        W16(b, 12, 0); W16(b, 14, 0); W32(b, 16, crc); W32(b, 20, (uint)comp); W32(b, 24, (uint)size);
        W16(b, 28, (ushort)name.Length); W16(b, 30, 0); W16(b, 32, 0); W16(b, 34, 0); W16(b, 36, 0); W32(b, 38, 0); W32(b, 42, (uint)offset);
        name.CopyTo(b, 46); return b;
    }

    private static byte[] PatchOffset(byte[] central, long newOffset, long oldOffset)
    {
        var copy = (byte[])central.Clone(); if (newOffset > uint.MaxValue) throw new NotSupportedException("ZIP64 offsets require a changed central extra field."); W32(copy, 42, (uint)newOffset); return copy;
    }

    private static void WriteZip64End(FileStream output, long count, long size, long offset)
    {
        var zip64Offset = output.Position; WriteU32(output, Zip64End); WriteU64(output, 44); WriteU16(output, 45); WriteU16(output, 45); WriteU32(output, 0); WriteU32(output, 0); WriteU64(output, count); WriteU64(output, count); WriteU64(output, size); WriteU64(output, offset);
        WriteU32(output, Zip64Locator); WriteU32(output, 0); WriteU64(output, zip64Offset); WriteU32(output, 1);
        WriteU32(output, End); WriteU16(output, 0); WriteU16(output, 0); WriteU16(output, ushort.MaxValue); WriteU16(output, ushort.MaxValue); WriteU32(output, uint.MaxValue); WriteU32(output, uint.MaxValue); WriteU16(output, 0);
    }

    private static long DescriptorLength(FileStream input, Entry e) { var p = input.Position; var sig = U32(Read(input, 4), 0); input.Position = p; return sig == Descriptor ? 16 : 12; }
    private static (long Compressed, long Uncompressed, long Offset) ReadZip64Values(byte[] extra, bool compressed, bool uncompressed, bool offset)
    {
        // Most entries in a Zip64 archive still use the normal 32-bit fields.
        // In that case no Zip64 extra field is required for the entry.
        if (!compressed && !uncompressed && !offset)
            return (0, 0, 0);

        var p = 0;
        while (p + 4 <= extra.Length)
        {
            var id = U16(extra, p); var length = U16(extra, p + 2); p += 4;
            if (id != 1) { p += length; continue; }
            var data = p; long uncompValue = 0, compValue = 0, offsetValue = 0;
            if (uncompressed) { uncompValue = (long)U64(extra, data); data += 8; }
            if (compressed) { compValue = (long)U64(extra, data); data += 8; }
            if (offset) offsetValue = (long)U64(extra, data);
            return (compValue, uncompValue, offsetValue);
        }
        throw new InvalidDataException("ZIP64 extra field is missing required values.");
    }
    private static byte[] Read(Stream s, int n) { var b = new byte[n]; s.ReadExactly(b); return b; }
    private static uint U32(byte[] b, int p) => BitConverter.ToUInt32(b, p);
    private static ushort U16(byte[] b, int p) => BitConverter.ToUInt16(b, p);
    private static ulong U64(byte[] b, int p) => BitConverter.ToUInt64(b, p);
    private static void W16(byte[] b, int p, ushort v) => BitConverter.GetBytes(v).CopyTo(b, p);
    private static void W32(byte[] b, int p, uint v) => BitConverter.GetBytes(v).CopyTo(b, p);
    private static void CopyBytes(Stream input, Stream output, long length) { var b = new byte[1024 * 1024]; while (length > 0) { var n = input.Read(b, 0, (int)Math.Min(b.Length, length)); if (n == 0) throw new EndOfStreamException(); output.Write(b, 0, n); length -= n; } }
    private static void WriteU16(Stream s, ushort v) => s.Write(BitConverter.GetBytes(v));
    private static void WriteU32(Stream s, uint v) => s.Write(BitConverter.GetBytes(v));
    private static void WriteU64(Stream s, long v) => s.Write(BitConverter.GetBytes(v));

}
