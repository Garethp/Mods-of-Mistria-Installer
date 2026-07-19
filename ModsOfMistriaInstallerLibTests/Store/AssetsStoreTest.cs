using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace ModsOfMistriaInstallerLibTests.Store;

[TestFixture]
public class AssetsStoreTest
{
    private const string PristineGame = "function step_begin() {\n}\n";

    private string _fom = "";

    [SetUp]
    public void CreateTempDir()
    {
        _fom = Path.Combine(Path.GetTempPath(), "momi_store_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_fom);
    }

    [TearDown]
    public void RemoveTempDir()
    {
        Directory.Delete(_fom, true);
    }

    private string LivePath => Path.Combine(_fom, "assets.zip");

    private string BackupPath => Path.Combine(_fom, "assets.bak.zip");

    private static void WriteZip(string path, params (string Name, string Content)[] entries)
    {
        if (File.Exists(path)) File.Delete(path);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            using var stream = archive.CreateEntry(name).Open();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }
    }

    // A vanilla archive as the game ships it: engine files, no marker
    private void WriteVanillaLive() => WriteZip(LivePath, ("assets/gml/objects/Game.gml", PristineGame));

    // An archive a previous install left behind: marked, with a mod entry
    private void WriteInstalledLive() => WriteZip(LivePath,
        ("assets/gml/objects/Game.gml", PristineGame + "// seamed\n"),
        ("assets/gml/scripts/example_mod/State.gml", "// state\n"),
        ("manifest.toml", ""));

    private void WriteVanillaBackup() => WriteZip(BackupPath, ("assets/gml/objects/Game.gml", PristineGame));

    private static Dictionary<string, byte[]> ReadEntries(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        Dictionary<string, byte[]> entries = [];
        foreach (var entry in archive.Entries)
        {
            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            entries[entry.FullName] = buffer.ToArray();
        }

        return entries;
    }

    private static Dictionary<string, string> EntryHashes(string path) =>
        ReadEntries(path).ToDictionary(e => e.Key, e => Convert.ToHexString(SHA256.HashData(e.Value)));

    [Test]
    public void ShouldCreateTheBackupOnAFreshInstall()
    {
        WriteVanillaLive();

        new AssetsStore(_fom).EnsureBackup();

        Assert.That(File.Exists(BackupPath), Is.True);
        Assert.That(File.ReadAllBytes(BackupPath), Is.EqualTo(File.ReadAllBytes(LivePath)));
    }

    [Test]
    public void ShouldRefreshTheBackupAfterAGameUpdate()
    {
        WriteZip(BackupPath, ("assets/gml/objects/Game.gml", "// old engine\n"));
        WriteVanillaLive();

        new AssetsStore(_fom).EnsureBackup();

        Assert.That(ReadEntries(BackupPath)["assets/gml/objects/Game.gml"],
            Is.EqualTo(Encoding.UTF8.GetBytes(PristineGame)));
    }

    [Test]
    public void ShouldKeepTheBackupOnAMarkedLiveArchive()
    {
        WriteInstalledLive();
        WriteVanillaBackup();
        var before = File.ReadAllBytes(BackupPath);

        new AssetsStore(_fom).EnsureBackup();

        Assert.That(File.ReadAllBytes(BackupPath), Is.EqualTo(before));
    }

    [Test]
    public void ShouldThrowOnAMarkedLiveArchiveWithNoBackup()
    {
        WriteInstalledLive();

        var exception = Assert.Throws<InvalidOperationException>(() => new AssetsStore(_fom).EnsureBackup());
        Assert.That(exception!.Message, Does.Contain("verify"));
    }

    [Test]
    public void ShouldAcceptAMissingLiveArchiveWithABackup()
    {
        WriteVanillaBackup();

        Assert.DoesNotThrow(() => new AssetsStore(_fom).EnsureBackup());
    }

    [Test]
    public void ShouldThrowWhenNeitherArchiveExists()
    {
        Assert.Throws<FileNotFoundException>(() => new AssetsStore(_fom).EnsureBackup());
    }

    [Test]
    public void ShouldNeverCopyATruncatedLiveArchiveOverTheBackup()
    {
        WriteVanillaBackup();
        var before = File.ReadAllBytes(BackupPath);
        File.WriteAllBytes(LivePath, before.Take(before.Length / 2).ToArray());

        var store = new AssetsStore(_fom);
        store.EnsureBackup();

        Assert.That(File.ReadAllBytes(BackupPath), Is.EqualTo(before));

        store.BeginRebuild();
        store.Commit();

        Assert.That(ReadEntries(LivePath)["assets/gml/objects/Game.gml"],
            Is.EqualTo(Encoding.UTF8.GetBytes(PristineGame)));
    }

    [Test]
    public void ShouldThrowOnATruncatedLiveArchiveWithNoBackup()
    {
        File.WriteAllBytes(LivePath, Encoding.UTF8.GetBytes("not a zip"));

        var exception = Assert.Throws<InvalidOperationException>(() => new AssetsStore(_fom).EnsureBackup());
        Assert.That(exception!.Message, Does.Contain("verify"));
    }

    [Test]
    public void ShouldRebuildFromThePristineBackupEveryTime()
    {
        WriteInstalledLive();
        WriteVanillaBackup();

        var store = new AssetsStore(_fom);
        store.EnsureBackup();
        store.BeginRebuild();
        store.Commit();

        var entries = ReadEntries(LivePath);
        Assert.That(entries.Keys, Is.EquivalentTo(new[] { "assets/gml/objects/Game.gml" }));
        Assert.That(entries["assets/gml/objects/Game.gml"], Is.EqualTo(Encoding.UTF8.GetBytes(PristineGame)));
    }

    [Test]
    public void ShouldBufferWritesInMemoryUntilCommit()
    {
        WriteVanillaLive();

        var store = new AssetsStore(_fom);
        store.EnsureBackup();
        var modifier = store.BeginRebuild();
        var copiedLength = new FileInfo(LivePath).Length;

        modifier.Write("assets/gml/scripts/mmapi/mmapi.gml", Encoding.UTF8.GetBytes(new string('x', 1024 * 1024)));

        Assert.That(new FileInfo(LivePath).Length, Is.EqualTo(copiedLength));

        store.Commit();

        Assert.That(ReadEntries(LivePath), Contains.Key("assets/gml/scripts/mmapi/mmapi.gml"));
    }

    [Test]
    public void ShouldTruncateAShorterEntryOnByteWrite()
    {
        var longBody = "function step_begin() {\n    // a much longer pristine body\n}\n";
        WriteZip(LivePath, ("assets/gml/objects/Game.gml", longBody));

        var store = new AssetsStore(_fom);
        store.EnsureBackup();
        var modifier = store.BeginRebuild();
        modifier.Write("assets/gml/objects/Game.gml", Encoding.UTF8.GetBytes("// short\n"));
        store.Commit();

        Assert.That(ReadEntries(LivePath)["assets/gml/objects/Game.gml"],
            Is.EqualTo(Encoding.UTF8.GetBytes("// short\n")));
    }

    // Windows-only: Unix does not enforce FileShare semantics, so an open
    // handle never blocks the rebuild there
    [Test]
    [Platform("Win")]
    public void ShouldFailInBeginRebuildWithTheRunningGameHint()
    {
        WriteInstalledLive();
        WriteVanillaBackup();
        var before = File.ReadAllBytes(LivePath);

        var store = new AssetsStore(_fom);
        store.EnsureBackup();

        using (File.Open(LivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var exception = Assert.Throws<IOException>(() => store.BeginRebuild());
            Assert.That(exception!.Message, Does.Contain("running"));
        }

        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(before));
    }

    // Windows-only: Unix does not enforce FileShare semantics, so an open
    // handle never blocks the rebuild there
    [Test]
    [Platform("Win")]
    public void ShouldCarryTheRunningGameHintWhenTheOpenIsBlocked()
    {
        // a game launched between the copy and the open: this handle's share
        // lets File.Copy succeed but blocks ZipFile.Open's Update mode
        WriteInstalledLive();
        WriteVanillaBackup();

        var store = new AssetsStore(_fom);
        store.EnsureBackup();

        using (File.Open(LivePath, FileMode.Open, FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            var exception = Assert.Throws<IOException>(() => store.BeginRebuild());
            Assert.That(exception!.Message, Does.Contain("running"));
        }

        // the copy itself went through, so the failure was the open's
        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(File.ReadAllBytes(BackupPath)));
    }

    // The Unix mirror of the two tests above: .NET's advisory locks make the
    // copy the failing step there regardless of share mode, so a rebuild
    // against an open handle throws the same hint with the previous install
    // still whole. A real game holds no advisory lock on Unix, so production
    // rebuilds do not block at all; this pins the failure-atomicity of
    // BeginRebuild, not running-game detection.
    [Test]
    [Platform(Exclude = "Win")]
    public void ShouldKeepThePreviousInstallLiveWhenTheCopyIsBlocked()
    {
        WriteInstalledLive();
        WriteVanillaBackup();
        var before = File.ReadAllBytes(LivePath);

        var store = new AssetsStore(_fom);
        store.EnsureBackup();

        using (File.Open(LivePath, FileMode.Open, FileAccess.Read,
                   FileShare.ReadWrite | FileShare.Delete))
        {
            var exception = Assert.Throws<IOException>(() => store.BeginRebuild());
            Assert.That(exception!.Message, Does.Contain("running"));
        }

        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(before));
    }

    [Test]
    public void ShouldProduceIdenticalArchivesAcrossTwoRebuilds()
    {
        WriteVanillaLive();

        var store = new AssetsStore(_fom);
        store.EnsureBackup();
        var modifier = store.BeginRebuild();
        modifier.Write("manifest.toml", "");
        modifier.Write("assets/gml/scripts/example_mod/State.gml", Encoding.UTF8.GetBytes("// state\n"));
        store.Commit();
        var first = EntryHashes(LivePath);

        store = new AssetsStore(_fom);
        store.EnsureBackup();
        modifier = store.BeginRebuild();
        modifier.Write("manifest.toml", "");
        modifier.Write("assets/gml/scripts/example_mod/State.gml", Encoding.UTF8.GetBytes("// state\n"));
        store.Commit();

        Assert.That(EntryHashes(LivePath), Is.EqualTo(first));
    }

    [Test]
    public void ShouldRestoreThePristineArchiveOnUninstall()
    {
        WriteInstalledLive();
        WriteVanillaBackup();

        var restored = new AssetsStore(_fom).Uninstall();

        Assert.That(restored, Is.True);
        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(File.ReadAllBytes(BackupPath)));
    }

    [Test]
    public void ShouldCleanUpWithoutRestoringWhenTheLiveArchiveIsUnmarked()
    {
        WriteVanillaLive();
        WriteZip(BackupPath, ("assets/gml/objects/Game.gml", "// stale pre-update engine\n"));
        var before = File.ReadAllBytes(LivePath);

        var restored = new AssetsStore(_fom).Uninstall();

        Assert.That(restored, Is.True);
        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(before));
        Assert.That(File.Exists(BackupPath), Is.False);
    }

    [Test]
    public void ShouldRefuseUninstallOnAMarkedArchiveWithNoBackup()
    {
        WriteInstalledLive();
        var before = File.ReadAllBytes(LivePath);

        var exception = Assert.Throws<InvalidOperationException>(() => new AssetsStore(_fom).Uninstall());

        Assert.That(exception!.Message, Does.Contain("verify"));
        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(before));
    }

    [Test]
    public void ShouldRestoreATruncatedLiveArchiveFromTheBackup()
    {
        WriteVanillaBackup();
        File.WriteAllBytes(LivePath, Encoding.UTF8.GetBytes("not a zip"));

        new AssetsStore(_fom).Uninstall();

        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(File.ReadAllBytes(BackupPath)));
    }

    [Test]
    public void ShouldRefuseUninstallOnATruncatedArchiveWithNoBackup()
    {
        File.WriteAllBytes(LivePath, Encoding.UTF8.GetBytes("not a zip"));

        Assert.Throws<InvalidOperationException>(() => new AssetsStore(_fom).Uninstall());
    }

    [Test]
    public void ShouldRecreateTheLiveArchiveWhenAbsentOnUninstall()
    {
        WriteVanillaBackup();

        var restored = new AssetsStore(_fom).Uninstall();

        Assert.That(restored, Is.True);
        Assert.That(File.ReadAllBytes(LivePath), Is.EqualTo(File.ReadAllBytes(BackupPath)));
    }

    [Test]
    public void ShouldDoNothingOnUninstallWhenNoStoreExists()
    {
        var restored = new AssetsStore(_fom).Uninstall();

        Assert.That(restored, Is.False);
        Assert.That(File.Exists(LivePath), Is.False);
    }

}
