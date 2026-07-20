using System.IO.Compression;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Store;

// The two-file assets store: assets.zip is live (the game loads it) and
// assets.bak.zip is pristine. Every install is a whole-archive rebuild from
// pristine: copy the backup over the live archive, write both layers into it
// in update mode, commit on dispose. No temp file and no atomic swap; a crash
// mid-install leaves the complete vanilla copy on disk (update mode buffers in
// memory), and the copy and flush windows stay a known risk (D4).
public class AssetsStore(string fomLocation)
{
    private ZipArchive? _archive;

    public string LivePath { get; } = Path.Combine(fomLocation, "assets.zip");

    public string BackupPath { get; } = Path.Combine(fomLocation, "assets.bak.zip");

    // What the live archive tells us. Unreadable is its own state, never
    // unmarked: the unmarked branch copies the live archive over the backup,
    // which against a truncated archive would destroy the only pristine copy.
    private enum LiveState
    {
        Absent,
        Unmarked,
        Marked,
        Unreadable,
    }

    // Make sure assets.bak.zip is the pristine source before anything writes.
    // An unmarked live archive is vanilla (fresh install, or the game
    // updated) → copy it over the backup. A marked or unreadable one needs the
    // backup already there; a missing pair means no game assets at all.
    public void EnsureBackup()
    {
        switch (ReadLiveState())
        {
            case LiveState.Unmarked:
                File.Copy(LivePath, BackupPath, true);
                return;
            case LiveState.Marked when !File.Exists(BackupPath):
                throw new InvalidOperationException(
                    string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));
            case LiveState.Unreadable when !File.Exists(BackupPath):
                throw new InvalidOperationException(
                    string.Format(Resources.CoreStoreUnreadableNoBackup, LivePath, BackupPath));
            case LiveState.Absent when !File.Exists(BackupPath):
                throw new FileNotFoundException(
                    string.Format(Resources.CoreStoreNoArchives, LivePath, BackupPath), LivePath);
        }
    }

    // Copy the backup over the live archive and open it for the rebuild. The
    // copy is the first write action, so a running game fails here with the
    // previous install still live. The open sits in the same guard: a game
    // launched after the copy still holds the file when Update acquires it.
    public IFileModifier BeginRebuild()
    {
        try
        {
            File.Copy(BackupPath, LivePath, true);
            _archive = ZipFile.Open(LivePath, ZipArchiveMode.Update);
        }
        catch (IOException exception) when (exception is not FileNotFoundException)
        {
            throw new IOException(string.Format(Resources.CoreStoreRebuildFailed, LivePath), exception);
        }

        return new ZipFileModifier(_archive);
    }

    // Dispose the archive, which is the flush. A failure here leaves the
    // bootable vanilla copy on disk; re-running the installer heals it.
    public void Commit()
    {
        if (_archive is null)
            throw new InvalidOperationException("Commit without BeginRebuild");

        try
        {
            _archive.Dispose();
        }
        catch (IOException exception)
        {
            throw new IOException(string.Format(Resources.CoreStoreFlushFailed, LivePath), exception);
        }
        finally
        {
            _archive = null;
        }
    }

    // The uninstall guard ladder (D15). Unmarked → the game updated or nothing
    // was ever installed, so clean up the stale backup instead of copying old
    // game files over new ones. Marked or unreadable → restore the pristine
    // copy, refusing when there is none. True when the store changed.
    public bool Uninstall()
    {
        var state = ReadLiveState();
        switch (state)
        {
            case LiveState.Unmarked:
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                return true;
            case LiveState.Absent when !File.Exists(BackupPath):
                Logger.Log(Resources.CoreStoreNothingToUninstall);
                return false;
            case LiveState.Marked or LiveState.Unreadable when !File.Exists(BackupPath):
                throw new InvalidOperationException(
                    string.Format(Resources.CoreStoreBackupMissing, LivePath, BackupPath));
        }

        try
        {
            File.Copy(BackupPath, LivePath, true);
        }
        catch (IOException exception) when (exception is not FileNotFoundException)
        {
            throw new IOException(string.Format(Resources.CoreStoreRestoreFailed, LivePath), exception);
        }

        return true;
    }

    private LiveState ReadLiveState()
    {
        if (!File.Exists(LivePath)) return LiveState.Absent;

        try
        {
            using var archive = ZipFile.OpenRead(LivePath);
            return archive.GetEntry("manifest.toml") is null ? LiveState.Unmarked : LiveState.Marked;
        }
        catch (InvalidDataException)
        {
            return LiveState.Unreadable;
        }
    }
}
