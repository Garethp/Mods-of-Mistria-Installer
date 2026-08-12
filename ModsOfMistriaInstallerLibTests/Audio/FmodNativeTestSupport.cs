using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib.Audio;

namespace ModsOfMistriaInstallerLibTests.Audio;

// Shared by every *LocalTest fixture that P/Invokes into the real FMOD
// native DLLs: NativeLibrary.SetDllImportResolver throws if called twice for
// the same assembly, and each fixture's own OneTimeSetUp can't see whether
// a sibling fixture already installed one - so the guard has to live here,
// not per-fixture.
internal static class FmodNativeTestSupport
{
    private static bool _resolverSet;

    // Returns the configured native DLL folder, or null (never Assert.Ignore
    // - that's a per-fixture decision) if it's unset or missing any of the
    // named DLLs.
    public static string? NativeDirOrNull(params string[] requiredDlls)
    {
        var dir = Environment.GetEnvironmentVariable("MOMI_FMOD_NATIVE_DIR");
        if (string.IsNullOrEmpty(dir)) return null;
        return requiredDlls.All(dll => File.Exists(Path.Combine(dir, dll))) ? dir : null;
    }

    public static void EnsureResolver(string dir)
    {
        if (_resolverSet) return;
        NativeLibrary.SetDllImportResolver(typeof(FmodCoreNative).Assembly, (name, _, _) => name switch
        {
            "fmod64" => NativeLibrary.Load(Path.Combine(dir, "fmod64.dll")),
            "fsbank64" => NativeLibrary.Load(Path.Combine(dir, "fsbank64.dll")),
            _ => IntPtr.Zero,
        });
        _resolverSet = true;
    }
}
