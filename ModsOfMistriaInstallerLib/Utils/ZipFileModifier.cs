using System.IO.Compression;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class ZipFileModifier(ZipArchive archive) : IFileModifier
{
    private ZipArchive _archive = archive;

    public bool Exists(string file)
    {
        file = file.Replace('\\', '/');
        return _archive.GetEntry(file) != null;
    }

    public string[] FileFiles(string path, string pattern)
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
        file = file.Replace('\\', '/');
        var entry = _archive.GetEntry(file);
        if (entry == null)
            throw new FileNotFoundException(file);

        var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var contents = reader.ReadToEnd();

        return contents;
    }

    public void Write(string file, string contents)
    {
        file = file.Replace('\\', '/');
        var entry = _archive.GetEntry(file);
        if (entry == null)
            entry = _archive.CreateEntry(file);
        
        var stream = entry.Open();
        stream.SetLength(contents.Length);
        using var writer = new StreamWriter(stream);
        writer.Write(contents);
        writer.Close();
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