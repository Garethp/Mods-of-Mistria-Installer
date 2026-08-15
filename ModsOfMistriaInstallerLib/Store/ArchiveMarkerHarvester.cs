using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.Store;

// Recovers symbols from the markers stamped on generated extension lines in
// the outgoing archive, the second half of the reseed union. Subordinate to
// the save harvest, since the archive tends to die with the ledger.
public static class ArchiveMarkerHarvester
{
    // mmapi_ext:<point>:<site>:<symbol>, ":vacant" suffix on tombstones.
    // Anchored to end of line so a garbled marker never matches a truncated
    // symbol prefix.
    private static Regex MarkerPattern(ExtensionPoint point) => new(
        $@"mmapi_ext:{Regex.Escape(point.Id)}:{Regex.Escape(point.EnumMemberSite.Id)}"
        + $@":({ExtensionSymbols.ShapeCore})(?::vacant)?(?=[ \t]*$)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private const int MaxSymbolsPerPoint = 256;

    // Reads each declared point's enum file out of the archive and returns
    // the marker symbols per point. Everything fails soft and logged. A
    // missing archive, an entry the zip lacks, or one unreadable point is a
    // skipped scope, never an error, and never silent when it drops data.
    public static Dictionary<string, HashSet<string>> Harvest(string archivePath, SeamCatalog catalog)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        if (!File.Exists(archivePath)) return result;

        ZipPristineSource archive;
        try
        {
            archive = new ZipPristineSource(archivePath);
        }
        catch (Exception exception)
        {
            Logger.Log($"  reseed: outgoing archive unreadable, marker harvest skipped: {exception.Message}");
            return result;
        }

        using (archive)
        {
            foreach (var point in catalog.Extensions)
            {
                try
                {
                    var raw = archive.Read(point.File);
                    if (raw is null) continue;

                    var text = StagingText.Norm(StagingText.Decode(raw));
                    var symbols = new HashSet<string>(StringComparer.Ordinal);
                    foreach (Match match in MarkerPattern(point).Matches(text))
                    {
                        if (symbols.Count >= MaxSymbolsPerPoint)
                        {
                            Logger.Log($"  reseed: marker harvest for '{point.Id}' hit the "
                                       + $"{MaxSymbolsPerPoint}-symbol cap, remaining markers dropped");
                            break;
                        }

                        symbols.Add(match.Groups[1].Value);
                    }

                    if (symbols.Count > 0) result[point.Id] = symbols;
                }
                catch (Exception exception)
                {
                    Logger.Log($"  reseed: marker harvest for '{point.Id}' failed, "
                               + $"point skipped: {exception.Message}");
                }
            }
        }

        return result;
    }
}
