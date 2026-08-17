using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Diagnostic/experimental modifier that keeps the pristine archive read-only
// and stores changed entries on disk until the final sequential rebuild.
public sealed class StreamingZipFileModifier : IFileModifier
{
    private static readonly DateTimeOffset DeterministicEntryTime =
        new(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly string _sourcePath;
    private readonly string _destinationPath;
    private readonly string _changesDirectory;
    private ZipArchive? _source;
    private readonly Dictionary<string, string> _changes = new(StringComparer.OrdinalIgnoreCase);
    private bool _finalized;

    public StreamingZipFileModifier(string sourcePath, string destinationPath)
    {
        _sourcePath = sourcePath;
        _destinationPath = destinationPath;
        _changesDirectory = destinationPath + ".changes";
        Directory.CreateDirectory(_changesDirectory);
        _source = ZipFile.OpenRead(sourcePath);
    }

    public bool Exists(string file)
    {
        file = Normalize(file);
        var source = RequireSource();
        return _changes.ContainsKey(file) || source.GetEntry(file) is not null || source.GetEntry($"{file}/") is not null;
    }

    public string[] FindFiles(string path, string pattern)
    {
        path = Normalize(path);
        var source = RequireSource();
        return source.Entries
            .Select(entry => entry.FullName)
            .Concat(_changes.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => name.StartsWith(path, StringComparison.OrdinalIgnoreCase)
                           && name.Contains(pattern, StringComparison.OrdinalIgnoreCase)
                           && !name.EndsWith('/'))
            .ToArray();
    }

    public string Read(string file)
    {
        using var stream = GetReadStream(file);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public Stream GetReadStream(string file)
    {
        file = Normalize(file);
        if (_changes.TryGetValue(file, out var changedPath))
            return File.OpenRead(changedPath);

        var entry = RequireSource().GetEntry(file) ?? throw new FileNotFoundException(file);
        return entry.Open();
    }

    public void Write(string file, string contents) => Write(file, Encoding.UTF8.GetBytes(contents));

    public void Write(string file, byte[] contents)
    {
        using var stream = GetWriteStream(file);
        stream.Write(contents);
    }

    public Stream GetWriteStream(string file)
    {
        file = Normalize(file);
        var changePath = GetChangePath(file);
        Directory.CreateDirectory(Path.GetDirectoryName(changePath)!);
        _changes[file] = changePath;
        return new FileStream(changePath, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    public bool ConditionalRestoreBackup(string file, Func<bool> condition) => true;

    public void Close() => FinalizeArchive();

    public void Abort()
    {
        if (_finalized) return;
        ReleaseSource();
        _finalized = true;
        CleanupChanges();
    }

    public void FinalizeArchive()
    {
        if (_finalized) return;

        try
        {
            ReleaseSource();
            if (IsTruthy(Environment.GetEnvironmentVariable("AIM_DIAGNOSTICS_HYBRID_REBUILD")))
            {
                RawZipRebuilder.Rebuild(_sourcePath, _destinationPath, _changes);
            }
            else using (var output = ZipFile.Open(_destinationPath, ZipArchiveMode.Create))
            {
                var copied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using var source = ZipFile.OpenRead(_sourcePath);
                foreach (var entry in source.Entries)
                {
                    CopyEntry(entry, output, copied);
                }

                foreach (var change in _changes)
                {
                    if (copied.Contains(change.Key)) continue;
                    CopyChangedEntry(change.Key, change.Value, output);
                }
            }

            _finalized = true;
        }
        finally
        {
            CleanupChanges();
        }
    }

    private void CopyEntry(ZipArchiveEntry sourceEntry, ZipArchive output, HashSet<string> copied)
    {
        var name = Normalize(sourceEntry.FullName);
        if (_changes.TryGetValue(name, out var changedPath))
            CopyChangedEntry(name, changedPath, output);
        else
        {
            var destination = output.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
            TryCopyTimestamp(sourceEntry, destination);
            using var input = sourceEntry.Open();
            using var target = destination.Open();
            input.CopyTo(target);
        }

        copied.Add(name);
    }

    private static void CopyChangedEntry(string name, string changedPath, ZipArchive output)
    {
        var destination = output.CreateEntry(name, CompressionLevel.Optimal);
        destination.LastWriteTime = DeterministicEntryTime;
        using var input = File.OpenRead(changedPath);
        using var target = destination.Open();
        input.CopyTo(target);
    }

    private static void TryCopyTimestamp(ZipArchiveEntry source, ZipArchiveEntry destination)
    {
        try { destination.LastWriteTime = source.LastWriteTime; }
        catch (ArgumentOutOfRangeException) { destination.LastWriteTime = DeterministicEntryTime; }
    }

    private string GetChangePath(string file)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file))).ToLowerInvariant();
        return Path.Combine(_changesDirectory, hash + ".bin");
    }

    private static string Normalize(string file) => file.Replace('\\', '/').TrimStart('/');

    private static bool IsTruthy(string? value) =>
        value is not null && (value.Equals("1", StringComparison.OrdinalIgnoreCase)
                              || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                              || value.Equals("on", StringComparison.OrdinalIgnoreCase));

    private ZipArchive RequireSource() =>
        _source ?? throw new ObjectDisposedException(nameof(StreamingZipFileModifier));

    private void ReleaseSource()
    {
        var source = _source;
        _source = null;
        source?.Dispose();
    }

    private void CleanupChanges()
    {
        try { Directory.Delete(_changesDirectory, recursive: true); }
        catch { /* best effort; the next rebuild can clean it up */ }
    }
}
