using System.Runtime.InteropServices;

namespace Garethp.ModsOfMistriaInstallerLib.Audio;

// P/Invoke bindings for the slice of FMOD's Core API needed to decode an
// existing FSB5 group's bytes (as extracted by FmodBankFile.ExtractGroup)
// back into per-subsound PCM. Read-only and playback-capable only in the
// API-surface sense - nothing here ever actually plays audio.
//
// Struct layouts and function signatures are taken verbatim from the
// FMOD Core SDK headers (fmod.h/fmod_common.h) bundled alongside fmod64.dll
// in the GPLv3 Fmod-Bank-Tools project (https://github.com/Wouldubeinta/Fmod-Bank-Tools),
// the same DLL this feature targets - not guessed from FMOD documentation,
// which drifts across SDK versions. The call sequence itself is ported
// (logic, not code) from that project's extract_worker.cpp::processSubSounds.
// See FmodBankFile.cs for the shared license/provenance note.
public static class FmodCoreNative
{
    private const string Lib = "fmod64";

    private const int FmodOk = 0;

    private const uint FmodOpenOnly = 0x00002000;
    private const uint FmodOpenMemory = 0x00000800;
    private const uint FmodTimeUnitPcmBytes = 0x00000004;

    // One decoded subsound: raw PCM plus the format fields FSBank's rebuild
    // step needs to re-encode it. Deliberately not a WAV byte blob - the
    // encode side consumes these fields directly (mirroring how
    // rebuild_worker.cpp re-queries FMOD_Sound_GetFormat rather than parsing
    // a WAV header back apart). Use ToWav below only for saving to disk.
    public sealed record DecodedSubsound(string Name, int SampleRate, int Channels, int Bits, byte[] Pcm);

    [StructLayout(LayoutKind.Sequential)]
    private struct FmodCreateSoundExInfo
    {
        public int cbsize;
        public uint length;
        public uint fileoffset;
        public int numchannels;
        public int defaultfrequency;
        public int format;
        public uint decodebuffersize;
        public int initialsubsound;
        public int numsubsounds;
        public IntPtr inclusionlist;
        public int inclusionlistnum;
        public IntPtr pcmreadcallback;
        public IntPtr pcmsetposcallback;
        public IntPtr nonblockcallback;
        public IntPtr dlsname;
        public IntPtr encryptionkey;
        public int maxpolyphony;
        public IntPtr userdata;
        public int suggestedsoundtype;
        public IntPtr fileuseropen;
        public IntPtr fileuserclose;
        public IntPtr fileuserread;
        public IntPtr fileuserseek;
        public IntPtr fileuserasyncread;
        public IntPtr fileuserasynccancel;
        public IntPtr fileuserdata;
        public int filebuffersize;
        public int channelorder;
        public int channelmask;
        public IntPtr initialsoundgroup;
        public uint initialseekposition;
        public int initialseekpostype;
        public int ignoresetfilesystem;
        public uint audioqueuepolicy;
        public uint minmidigranularity;
        public int nonblockthreadid;
        public IntPtr fsbguid;
    }

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_System_Create(out IntPtr system);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_System_Init(IntPtr system, int maxchannels, uint flags, IntPtr extradriverdata);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_System_Release(IntPtr system);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_System_CreateSound(
        IntPtr system, IntPtr nameOrData, uint mode, ref FmodCreateSoundExInfo exinfo, out IntPtr sound);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_Release(IntPtr sound);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_GetNumSubSounds(IntPtr sound, out int numsubsounds);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_GetSubSound(IntPtr sound, int index, out IntPtr subsound);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_SeekData(IntPtr sound, uint pcm);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_GetDefaults(IntPtr sound, out float frequency, out int priority);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_GetFormat(
        IntPtr sound, out int type, out int format, out int channels, out int bits);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_GetLength(IntPtr sound, out uint length, uint lengthtype);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_GetName(IntPtr sound, byte[] name, int namelen);

    [DllImport(Lib, CallingConvention = CallingConvention.StdCall)]
    private static extern int FMOD_Sound_ReadData(IntPtr sound, byte[] buffer, uint length, out uint read);

    // Decodes every subsound embedded in one FSB5 group back to raw PCM.
    // Opens the whole group as a single in-memory sound with FMOD_OPENONLY
    // (never prebuffers/plays) and walks its subsounds, mirroring
    // extract_worker.cpp::processSubSounds.
    public static List<DecodedSubsound> DecodeGroup(byte[] fsb5Bytes)
    {
        var system = IntPtr.Zero;
        var sound = IntPtr.Zero;
        var pinned = GCHandle.Alloc(fsb5Bytes, GCHandleType.Pinned);
        try
        {
            Check(FMOD_System_Create(out system), "FMOD_System_Create");
            Check(FMOD_System_Init(system, 1, 0 /* FMOD_INIT_NORMAL */, IntPtr.Zero), "FMOD_System_Init");

            var exinfo = new FmodCreateSoundExInfo
            {
                cbsize = Marshal.SizeOf<FmodCreateSoundExInfo>(),
                length = (uint)fsb5Bytes.Length,
            };
            Check(
                FMOD_System_CreateSound(
                    system, pinned.AddrOfPinnedObject(), FmodOpenOnly | FmodOpenMemory, ref exinfo, out sound),
                "FMOD_System_CreateSound");

            Check(FMOD_Sound_GetNumSubSounds(sound, out var numSubSounds), "FMOD_Sound_GetNumSubSounds");

            var result = new List<DecodedSubsound>(numSubSounds);
            for (var i = 0; i < numSubSounds; i++)
            {
                Check(FMOD_Sound_GetSubSound(sound, i, out var subsound), "FMOD_Sound_GetSubSound");
                try
                {
                    result.Add(DecodeSubsound(subsound, i));
                }
                finally
                {
                    // Mirrors the reference tool: it releases each subsound
                    // handle after reading it, even though it did not create
                    // that handle directly via System_CreateSound.
                    FMOD_Sound_Release(subsound);
                }
            }

            return result;
        }
        finally
        {
            if (sound != IntPtr.Zero) FMOD_Sound_Release(sound);
            if (system != IntPtr.Zero) FMOD_System_Release(system);
            pinned.Free();
        }
    }

    private static DecodedSubsound DecodeSubsound(IntPtr subsound, int index)
    {
        Check(FMOD_Sound_SeekData(subsound, 0), "FMOD_Sound_SeekData");
        Check(FMOD_Sound_GetDefaults(subsound, out var sampleRate, out _), "FMOD_Sound_GetDefaults");
        Check(FMOD_Sound_GetFormat(subsound, out _, out _, out var channels, out var bits), "FMOD_Sound_GetFormat");
        Check(FMOD_Sound_GetLength(subsound, out var length, FmodTimeUnitPcmBytes), "FMOD_Sound_GetLength");

        var nameBuffer = new byte[64];
        Check(FMOD_Sound_GetName(subsound, nameBuffer, nameBuffer.Length), "FMOD_Sound_GetName");
        var nameEnd = Array.IndexOf(nameBuffer, (byte)0);
        var name = System.Text.Encoding.UTF8.GetString(nameBuffer, 0, nameEnd < 0 ? nameBuffer.Length : nameEnd);
        if (string.IsNullOrEmpty(name)) name = $"sound_{index}";

        var pcm = new byte[length];
        Check(FMOD_Sound_ReadData(subsound, pcm, length, out var read), "FMOD_Sound_ReadData");
        if (read != length) Array.Resize(ref pcm, (int)read);

        return new DecodedSubsound(name, (int)sampleRate, channels, bits, pcm);
    }

    // Wraps raw PCM in a standard 16-byte-fmt-chunk PCM WAV container, for
    // saving to disk / manual inspection. Not used by the encode path, which
    // reads DecodedSubsound's fields directly instead of re-parsing a WAV
    // header - so this is free to be a plain, standards-conformant writer
    // rather than replicate the reference tool's WAV header (which pads its
    // fmt chunk to 18 bytes by over-reading a 2-byte field as 4).
    public static byte[] ToWav(DecodedSubsound subsound)
    {
        var blockAlign = (short)(subsound.Channels * (subsound.Bits / 8));
        var byteRate = subsound.SampleRate * blockAlign;
        var dataLen = subsound.Pcm.Length;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLen);
        writer.Write("WAVE"u8.ToArray());

        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write((short)subsound.Channels);
        writer.Write(subsound.SampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((short)subsound.Bits);

        writer.Write("data"u8.ToArray());
        writer.Write(dataLen);
        writer.Write(subsound.Pcm);

        return stream.ToArray();
    }

    private static void Check(int fmodResult, string call)
    {
        if (fmodResult != FmodOk)
            throw new InvalidOperationException($"{call} failed with FMOD_RESULT {fmodResult}");
    }
}
