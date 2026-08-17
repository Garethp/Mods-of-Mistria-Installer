using System.Text.Json;
using Garethp.ModsOfMistriaInstallerLib.GmlMods;
using Garethp.ModsOfMistriaInstallerLib.Tools;

namespace Garethp.ModsOfMistriaInstallerLib.Worker;

public static class ArchiveWorkerRunner
{
    public static bool IsWorkerInvocation(IReadOnlyList<string> args) =>
        args.Count == 3 &&
        string.Equals(args[0], "--archive-worker", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(args[1], "--request", StringComparison.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(IReadOnlyList<string> args)
    {
        if (!IsWorkerInvocation(args)) return 2;
        var requestPath = Path.GetFullPath(args[2]);
        try
        {
            var request = JsonSerializer.Deserialize<ArchiveWorkerRequest>(
                              await File.ReadAllTextAsync(requestPath), ArchiveWorkerJson.Options)
                          ?? throw new InvalidDataException("Worker request is empty.");
            var response = request.Operation.Equals("uninstall", StringComparison.OrdinalIgnoreCase)
                ? RunUninstall(request)
                : RunInstall(request);
            await WriteResponseAsync(request.ResponsePath, response);
            return response.Success ? 0 : 1;
        }
        catch (Exception exception)
        {
            var responsePath = TryReadResponsePath(requestPath);
            if (responsePath is not null)
                await WriteResponseAsync(responsePath, ArchiveWorkerResponse.Failed(exception));
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static ArchiveWorkerResponse RunInstall(ArchiveWorkerRequest request)
    {
        var allMods = MistriaLocator.GetMods(request.MistriaLocation, request.ModsLocation);
        var selected = request.ModSources.Length == 0
            ? allMods
            : allMods.Where(mod => request.ModSources.Contains(
                Path.GetFullPath(mod.GetSourcePath()), StringComparer.OrdinalIgnoreCase)).ToList();
        if (selected.Count == 0)
            throw new InvalidOperationException("The worker could not resolve any selected mods.");

        var gateMode = request.GateMode.ToLowerInvariant() switch
        {
            "off" => CompileGateMode.Off,
            "mandatory" => CompileGateMode.Mandatory,
            _ => CompileGateMode.Auto
        };
        var result = new ModInstaller(request.MistriaLocation, request.ModsLocation).InstallMods(
            selected, (message, _) => Console.WriteLine(message),
            new GmlLayerOptions { StrictLints = request.StrictLints, FailOnSkip = request.FailOnSkip },
            gateMode, (mod, phase) => Console.WriteLine($"PHASE|{mod}|{phase}"));
        return new ArchiveWorkerResponse(true, "install", result.Summary(), null,
            result.Installed.Select(mod => mod.GetId()).ToArray(),
            result.Skipped.Select(mod => mod.Id).ToArray());
    }

    private static ArchiveWorkerResponse RunUninstall(ArchiveWorkerRequest request)
    {
        new ModInstaller(request.MistriaLocation, request.ModsLocation).Uninstall();
        return new ArchiveWorkerResponse(true, "uninstall", "Uninstall completed", null, [], []);
    }

    private static async Task WriteResponseAsync(string path, ArchiveWorkerResponse response)
    {
        var temp = path + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(response, ArchiveWorkerJson.Options));
        File.Move(temp, path, true);
    }

    private static string? TryReadResponsePath(string requestPath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(requestPath));
            return document.RootElement.TryGetProperty("responsePath", out var value) ? value.GetString() : null;
        }
        catch { return null; }
    }
}
