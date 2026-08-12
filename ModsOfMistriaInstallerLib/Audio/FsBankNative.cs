using System.Runtime.InteropServices;

namespace Garethp.ModsOfMistriaInstallerLib.Audio;

// P/Invoke bindings for the FSBank encode API needed to rebuild an FSB5
// group from a full replacement WAV subsound list. Struct layout and
// function signatures are taken verbatim from fsbank.h, bundled alongside
// fsbank64.dll + libfsbvorbis64.dll in the same Fmod-Bank-Tools project
// referenced in FmodCoreNative.cs and FmodBankFile.cs - same provenance
// note applies.
//
// Writes each WAV to a temp file and uses FSBANK_SUBSOUND::fileNames rather
// than its fileData/fileDataLengths (raw in-memory) alternative: fsbank.h
// documents, and testing confirmed, that FSBank silently drops each
// subsound's embedded name when built from memory
// (FSBANK_WARN_FORCED_DONTWRITENAMES). Whether the game needs those names at
// runtime is unconfirmed, so this uses the file-based path instead - the
// same one rebuild_worker.cpp itself uses and the one already proven
// in-game.
public static class FsBankNative
{
    private const string Lib = "fsbank64";

    private const int FsbankOk = 0;
    private const int FsbankFsbversionFsb5 = 0;
    private const uint FsbankInitNormal = 0x00000000;
    private const int FsbankFormatVorbis = 5;
    private const uint FsbankBuildDefault = 0x00000000;

    // Matches Fmod-Bank-Tools' own shipped config.ini defaults (Format=vorbis,
    // Quality=92, DefaultSettings=true i.e. FSBANK_BUILD_DEFAULT) - settings
    // already confirmed in-game, not an independent guess.
    private const uint DefaultQuality = 92;

    [StructLayout(LayoutKind.Sequential)]
    private struct FsbankSubsound
    {
        public IntPtr fileNames;
        public IntPtr fileData;
        public IntPtr fileDataLengths;
        public uint numFiles;
        public uint overrideFlags;
        public uint overrideQuality;
        public float desiredSampleRate;
        public float percentOptimizedRate;
    }

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FSBank_Init(
        int version, uint flags, uint numSimultaneousJobs,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string cacheDirectory);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FSBank_Release();

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FSBank_Build(
        IntPtr subSounds, uint numSubSounds, int encodeFormat, uint buildFlags, uint quality, IntPtr encryptKey,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string outputFileName);

    // Encodes one subsound per entry into a single FSB5 group, in the exact
    // order given - subsound index order must match the target group's
    // original order for a replacement to land on the right track. Each
    // subsound's WAV bytes are staged to a temp file under its own name, so
    // FSBank can embed that name in the FSB5's names chunk (see the class
    // comment above for why that rules out the in-memory fileData path).
    public static byte[] EncodeGroup(IReadOnlyList<FmodCoreNative.DecodedSubsound> subsounds)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "momi-fsbank-" + Guid.NewGuid());
        var wavDir = Path.Combine(workDir, "wav");
        var cacheDir = Path.Combine(workDir, "cache");
        var outputFile = Path.Combine(workDir, "out.fsb");
        Directory.CreateDirectory(wavDir);
        Directory.CreateDirectory(cacheDir);

        var handles = new List<GCHandle>();
        var unmanaged = new List<IntPtr>();
        try
        {
            Check(FSBank_Init(FsbankFsbversionFsb5, FsbankInitNormal, 2, cacheDir), "FSBank_Init");
            try
            {
                var fsbankSubsounds = new FsbankSubsound[subsounds.Count];
                for (var i = 0; i < subsounds.Count; i++)
                {
                    // FSBank derives the embedded subsound name from the WAV
                    // file's own base name, so uniqueness has to come from a
                    // per-index subdirectory rather than a filename prefix -
                    // any prefix would leak into the name it re-embeds.
                    var subsoundDir = Path.Combine(wavDir, i.ToString());
                    Directory.CreateDirectory(subsoundDir);
                    var wavPath = Path.Combine(subsoundDir, $"{SanitizeFileName(subsounds[i].Name)}.wav");
                    File.WriteAllBytes(wavPath, FmodCoreNative.ToWav(subsounds[i]));

                    // Marshal.StringToCoTaskMemUTF8 would need FreeCoTaskMem,
                    // not FreeHGlobal - encoded and allocated by hand instead
                    // so every entry in `unmanaged` shares one free function.
                    var pathBytes = System.Text.Encoding.UTF8.GetBytes(wavPath + "\0");
                    var pathPtr = Marshal.AllocHGlobal(pathBytes.Length);
                    Marshal.Copy(pathBytes, 0, pathPtr, pathBytes.Length);
                    unmanaged.Add(pathPtr);

                    // fileNames is itself an array (one entry per file
                    // backing this subsound) - numFiles is always 1 here, so
                    // this is a one-element native array holding pathPtr.
                    var fileNamesArray = Marshal.AllocHGlobal(IntPtr.Size);
                    Marshal.WriteIntPtr(fileNamesArray, pathPtr);
                    unmanaged.Add(fileNamesArray);

                    fsbankSubsounds[i] = new FsbankSubsound { fileNames = fileNamesArray, numFiles = 1 };
                }

                var subsoundsHandle = GCHandle.Alloc(fsbankSubsounds, GCHandleType.Pinned);
                handles.Add(subsoundsHandle);

                Check(
                    FSBank_Build(
                        subsoundsHandle.AddrOfPinnedObject(), (uint)fsbankSubsounds.Length, FsbankFormatVorbis,
                        FsbankBuildDefault, DefaultQuality, IntPtr.Zero, outputFile),
                    "FSBank_Build");
            }
            finally
            {
                FSBank_Release();
            }

            return File.ReadAllBytes(outputFile);
        }
        finally
        {
            foreach (var handle in handles) handle.Free();
            foreach (var ptr in unmanaged) Marshal.FreeHGlobal(ptr);
            Directory.Delete(workDir, true);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static void Check(int fsbankResult, string call)
    {
        if (fsbankResult != FsbankOk)
            throw new InvalidOperationException($"{call} failed with FSBANK_RESULT {fsbankResult}");
    }
}
