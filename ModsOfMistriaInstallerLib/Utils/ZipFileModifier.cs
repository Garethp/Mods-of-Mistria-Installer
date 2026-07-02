using System.IO.Compression;
using SixLabors.ImageSharp.Advanced;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class ZipFileModifier(ZipArchive archive) : IFileModifier
{
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
        var stream = GetWriteStream(file);
        stream.SetLength(contents.Length);
        using var writer = new StreamWriter(stream);
        writer.Write(contents);
        writer.Close();
    }

    public Stream GetWriteStream(string file)
    {
        file = file.Replace('\\', '/');
        var entry = _archive.GetEntry(file);
        if (entry == null)
            entry = _archive.CreateEntry(file);
        
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