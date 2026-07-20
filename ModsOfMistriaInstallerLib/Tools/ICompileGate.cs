namespace Garethp.ModsOfMistriaInstallerLib.Tools;

// When the gate runs. Auto is the default: on when a backend resolves,
// skipped with a log line when not.
public enum CompileGateMode
{
    Auto,
    Mandatory,
    Off,
}

// The compile gate over staged GML. Compiles only, never executes: the
// backend binary has no run mode, which is a safety property of the shipped
// binary rather than a flag.
public interface ICompileGate
{
    bool Available { get; }

    // Each path as an independent chunk.
    void RunFiles(IReadOnlyList<string> paths);

    // All paths as one compilation unit, the way the boot's global-script
    // compile sees them: this is what catches cross-chunk duplicate exports.
    void RunUnit(IReadOnlyList<string> paths);
}
