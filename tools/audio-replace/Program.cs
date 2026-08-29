using System.IO.Compression;
using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib.Audio;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return args.Length == 0 ? 1 : 0;
}

var nativeDir = Environment.GetEnvironmentVariable("MOMI_FMOD_NATIVE_DIR");
if (string.IsNullOrEmpty(nativeDir) || !File.Exists(Path.Combine(nativeDir, "fmod64.dll")))
{
    Console.Error.WriteLine(
        "MOMI_FMOD_NATIVE_DIR must point at a folder containing fmod64.dll, fsbank64.dll and " +
        "libfsbvorbis64.dll (see the FMOD Engine SDK, or the bundle inside a Fmod-Bank-Tools release).");
    return 1;
}

// fmod64/fsbank64 aren't beside this exe or on PATH, so DllImport can't find
// them via the OS's normal search - redirect it at MOMI_FMOD_NATIVE_DIR.
NativeLibrary.SetDllImportResolver(typeof(FmodCoreNative).Assembly, (name, _, _) => name switch
{
    "fmod64" => NativeLibrary.Load(Path.Combine(nativeDir, "fmod64.dll")),
    "fsbank64" => NativeLibrary.Load(Path.Combine(nativeDir, "fsbank64.dll")),
    _ => IntPtr.Zero,
});

// fsbank64 loads its Vorbis encoder plugin (libfsbvorbis64) itself; preload
// it explicitly rather than relying on implicit search order finding it.
if (File.Exists(Path.Combine(nativeDir, "libfsbvorbis64.dll")))
    NativeLibrary.Load(Path.Combine(nativeDir, "libfsbvorbis64.dll"));

try
{
    switch (args[0])
    {
        case "list" when args.Length == 3:
            ListTracks(args[1], args[2]);
            return 0;
        case "replace" when args.Length == 6:
            Replace(args[1], args[2], args[3], args[4], args[5]);
            return 0;
        default:
            PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

void PrintUsage()
{
    Console.Error.WriteLine("""
        Usage:
          audio-replace list <assets.zip> <bank-name>
              Prints every track name inside assets/audio/<bank-name>.bank.

          audio-replace replace <assets.zip> <bank-name> <track-name> <new.wav> <output.zip>
              Replaces one track's audio with a plain PCM WAV file and writes
              a new zip. The input zip is never modified.

        Example:
          audio-replace list game/assets.zip Fall
          audio-replace replace game/assets.zip Fall snd_Fall_DanceOfTheLeaves_HidehitoIkumo my_song.wav out.zip
        """);
}

byte[] ReadBankFromZip(string zipPath, string bankName)
{
    using var zip = ZipFile.OpenRead(zipPath);
    var entryPath = $"assets/audio/{bankName}.bank";
    var entry = zip.GetEntry(entryPath) ?? throw new FileNotFoundException($"{entryPath} not found in {zipPath}");
    using var stream = entry.Open();
    using var mem = new MemoryStream();
    stream.CopyTo(mem);
    return mem.ToArray();
}

void ListTracks(string zipPath, string bankName)
{
    var bank = ReadBankFromZip(zipPath, bankName);
    var groups = FmodBankFile.ReadGroups(bank);
    for (var g = 0; g < groups.Count; g++)
    {
        var decoded = FmodCoreNative.DecodeGroup(FmodBankFile.ExtractGroup(bank, g));
        foreach (var s in decoded)
            Console.WriteLine($"[group {g}] {s.Name}  ({s.SampleRate}Hz {s.Channels}ch {s.Bits}bit, {s.Pcm.Length / (double)(s.SampleRate * s.Channels * s.Bits / 8):F1}s)");
    }
}

void Replace(string zipPath, string bankName, string trackName, string newWavPath, string outputZipPath)
{
    var bank = ReadBankFromZip(zipPath, bankName);
    var groups = FmodBankFile.ReadGroups(bank);

    for (var g = 0; g < groups.Count; g++)
    {
        var decoded = FmodCoreNative.DecodeGroup(FmodBankFile.ExtractGroup(bank, g));
        var index = decoded.FindIndex(s => s.Name == trackName);
        if (index < 0) continue;

        Console.WriteLine($"Found '{trackName}' in group {g} (subsound {index} of {decoded.Count}).");
        decoded[index] = FmodCoreNative.FromWav(trackName, File.ReadAllBytes(newWavPath));

        Console.WriteLine("Re-encoding group (this can take a few seconds)...");
        var rebuiltFsb = FsBankNative.EncodeGroup(decoded);
        var rebuiltBank = FmodBankFile.ReplaceGroup(bank, g, rebuiltFsb);

        WriteZipWithReplacedBank(zipPath, $"assets/audio/{bankName}.bank", rebuiltBank, outputZipPath);
        Console.WriteLine($"Written: {outputZipPath}");
        return;
    }

    throw new InvalidOperationException($"track '{trackName}' not found in any group of {bankName}.bank");
}

void WriteZipWithReplacedBank(string sourceZipPath, string entryPath, byte[] newEntryBytes, string outputZipPath)
{
    using var source = ZipFile.OpenRead(sourceZipPath);
    using var outputStream = new FileStream(outputZipPath, FileMode.Create);
    using var output = new ZipArchive(outputStream, ZipArchiveMode.Create);

    foreach (var entry in source.Entries)
    {
        var newEntry = output.CreateEntry(entry.FullName, CompressionLevel.Optimal);
        using var writeStream = newEntry.Open();
        if (entry.FullName == entryPath)
        {
            writeStream.Write(newEntryBytes);
        }
        else
        {
            using var readStream = entry.Open();
            readStream.CopyTo(writeStream);
        }
    }
}
