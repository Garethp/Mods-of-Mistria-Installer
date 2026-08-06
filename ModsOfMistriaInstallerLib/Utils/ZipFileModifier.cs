using System.IO.Compression;
using SixLabors.ImageSharp.Advanced;
using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class ZipFileModifier(ZipArchive archive) : IFileModifier
{
    private static readonly DateTimeOffset DeterministicEntryTime =
        new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private ZipArchive _archive = archive;

    public bool Exists(string file)
    {
        file = file.Replace('\\', '/');
        return _archive.GetEntry(file) != null || _archive.GetEntry($"{file}/") != null;
    }

    public string[] FindFiles(string path, string pattern)
    {
        path = path.Replace('\\', '/');
        return _archive
                .Entries
                .Select(entry => entry.FullName)
                .Where(name => 
                    name.StartsWith(path) && name.Contains(pattern) && !name.EndsWith('/')
                )
                .ToArray()
            ;
    }

    public string Read(string file)
    {
        var stream = GetReadStream(file);
        using var reader = new StreamReader(stream);
        var contents = reader.ReadToEnd();
        
        reader.Close();

        return contents;
    }

    public Stream GetReadStream(string file)
    {
        file = file.Replace('\\', '/');
        var entry = _archive.GetEntry(file);
        if (entry == null)
            throw new FileNotFoundException(file);

        return entry.Open();
    }

    public void Write(string file, string contents)
    {
        // String length is UTF-16 code units, not the UTF-8 byte count used by
        // the archive entry. Direct byte writes preserve Cyrillic text.
        Write(file, Encoding.UTF8.GetBytes(contents));
    }

    public void Write(string file, byte[] contents)
    {
        // Update mode opens existing entries in place, so truncate first or a
        // shorter write keeps the old entry's tail
        using var stream = GetWriteStream(file);
        stream.SetLength(0);
        stream.Write(contents);
    }

    public Stream GetWriteStream(string file)
    {
        file = file.Replace('\\', '/');
        var entry = _archive.GetEntry(file);
        if (entry == null)
        {
            entry = _archive.CreateEntry(file);
            // Mod-generated entries must not receive the current clock time;
            // otherwise identical rebuilds produce different ZIP bytes.
            entry.LastWriteTime = DeterministicEntryTime;
        }
        
        var stream = entry.Open();
        return stream;
    }

    public bool ConditionalRestoreBackup(string file, Func<bool> condition)
    {
        return true;
    }

    public void Close()
    {
        _archive.Dispose();
    }
}
