using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

public class MockFileModifier: IFileModifier
{
    private readonly Dictionary<string, string> _originalFiles;
    private readonly Dictionary<string, string> _resultingFiles;

    public MockFileModifier(Dictionary<string, string> files)
    {
        _originalFiles = files.ToDictionary(x => x.Key, x => x.Value);
        _resultingFiles = files.ToDictionary(x => x.Key, x => x.Value);
    }

    public bool Exists(string file)
    {
        return _resultingFiles.ContainsKey(file);
    }

    public string[] FindFiles(string path, string pattern)
    {
        return _resultingFiles
            .Keys
            .Where(x => x.StartsWith(path) && x.Contains(pattern) && !x.EndsWith('/'))
            .ToArray()
        ;
    }

    public string Read(string file)
    {
        return _resultingFiles[file];
    }

    public Stream GetReadStream(string file)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(_resultingFiles[file]);
        writer.Flush();
        stream.Position = 0;

        return stream;
    }

    public void Write(string file, string contents)
    {
        _resultingFiles[file] = contents;
    }

    public void Write(string file, byte[] contents)
    {
        _resultingFiles[file] = System.Text.Encoding.UTF8.GetString(contents);
    }

    public Stream GetWriteStream(string file)
    {
        throw new NotImplementedException();
    }

    public bool ConditionalRestoreBackup(string file, Func<bool> condition)
    {
        if (condition())
        {
            _resultingFiles[file] = _originalFiles[file];
            return true;
        }

        return false;
    }

    public string GetFile(string file)
    {
        return _resultingFiles[file];
    }
}