using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.Store;

// Recovers extension symbols from the player's saves when the ledger is
// lost, re-minting them as vacancies before the staging gate runs. Fails
// soft. An unreadable save is a logged skip, never a blocked install, and
// the reader is strictly read-only.
public static class SaveSymbolHarvester
{
    // Caps chosen far above any real save (about 1 MB of plaintext, 36
    // records) so they only ever reject corruption. The symbol cap is public
    // because the installer's archive-marker union enforces the same bound.
    private const long MaxPlainBytes = 64L << 20;
    private const int MaxRecords = 4096;
    private const int MaxNameBytes = 256;
    public const int MaxSymbolsPerPoint = 256;

    public sealed record PointHarvest(int BaseLen, HashSet<string> Symbols, HashSet<string> PristineNames)
    {
        // Set when the per-point cap dropped symbols, so the caller can say
        // so instead of presenting a truncated recovery as a complete one.
        public bool CapHit { get; set; }
    }

    // Per save-harvest-capable extension point, the record name the rule
    // reads. A declared point with no rule here still enters the result (its
    // pristine scan permitting), because the archive-marker harvest covers
    // every declared point and unions into the same entries. A future point
    // whose symbols can appear in saves must add its rule here, and the
    // catalog completeness test forces that decision to be made explicitly.
    private static readonly Dictionary<string, string> RecordForPoint = new(StringComparer.Ordinal)
    {
        ["npc_roster"] = "npcs",
        ["status_effect"] = "player",
    };

    public static bool HasRuleFor(string pointId) => RecordForPoint.ContainsKey(pointId);

    // Walks every save in savesDir and returns, per declared point whose
    // pristine scan succeeded, the base length, the pristine native names,
    // and the set of save-carried symbol names the pristine enum does not
    // explain. The caller subtracts the ledger and unions the remainder.
    public static Dictionary<string, PointHarvest> Harvest(
        string savesDir, SeamCatalog catalog, IPristineSource pristine)
    {
        var result = new Dictionary<string, PointHarvest>(StringComparer.Ordinal);

        foreach (var point in catalog.Extensions)
        {
            var scanned = PristineMembers(point, pristine);
            if (scanned is null)
            {
                Logger.Log($"  reseed: pristine enum scan failed for '{point.Id}', "
                           + "the point cannot be harvested this install");
                continue;
            }

            result[point.Id] = new PointHarvest(scanned.Value.BaseLen,
                new HashSet<string>(StringComparer.Ordinal), scanned.Value.Members);
        }

        var wanted = result.Keys
            .Where(RecordForPoint.ContainsKey)
            .Select(id => RecordForPoint[id])
            .ToHashSet(StringComparer.Ordinal);
        if (wanted.Count == 0) return result;

        List<string> savFiles;
        try
        {
            savFiles = Directory.EnumerateFiles(savesDir, "game-*.sav").ToList();
        }
        catch (Exception exception)
        {
            Logger.Log($"  reseed: could not list saves in {savesDir}: {exception.Message}");
            return result;
        }

        foreach (var savPath in savFiles)
        {
            var records = ReadRecords(savPath, wanted);
            if (records is null)
            {
                Logger.Log($"  reseed: unreadable save skipped: {System.IO.Path.GetFileName(savPath)}");
                continue;
            }

            foreach (var (pointId, harvest) in result)
            {
                if (!RecordForPoint.TryGetValue(pointId, out var recordName)) continue;
                if (!records.TryGetValue(recordName, out var body)) continue;
                try
                {
                    var recognized = pointId switch
                    {
                        "npc_roster" => HarvestNpcRoster(body, harvest),
                        "status_effect" => HarvestStatusEffects(body, harvest),
                        _ => true,
                    };

                    // An unrecognized shape is the engine's format drifting
                    // under the reader. Left silent, an empty harvest would
                    // be indistinguishable from a healthy no-op, which is
                    // the one place this feature cannot afford silence.
                    if (!recognized)
                        Logger.Log($"  reseed: {recordName} in {System.IO.Path.GetFileName(savPath)} "
                                   + "has an unexpected shape, possible save-format drift - "
                                   + "nothing harvested from it");
                }
                catch (JsonException exception)
                {
                    Logger.Log($"  reseed: {recordName} in "
                               + $"{System.IO.Path.GetFileName(savPath)} did not parse: {exception.Message}");
                }
            }
        }

        foreach (var (pointId, harvest) in result)
        {
            if (harvest.CapHit)
                Logger.Log($"  reseed: save harvest for '{pointId}' hit the {MaxSymbolsPerPoint}-symbol "
                           + "cap, further symbols were dropped and the recovery is incomplete");
        }

        return result;
    }

    // Pristine member names and base length for a point, through the same
    // scan the expander and the rebaser use. Null on any scan problem, which
    // skips harvesting the point rather than guessing at the vanilla set.
    private static (int BaseLen, HashSet<string> Members)? PristineMembers(
        ExtensionPoint point, IPristineSource pristine)
    {
        var raw = pristine.Read(point.File);
        if (raw is null) return null;

        string text;
        try
        {
            text = StagingText.Norm(StagingText.Decode(raw));
        }
        catch (Exception)
        {
            return null;
        }

        List<SeamProblem> problems = [];
        var scan = ExtensionExpander.ScanOrdinalEnum(point, text, problems);
        if (scan is null) return null;

        // Saves carry the native name form, not the member spelling. The
        // engine's reflection lowercases the member (Eiland serializes as
        // eiland). Subtracting the member spelling verbatim would harvest
        // the entire vanilla roster. Compare in the native form.
        var names = scan.Members
            .Take(scan.Members.Count - 1)
            .Select(m => ExtensionSymbols.ToNativeName(m.Name))
            .ToHashSet(StringComparer.Ordinal);
        return (scan.Members.Count - 1, names);
    }

    // The npcs record is a struct keyed by NPC name, one key per enum member
    // alive when the save was written, tombstoned vacancies included. Keys
    // the pristine enum does not explain are harvested. Returns false when
    // the record's shape is not the one this rule understands.
    private static bool HarvestNpcRoster(byte[] body, PointHarvest harvest)
    {
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
        foreach (var property in doc.RootElement.EnumerateObject())
            Consider(property.Name, harvest);
        return true;
    }

    // An active status effect serializes its type name in the player stats
    // slot array. Only active effects are referenced by a save, so only
    // active effects need their names kept resolvable. Returns false when
    // the stats path is missing or misshapen.
    private static bool HarvestStatusEffects(byte[] body, PointHarvest harvest)
    {
        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
        if (!doc.RootElement.TryGetProperty("stats", out var stats)
            || stats.ValueKind != JsonValueKind.Object) return false;
        if (!stats.TryGetProperty("status_effects", out var slots)
            || slots.ValueKind != JsonValueKind.Array) return false;

        foreach (var slot in slots.EnumerateArray())
        {
            if (slot.ValueKind != JsonValueKind.Object) continue;
            if (!slot.TryGetProperty("type", out var type)
                || type.ValueKind != JsonValueKind.String) continue;
            var name = type.GetString();
            if (name is not null) Consider(name, harvest);
        }

        return true;
    }

    private static void Consider(string name, PointHarvest harvest)
    {
        if (harvest.PristineNames.Contains(name)) return;
        if (harvest.Symbols.Contains(name)) return;
        if (!ExtensionSymbols.Shape.IsMatch(name)) return;
        if (harvest.Symbols.Count >= MaxSymbolsPerPoint)
        {
            harvest.CapHit = true;
            return;
        }

        harvest.Symbols.Add(name);
    }

    // Inflates the container and returns the wanted records' bodies, or null
    // when anything about the file is not the format described above.
    internal static Dictionary<string, byte[]>? ReadRecords(string savPath, IReadOnlySet<string> wanted)
    {
        byte[] plain;
        try
        {
            using var file = File.OpenRead(savPath);
            using var inflate = new ZLibStream(file, CompressionMode.Decompress);
            using var buffer = new MemoryStream();
            CopyBounded(inflate, buffer, MaxPlainBytes);
            plain = buffer.ToArray();
        }
        catch (Exception)
        {
            return null;
        }

        var output = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var offset = 0;
        if (!TryReadU64(plain, ref offset, out var count) || count > MaxRecords) return null;

        for (ulong i = 0; i < count; i++)
        {
            if (!TryReadU64(plain, ref offset, out var nameLength) || nameLength > MaxNameBytes) return null;
            if ((ulong)(plain.Length - offset) < nameLength) return null;
            var name = Encoding.UTF8.GetString(plain, offset, (int)nameLength);
            offset += (int)nameLength;

            if (!TryReadU64(plain, ref offset, out var bodyLength)) return null;
            if ((ulong)(plain.Length - offset) < bodyLength) return null;
            if (wanted.Contains(name))
                output[name] = plain[offset..(offset + (int)bodyLength)];
            offset += (int)bodyLength;
        }

        return output;
    }

    private static void CopyBounded(Stream from, MemoryStream to, long cap)
    {
        var chunk = new byte[81920];
        int read;
        while ((read = from.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (to.Length + read > cap)
                throw new InvalidDataException("decompressed save exceeds the sanity cap");
            to.Write(chunk, 0, read);
        }
    }

    private static bool TryReadU64(byte[] data, ref int offset, out ulong value)
    {
        value = 0;
        if (data.Length - offset < 8) return false;
        value = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset, 8));
        offset += 8;
        return true;
    }
}
