using System.Diagnostics;
using System.Text.Json;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Worker;

namespace Garethp.ModsOfMistriaGUI.Services;

public sealed class ArchiveWorkerClient
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, Process> ActiveWorkers = new();

    public static void StopAll()
    {
        foreach (var process in ActiveWorkers.Values)
            TryKill(process);
    }

    public async Task<ArchiveWorkerResponse> RunAsync(
        ArchiveWorkerRequest request,
        Action<string>? reportPhase,
        CancellationToken cancellationToken)
    {
        var workDirectory = Path.Combine(Path.GetTempPath(), "AIM", "worker", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDirectory);
        var requestPath = Path.Combine(workDirectory, "request.json");
        var responsePath = Path.Combine(workDirectory, "response.json");
        request = request with { ResponsePath = responsePath };
        var processId = 0;

        try
        {
            await File.WriteAllTextAsync(requestPath, JsonSerializer.Serialize(request, ArchiveWorkerJson.Options), cancellationToken);
            using var process = StartWorker(requestPath);
            processId = process.Id;
            ActiveWorkers[process.Id] = process;
            var outputTask = ReadOutputAsync(process, reportPhase, cancellationToken);
            var errorTask = ReadErrorAsync(process, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            await outputTask;
            var workerError = await errorTask;
            ArchiveWorkerResponse? response = null;
            if (File.Exists(responsePath))
                response = JsonSerializer.Deserialize<ArchiveWorkerResponse>(await File.ReadAllTextAsync(responsePath), ArchiveWorkerJson.Options);

            if (response is null)
            {
                var detail = string.IsNullOrWhiteSpace(workerError)
                    ? "No diagnostic output was returned."
                    : workerError.Trim();
                throw new InvalidOperationException(
                    $"Archive worker exited with code {process.ExitCode} without a response.\r\n{detail}");
            }
            if (!response.Success)
                throw new InvalidOperationException(response.Error ?? "Archive worker failed.");
            return response;
        }
        finally
        {
            // The process is removed by its captured ID. Do not inspect HasExited
            // here: the using scope may already have disposed the Process handle,
            // in which case HasExited throws "No process is associated...".
            if (processId != 0)
                ActiveWorkers.TryRemove(processId, out _);
            try { Directory.Delete(workDirectory, recursive: true); } catch { }
        }
    }

    private static Process StartWorker(string requestPath)
    {
        var worker = FindWorker();
        var startInfo = worker.IsDll
            ? new ProcessStartInfo("dotnet", $"\"{worker.Path}\" --request \"{requestPath}\"")
            : new ProcessStartInfo(worker.Path, worker.SelfHost
                ? $"--archive-worker --request \"{requestPath}\""
                : $"--request \"{requestPath}\"");
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start AIM.ArchiveWorker.");
        return process;
    }

    private static async Task ReadOutputAsync(Process process, Action<string>? reportPhase, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line is null) break;
            if (line.StartsWith("PHASE|", StringComparison.Ordinal))
                reportPhase?.Invoke(line[(line.IndexOf('|') + 1)..].Replace('|', ' '));
        }
    }

    private static async Task<string> ReadErrorAsync(Process process, CancellationToken cancellationToken)
    {
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(error))
            Logger.Log(error.Trim());
        return error;
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
    }

    private static WorkerPath FindWorker()
    {
        var processDirectory = Environment.ProcessPath is { } processPath
            ? Path.GetDirectoryName(processPath)
            : null;
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("AIM_ARCHIVE_WORKER_PATH"),
            processDirectory is null ? null : Path.Combine(processDirectory, "AIM.ArchiveWorker.exe"),
            processDirectory is null ? null : Path.Combine(processDirectory, "AIM.ArchiveWorker"),
            Path.Combine(AppContext.BaseDirectory, "AIM.ArchiveWorker.exe"),
            Path.Combine(AppContext.BaseDirectory, "AIM.ArchiveWorker"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ModsOfMistriaArchiveWorker", "bin", "Release", "net10.0", "win-x64", "AIM.ArchiveWorker.exe")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "ModsOfMistriaArchiveWorker", "bin", "Debug", "net10.0", "win-x64", "AIM.ArchiveWorker.dll"))
        };
        foreach (var candidate in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (File.Exists(candidate!)) return new WorkerPath(candidate!, candidate!.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        }

        if (Environment.ProcessPath is { } self)
            return new WorkerPath(self, false, true);

        throw new FileNotFoundException("AIM.ArchiveWorker and the AIM self-worker were not found.");
    }

    private sealed record WorkerPath(string Path, bool IsDll, bool SelfHost = false);
}
