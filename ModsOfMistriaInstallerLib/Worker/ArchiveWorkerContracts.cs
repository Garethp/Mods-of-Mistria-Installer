using System.Text.Json;

namespace Garethp.ModsOfMistriaInstallerLib.Worker;

public sealed record ArchiveWorkerRequest(
    string Operation,
    string MistriaLocation,
    string ModsLocation,
    string[] ModSources,
    string ResponsePath,
    string GateMode = "auto",
    bool StrictLints = false,
    bool FailOnSkip = false);

public sealed record ArchiveWorkerResponse(
    bool Success,
    string Operation,
    string Summary,
    string? Error,
    string[] Installed,
    string[] Skipped)
{
    public static ArchiveWorkerResponse Failed(Exception exception) =>
        new(false, "unknown", "", exception.ToString(), [], []);
}

public static class ArchiveWorkerJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
