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

    public string[] FileFiles(string path, string pattern)
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

    public void Write(string file, string contents)
    {
        _resultingFiles[file] = contents;
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