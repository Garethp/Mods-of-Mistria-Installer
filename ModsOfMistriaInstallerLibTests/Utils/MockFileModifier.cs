using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace ModsOfMistriaInstallerLibTests.Utils;

public class MockFileModifier: IFileModifier
{
    private readonly Dictionary<string, string> _originalFiles;
    private readonly Dictionary<string, string> _resultingFiles;

    public MockFileModifier(Dictionary<string, string> files)
    {
        _originalFiles = files;
        _resultingFiles = files;
    }

    public string Read(string fieldsOfMistriaLocation, string file)
    {
        return _resultingFiles[file];
    }

    public void Write(string fieldsOfMistriaLocation, string file, string contents)
    {
        _resultingFiles[file] = contents;
    }

    public bool ConditionalRestoreBackup(string fieldsOfMistriaLocation, string file, Func<bool> condition)
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