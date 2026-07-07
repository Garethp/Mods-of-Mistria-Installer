using System.Diagnostics;

namespace Garethp.ModsOfMistriaInstallerLib;

/// <summary>
/// Watches for a flag file written by the in-game Mods UI requesting a restart.
/// When found, deletes the flag and relaunches FieldsOfMistria.exe.
/// </summary>
public sealed class GameRestartMonitor : IDisposable
{
    private const string FlagFileName = "momi_restart";

    private readonly string _mistriaExePath;
    private readonly string _watchRoot;
    private FileSystemWatcher? _watcher;

    public GameRestartMonitor(string mistriaLocation)
    {
        _mistriaExePath = Path.Combine(mistriaLocation, "FieldsOfMistria.exe");
        _watchRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FieldsOfMistria");
    }

    public void Start()
    {
        if (!Directory.Exists(_watchRoot))
            return;

        _watcher = new FileSystemWatcher(_watchRoot)
        {
            Filter = FlagFileName,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        _watcher.Created += OnFlagCreated;
    }

    public void Stop()
    {
        if (_watcher is null) return;
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
    }

    private async void OnFlagCreated(object sender, FileSystemEventArgs e)
    {
        // Brief delay to ensure game_end() has fully exited the process.
        await Task.Delay(TimeSpan.FromSeconds(2));

        try { File.Delete(e.FullPath); } catch { /* already gone */ }

        if (File.Exists(_mistriaExePath))
            Process.Start(new ProcessStartInfo(_mistriaExePath) { UseShellExecute = true });
    }

    public void Dispose() => Stop();
}
