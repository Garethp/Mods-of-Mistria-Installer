using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Store;

public sealed record InstalledModState(string Id, string Version);

public sealed record RecordedInstallState(
    IReadOnlyList<InstalledModState> Mods,
    string PristineSha256,
    string GeneratedLiveSha256,
    DateTimeOffset? InstalledAtUtc);

// Owns the assets.zip transaction. The live archive is never opened for
// writing: every install is rebuilt from a verified pristine archive into a
// same-directory temporary archive and published only after validation.
public class AssetsStore(string fomLocation)
{
    private ZipArchive? _archive;
    private string? _temporaryPath;

    public string LivePath { get; } = Path.Combine(fomLocation, "assets.zip");
    public string BackupPath { get; } = Path.Combine(fomLocation, "assets.bak.zip");
    public string TemporaryPath { get; } = Path.Combine(fomLocation, "assets.momi.tmp.zip");
    public string StatePath { get; } = Path.Combine(fomLocation, "assets.momi.state.toml");
    private string GameExecutablePath { get; } = Path.Combine(fomLocation, "FieldsOfMistria.exe");

    private enum LiveState { Absent, Unmarked, Marked, Unreadable }

    public void EnsureBackup()
    {
        var backupHash = EnsureReadableBackup();
        var state = ReadState();
        var liveState = ReadLiveState();

        if (state is not null && backupHash is null)
            throw new InvalidOperationException(string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));

        if (state is not null && backupHash != state.PristineSha256)
            throw new InvalidOperationException("The MOMI state file does not match the verified pristine archive; the backup was preserved.");

        if (liveState == LiveState.Absent)
        {
            if (backupHash is null)
                throw new FileNotFoundException(string.Format(Resources.CoreStoreNoArchives, LivePath, BackupPath), LivePath);
            return;
        }

        if (liveState == LiveState.Unreadable)
        {
            if (backupHash is null)
                throw new InvalidOperationException(string.Format(Resources.CoreStoreUnreadableNoBackup, LivePath, BackupPath));
            return;
        }

        var liveHash = Sha256File(LivePath);
        if (state is not null)
        {
            if (liveHash == state.GeneratedLiveSha256 || liveHash == state.PristineSha256)
                return;

            // Steam can replace assets.zip while leaving MOMI's state file and
            // old pristine backup behind. A valid, unmarked archive plus a
            // changed game executable is strong evidence of a game update.
            // Adopt it as the new pristine source without trusting arbitrary
            // external edits to assets.zip.
            if (liveState == LiveState.Unmarked && IsGameUpdate(state))
            {
                AdoptVanillaUpdate(liveHash);
                return;
            }

            throw UnknownLiveArchive(liveHash, state);
        }

        // Legacy installs have no state file. An unmarked live archive is the
        // only safe legacy signal that it is vanilla; establish the backup.
        // Once state exists, the branch above refuses to overwrite a verified
        // pristine archive merely because manifest.toml is absent.
        if (liveState == LiveState.Unmarked)
        {
            CopyVerified(LivePath, BackupPath);
            return;
        }

        if (backupHash is null)
            throw new InvalidOperationException(string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));
    }

    public IFileModifier BeginRebuild()
    {
        EnsureReadableBackup();
        Abort();

        try
        {
            File.Copy(BackupPath, TemporaryPath, true);
            _temporaryPath = TemporaryPath;
            _archive = ZipFile.Open(TemporaryPath, ZipArchiveMode.Update);
            return new ZipFileModifier(_archive);
        }
        catch (IOException exception) when (exception is not FileNotFoundException)
        {
            Abort();
            throw new IOException(string.Format(Resources.CoreStoreRebuildFailed, LivePath), exception);
        }
    }

    public void Commit() => Commit([]);

    public void Commit(IEnumerable<InstalledModState> installedMods)
    {
        if (_archive is null || _temporaryPath is null)
            throw new InvalidOperationException("Commit without BeginRebuild");

        try
        {
            _archive.Dispose();
            _archive = null;

            ValidateArchive(_temporaryPath);
            var pristineHash = Sha256File(BackupPath);
            var generatedHash = Sha256File(_temporaryPath);
            var stateText = SerializeState(pristineHash, generatedHash, installedMods);
            Toml.ParseToml(stateText);
            var stateTemp = StatePath + ".tmp";
            SafeDelete(stateTemp);
            WriteExclusiveReplacement(stateTemp, stateText);

            AtomicReplace(_temporaryPath, LivePath);
            _temporaryPath = null;

            AtomicReplace(stateTemp, StatePath);
        }
        catch (IOException exception) when (_archive is null)
        {
            CleanupTemporary();
            SafeDelete(StatePath + ".tmp");
            throw new IOException(string.Format(Resources.CoreStoreFlushFailed, LivePath), exception);
        }
        catch
        {
            Abort();
            SafeDelete(StatePath + ".tmp");
            throw;
        }
    }

    public void Abort()
    {
        try { _archive?.Dispose(); } catch { /* best effort; live is untouched */ }
        _archive = null;
        CleanupTemporary();
    }

    public bool Uninstall()
    {
        var backupHash = EnsureReadableBackup();
        var state = ReadState();
        var liveState = ReadLiveState();

        // Uninstall can be the first operation after Steam updated the game.
        // Bring the pristine source forward before comparing the old state;
        // otherwise a harmless vanilla update would be reported as a damaged
        // or externally modified installation.
        if (state is not null && backupHash is not null &&
            liveState == LiveState.Unmarked && File.Exists(LivePath))
        {
            var currentLiveHash = Sha256File(LivePath);
            if (currentLiveHash != state.GeneratedLiveSha256 &&
                currentLiveHash != state.PristineSha256 && IsGameUpdate(state))
            {
                AdoptVanillaUpdate(currentLiveHash);
                backupHash = currentLiveHash;
                state = ReadState();
            }
        }

        if (state is not null && backupHash is null)
            throw new InvalidOperationException(string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));
        if (state is not null && backupHash != state.PristineSha256)
            throw new InvalidOperationException("The MOMI state file does not match the verified pristine archive; the backup was preserved.");

        if (liveState == LiveState.Absent)
        {
            if (backupHash is null)
            {
                Logger.Log(Resources.CoreStoreNothingToUninstall);
                return false;
            }
            RestoreBackupTransactionally();
            if (state is null) RemoveState();
            else WritePristineState(backupHash!);
            return true;
        }

        if (liveState == LiveState.Unmarked && state is null)
        {
            // A legacy unmarked archive is treated as vanilla. Keep the
            // live archive and remove only the legacy, untracked backup.
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            return true;
        }

        if (backupHash is null)
            throw new InvalidOperationException(string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));

        if (state is not null && (liveState is LiveState.Unmarked or LiveState.Marked))
        {
            var liveHash = Sha256File(LivePath);
            if (liveHash != state.GeneratedLiveSha256 && liveHash != state.PristineSha256)
                throw UnknownLiveArchive(liveHash, state);
        }

        if (state is null && liveState == LiveState.Marked && backupHash is null)
            throw new InvalidOperationException(string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));

        RestoreBackupTransactionally();
        if (state is null) RemoveState();
        else WritePristineState(backupHash);
        return true;
    }

    /// <summary>
    /// Returns whether the live archive has a MOMI installation that can be
    /// offered to the user for removal. This is deliberately independent of
    /// the enabled state of the currently displayed mod list.
    /// </summary>
    public bool HasMomiInstallation()
    {
        try
        {
            var state = ReadState();
            if (state is not null)
            {
                if (!File.Exists(LivePath) || state.GeneratedLiveSha256 == state.PristineSha256)
                    return false;

                // A stale state file must not enable Uninstall after a game
                // update or manual restoration. The live archive must still
                // carry MOMI's marker before it is considered removable.
                using var markedArchive = ZipFile.OpenRead(LivePath);
                return markedArchive.GetEntry("manifest.toml") is not null;
            }

            if (!File.Exists(LivePath)) return false;
            using var archive = ZipFile.OpenRead(LivePath);
            return archive.GetEntry("manifest.toml") is not null;
        }
        catch
        {
            // A damaged or unknown archive must not enable a destructive
            // action. The installer will report the detailed diagnostic if
            // the user repairs the archive and retries.
            return false;
        }
    }

    /// <summary>
    /// Reads the mod IDs and versions recorded by the last successful MOMI
    /// rebuild. A null result means that no valid MOMI state file exists.
    /// </summary>
    public RecordedInstallState? GetRecordedInstallState()
    {
        var state = ReadState();
        if (state is null) return null;

        return new RecordedInstallState(
            state.Mods,
            state.PristineSha256,
            state.GeneratedLiveSha256,
            state.InstalledAtUtc);
    }

    private void RestoreBackupTransactionally()
    {
        EnsureReadableBackup();
        Abort();
        File.Copy(BackupPath, TemporaryPath, true);
        try
        {
            ValidateArchive(TemporaryPath);
            AtomicReplace(TemporaryPath, LivePath);
        }
        catch (IOException exception)
        {
            CleanupTemporary();
            throw new IOException(string.Format(Resources.CoreStoreRestoreFailed, LivePath), exception);
        }
    }

    private string? EnsureReadableBackup()
    {
        if (!File.Exists(BackupPath)) return null;
        ValidateArchive(BackupPath);
        return Sha256File(BackupPath);
    }

    private LiveState ReadLiveState()
    {
        if (!File.Exists(LivePath)) return LiveState.Absent;
        try
        {
            ValidateArchive(LivePath);
            using var archive = ZipFile.OpenRead(LivePath);
            return archive.GetEntry("manifest.toml") is null ? LiveState.Unmarked : LiveState.Marked;
        }
        catch (InvalidDataException) { return LiveState.Unreadable; }
        catch (IOException) { return LiveState.Unreadable; }
    }

    private StoreState? ReadState()
    {
        if (!File.Exists(StatePath)) return null;
        try
        {
            var root = Toml.ParseToml(File.ReadAllText(StatePath));
            if (!root.TryGetValue("schema_version", out var schema) || schema is not long version || version != 1)
                throw new FormatException("Unsupported state schema version");
            DateTimeOffset? installedAt = null;
            if (root.TryGetValue("installed_at_utc", out var installed) && installed is string installedText &&
                DateTimeOffset.TryParse(installedText, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                installedAt = parsed;

            var executableHash = root.TryGetValue("game_executable_sha256", out var executable) && executable is string executableText
                ? executableText
                : null;

            var installedMods = new List<InstalledModState>();
            if (root.TryGetValue("mods", out var modsValue) && modsValue is TomlTableArray mods)
            {
                foreach (var item in mods)
                {
                    if (item is not TomlTable table) continue;
                    if (table.TryGetValue("id", out var id) && id is string modId &&
                        table.TryGetValue("version", out var modVersionValue) && modVersionValue is string modVersion)
                        installedMods.Add(new InstalledModState(modId, modVersion));
                }
            }

            return new StoreState(
                GetString(root, "pristine_sha256"),
                GetString(root, "generated_live_sha256"),
                installedAt,
                executableHash,
                installedMods);
        }
        catch (Exception exception) when (exception is FormatException or IOException or TomlException)
        {
            throw new InvalidOperationException($"MOMI state file is invalid: {StatePath}", exception);
        }
    }

    private static string GetString(TomlTable root, string key) =>
        root.TryGetValue(key, out var value) && value is string text && !string.IsNullOrWhiteSpace(text)
            ? text
            : throw new FormatException($"Missing state field '{key}'");

    private string SerializeState(string pristineHash, string generatedHash, IEnumerable<InstalledModState> installedMods)
    {
        var root = new TomlTable
        {
            ["schema_version"] = 1L,
            ["pristine_sha256"] = pristineHash,
            ["generated_live_sha256"] = generatedHash,
            ["momi_version"] = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
                               ?? GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
            ["installed_at_utc"] = DateTimeOffset.UtcNow.ToString("O"),
        };
        if (File.Exists(GameExecutablePath))
            root["game_executable_sha256"] = Sha256File(GameExecutablePath);
        var mods = new TomlTableArray();
        foreach (var mod in installedMods.OrderBy(m => m.Id, StringComparer.Ordinal))
            mods.Add(new TomlTable { ["id"] = mod.Id, ["version"] = mod.Version });
        root["mods"] = mods;
        return TomlSerializer.Serialize(root);
    }

    private static void ValidateArchive(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasAssets = false;
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            var key = name.TrimEnd('/');
            if (key.Length == 0 || !normalized.Add(key))
                throw new InvalidDataException($"Archive contains a duplicate or invalid normalized path: {entry.FullName}");
            if (name.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)) hasAssets = true;

            using var input = entry.Open();
            input.CopyTo(Stream.Null); // verifies the entry CRC while reading

            if (name.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
            {
                using var text = entry.Open();
                using var reader = new StreamReader(text, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
                try
                {
                    Toml.ParseToml(reader.ReadToEnd());
                }
                catch (Exception exception) when (exception is TomlException or FormatException)
                {
                    throw new InvalidDataException($"Invalid TOML in archive entry '{name}'.", exception);
                }
            }
        }
        if (!hasAssets) throw new InvalidDataException("Archive contains no assets/ game entries");
    }

    private static void CopyVerified(string source, string destination)
    {
        ValidateArchive(source);
        var temp = destination + ".tmp";
        SafeDelete(temp);
        try
        {
            File.Copy(source, temp, false);
            AtomicReplace(temp, destination);
            if (Sha256File(source) != Sha256File(destination))
                throw new IOException("Pristine archive hash verification failed");
        }
        finally
        {
            SafeDelete(temp);
        }
    }

    private void WritePristineState(string pristineHash)
    {
        var stateTemp = StatePath + ".tmp";
        SafeDelete(stateTemp);
        var stateText = SerializeState(pristineHash, pristineHash, []);
        Toml.ParseToml(stateText);
        WriteExclusiveReplacement(stateTemp, stateText);
        AtomicReplace(stateTemp, StatePath);
    }

    private static void WriteExclusiveReplacement(string path, string text) =>
        WriteExclusiveReplacement(path, Encoding.UTF8.GetBytes(text));

    private static void WriteExclusiveReplacement(string path, byte[] bytes)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        stream.Flush(true);
    }

    private static void SafeDelete(string path)
    {
        if (!File.Exists(path)) return;
        File.Delete(path);
    }

    private static void AtomicReplace(string source, string destination)
    {
        try
        {
            if (File.Exists(destination))
                File.Replace(source, destination, null, ignoreMetadataErrors: true);
            else
                File.Move(source, destination);
        }
        catch (PlatformNotSupportedException)
        {
            File.Move(source, destination, overwrite: true);
        }
        catch (IOException) when (!File.Exists(destination))
        {
            File.Move(source, destination);
        }
    }

    private void CleanupTemporary()
    {
        _temporaryPath = null;
        if (File.Exists(TemporaryPath))
        {
            try { File.Delete(TemporaryPath); } catch { /* next run reports a stale temp */ }
        }
    }

    private void RemoveState()
    {
        if (File.Exists(StatePath)) File.Delete(StatePath);
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static InvalidOperationException UnknownLiveArchive(string liveHash, StoreState state) =>
        new($"The live assets archive is not the known MOMI output or pristine archive (SHA-256 {liveHash}); possible game update or external modification. The verified backup was preserved. Reinstall/verify the game before retrying.");

    private bool IsGameUpdate(StoreState state)
    {
        if (!File.Exists(GameExecutablePath)) return false;

        if (!string.IsNullOrWhiteSpace(state.GameExecutableSha256))
            return Sha256File(GameExecutablePath) != state.GameExecutableSha256;

        // State files written by older MOMI versions did not store an
        // executable fingerprint. The timestamp is only a migration fallback;
        // future state files use the stronger hash comparison above.
        return state.InstalledAtUtc is not null &&
               File.GetLastWriteTimeUtc(GameExecutablePath) > state.InstalledAtUtc.Value.UtcDateTime;
    }

    private void AdoptVanillaUpdate(string liveHash)
    {
        PreservePreviousBackup();
        CopyVerified(LivePath, BackupPath);
        WritePristineState(liveHash);
    }

    private void PreservePreviousBackup()
    {
        if (!File.Exists(BackupPath)) return;

        var directory = Path.GetDirectoryName(BackupPath)!;
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
        var previous = Path.Combine(directory, $"assets.bak.momi-previous-{stamp}.zip");
        var temp = previous + ".tmp";
        SafeDelete(temp);
        File.Copy(BackupPath, temp, false);
        ValidateArchive(temp);
        AtomicReplace(temp, previous);
    }

    private sealed record StoreState(
        string PristineSha256,
        string GeneratedLiveSha256,
        DateTimeOffset? InstalledAtUtc,
        string? GameExecutableSha256,
        IReadOnlyList<InstalledModState> Mods);
}
