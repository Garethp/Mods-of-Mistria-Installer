using System.IO.Compression;
using SixLabors.ImageSharp.Advanced;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public class ZipFileModifier(ZipArchive archive) : IFileModifier
{
    private ZipArchive _archive = archive;
    private string[]? _entryNames;

    public bool Exists(string file)
    {
        file = file.Replace('\\', '/');
        return _archive.GetEntry(file) != null || _archive.GetEntry($"{file}/") != null;
    }

    public string[] FindFiles(string path, string pattern)
    {
        using var _ = InstallProfiler.Measure("ZipFileModifier.FindFiles");
        path = path.Replace('\\', '/');
        InstallProfiler.AddCount("ZipFileModifier.FindFiles.calls");

        var names = GetEntryNames();
        return names
            .Where(name => name.StartsWith(path, StringComparison.OrdinalIgnoreCase)
                           && name.Contains(pattern, StringComparison.OrdinalIgnoreCase)
                           && !name.EndsWith('/'))
            .ToArray();
    }

    private string[] GetEntryNames()
    {
        if (_entryNames is not null)
            return _entryNames;

        using var _ = InstallProfiler.Measure("ZipFileModifier.BuildEntryIndex");
        _entryNames = _archive.Entries
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .ToArray();
        return _entryNames;
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
