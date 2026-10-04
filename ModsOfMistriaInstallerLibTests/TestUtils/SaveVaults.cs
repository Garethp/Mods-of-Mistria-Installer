using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

// A test-only writer for the game's save container: one zlib stream over a
// u64le record count, then per record u64le name length, name, u64le body
// length, body. The reseed harvest reads this format, so its tests build it.
public static class SaveVaults
{
    public static byte[] Pack(params (string Name, string Body)[] records)
    {
        using var plain = new MemoryStream();
        void U64(ulong value)
        {
            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            plain.Write(buffer);
        }

        U64((ulong)records.Length);
        foreach (var (name, body) in records)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            U64((ulong)nameBytes.Length);
            plain.Write(nameBytes);
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            U64((ulong)bodyBytes.Length);
            plain.Write(bodyBytes);
        }

        using var packed = new MemoryStream();
        using (var deflate = new ZLibStream(packed, CompressionLevel.Fastest, true))
            deflate.Write(plain.ToArray());
        return packed.ToArray();
    }

    // A fresh folder holding the saves under the game's own file pattern.
    public static string WriteSaves(params byte[][] saves)
    {
        var dir = Directory.CreateTempSubdirectory("momi-vault-test").FullName;
        for (var i = 0; i < saves.Length; i++)
            File.WriteAllBytes(Path.Combine(dir, $"game-1-{i}.sav"), saves[i]);
        return dir;
    }
}
