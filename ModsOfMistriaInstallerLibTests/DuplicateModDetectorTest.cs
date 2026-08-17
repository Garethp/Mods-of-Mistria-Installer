using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace ModsOfMistriaInstallerLibTests;

public class DuplicateModDetectorTest
{
    [Test]
    public void FindsTwoFolderCopiesWithSameLogicalId()
    {
        using var temp = new TempDirectory();
        var first = CreateMod(temp.Path, "first");
        var second = CreateMod(temp.Path, "second");

        var groups = DuplicateModDetector.Find([first, second]);

        Assert.That(groups, Has.Count.EqualTo(1));
        Assert.That(groups[0].ModId, Is.EqualTo(first.GetId()));
        Assert.That(groups[0].Copies, Has.Count.EqualTo(2));
    }

    [Test]
    public void DoesNotReportTheSameSourceTwice()
    {
        using var temp = new TempDirectory();
        var mod = CreateMod(temp.Path, "same");

        Assert.That(DuplicateModDetector.Find([mod, mod]), Is.Empty);
    }

    private static FolderMod CreateMod(string root, string folder)
    {
        var path = Path.Combine(root, folder);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "manifest.toml"), """
            name = "Example Mod"
            author = "Example Author"
            version = "1.0.0"
            manifestVersion = "1"
            minInstallerVersion = "0.1.0"
            """);
        return FolderMod.FromManifest(path);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Directory.CreateTempSubdirectory("aim-duplicate-test-").FullName;
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
