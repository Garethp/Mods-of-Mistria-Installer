using Garethp.ModsOfMistriaInstallerLib.Seam;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Store;

// The per-game-install ordinal ledger, beside assets.bak.zip. Append-only:
// a symbol keeps its ordinal forever, and the tombstone of an uninstalled
// mod keeps the enum member alive for saves that still name it. Nothing
// reaches disk until the install commits.
public class ExtensionLedgerStore : IExtensionLedger
{
    public const int SupportedVersion = 1;

    public const string FileName = "mmapi-extensions.json";

    private readonly Dictionary<string, List<ExtensionAssignment>> _points = [];

    private ExtensionLedgerStore(string path)
    {
        Path = path;
    }

    public string Path { get; }

    // True when an assignment, rebase move, or reattribution has landed since
    // the last Save, so the install only writes when something actually changed.
    public bool Dirty { get; private set; }

    // True when any point holds any assignment. Load-bearing for the install
    // gate. A ledger tombstone must render its vacancy even when zero mods are
    // installed. The enum member is what keeps a save's name references
    // (date photos) resolving, so a non-empty ledger pulls the GML layer
    // into the install all by itself.
    public bool HasAssignments => _points.Any(p => p.Value.Count > 0);

    // A corrupt or future-versioned file is a hard error, never a silent
    // reset. The message says deletion is recoverable through the reseed.
    public static ExtensionLedgerStore Load(string fomLocation)
    {
        var path = System.IO.Path.Combine(fomLocation, FileName);
        var ledger = new ExtensionLedgerStore(path);
        if (!File.Exists(path)) return ledger;

        try
        {
            var root = JObject.Parse(File.ReadAllText(path));

            var version = (int?)root["version"] ?? 0;
            if (version > SupportedVersion)
                throw new InvalidOperationException(
                    $"the extension ledger at {path} is version {version}, and this installer "
                    + $"supports {SupportedVersion} - a newer MOMI wrote it, so use that one.");
            if (version != SupportedVersion)
                throw Corrupt(path, $"it has no version {SupportedVersion} marker");

            foreach (var (pointId, node) in (JObject?)root["points"] ?? [])
            {
                if (node is not JObject pointNode)
                    throw Corrupt(path, $"point '{pointId}' is not an object");

                List<ExtensionAssignment> assignments = [];
                foreach (var entry in (JArray?)pointNode["assigned"] ?? [])
                {
                    if (entry is not JObject)
                        throw Corrupt(path, $"an entry for point '{pointId}' is not an object");

                    var symbol = (string?)entry["symbol"];
                    var ordinal = (int?)entry["ordinal"];
                    if (symbol is null || ordinal is null || ordinal.Value < 0)
                        throw Corrupt(path, $"an entry for point '{pointId}' has no symbol or no "
                                            + "usable ordinal, and guessing at the missing half "
                                            + "would hand a symbol someone else's ordinal");

                    // The same shape boundary the harvesters enforce. Symbols
                    // land in generated GML, so a file outside the shape is
                    // rejected whole rather than partially trusted.
                    if (!ExtensionSymbols.Shape.IsMatch(symbol))
                        throw Corrupt(path, $"point '{pointId}' records symbol '{symbol}', which is "
                                            + "outside the symbol alphabet no real MOMI install "
                                            + "ever writes");

                    assignments.Add(new ExtensionAssignment(symbol, ordinal.Value, (string?)entry["mod"] ?? ""));
                }

                ledger._points[pointId] = assignments;
            }
        }
        catch (Exception exception) when (exception is JsonException or InvalidCastException
                                              or FormatException or OverflowException or ArgumentException)
        {
            throw Corrupt(path, exception.Message);
        }

        return ledger;
    }

    private static InvalidOperationException Corrupt(string path, string detail) => new(
        $"the extension ledger at {path} is corrupt ({detail}). It records which enum ordinal "
        + "every installed extension owns. Restore it from a backup if you can. If you cannot, "
        + "deleting the file is recoverable: the next install rebuilds the symbols your saves "
        + "and the installed archive still name, minting fresh ordinals, which engine saves "
        + "never see.");

    public IReadOnlyCollection<string> PointIds => _points.Keys;

    public IReadOnlyList<ExtensionAssignment> Assignments(string pointId) =>
        _points.TryGetValue(pointId, out var assignments) ? assignments : [];

    public void Assign(string pointId, ExtensionAssignment assignment)
    {
        if (!_points.TryGetValue(pointId, out var assignments))
        {
            assignments = [];
            _points[pointId] = assignments;
        }

        assignments.Add(assignment);
        Dirty = true;
    }

    // The one sanctioned rewrite, the automatic install-time rebase,
    // after a game update grows the base enum into assigned ordinals, or the
    // update shrinks it. Replaces a point's
    // assignments wholesale. Every symbol must survive. The tombstone
    // guarantee is about names living forever, not ordinals, so dropping one
    // here is a programming error, not a policy call.
    public void Rebase(string pointId, IReadOnlyList<ExtensionAssignment> reassigned)
    {
        var before = Assignments(pointId).Select(a => a.Symbol).Order(StringComparer.Ordinal);
        var after = reassigned.Select(a => a.Symbol).Order(StringComparer.Ordinal);
        if (!before.SequenceEqual(after))
            throw new InvalidOperationException(
                $"rebase for '{pointId}' would change the symbol set - a rebase moves ordinals, "
                + "never adds or removes symbols");

        // A rebase that moves nothing must not mark the ledger dirty, or
        // every install rewrites the file and the write path's failure modes
        // run far more often than they need to.
        if (Assignments(pointId).OrderBy(a => a.Ordinal)
            .SequenceEqual(reassigned.OrderBy(a => a.Ordinal)))
            return;

        _points[pointId] = reassigned.ToList();
        Dirty = true;
    }

    // Correct a stale mod attribution in place, most commonly a reseed's
    // "recovered" placeholder after the registering mod returns. Attribution
    // is diagnostic (the symbol and ordinal are the contract), but a wrong
    // owner misleads anyone reading the ledger to trace a symbol.
    public void Reattribute(string pointId, string symbol, string modId)
    {
        if (!_points.TryGetValue(pointId, out var assignments)) return;
        var index = assignments.FindIndex(a => a.Symbol == symbol);
        if (index < 0 || assignments[index].ModId == modId) return;

        assignments[index] = assignments[index] with { ModId = modId };
        Dirty = true;
    }

    // Write the ledger out. Called only alongside a successful commit. Sorted
    // by ordinal so the file is stable and diffable, because a ledger that reshuffles
    // its own lines makes a real reassignment impossible to spot by eye.
    public void Save()
    {
        var points = new JObject();
        foreach (var (pointId, assignments) in _points.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            points[pointId] = new JObject
            {
                ["assigned"] = new JArray(assignments
                    .OrderBy(a => a.Ordinal)
                    .Select(a => new JObject
                    {
                        ["symbol"] = a.Symbol,
                        ["ordinal"] = a.Ordinal,
                        ["mod"] = a.ModId,
                    })),
            };
        }

        var root = new JObject
        {
            ["version"] = SupportedVersion,
            ["points"] = points,
        };

        // Write-then-rename, so a crash mid-write can truncate only the
        // temp file and never the ledger itself. The rename is atomic on
        // the same volume, which a sibling path guarantees.
        var temp = Path + ".tmp";
        File.WriteAllText(temp, root.ToString(Formatting.Indented));
        File.Move(temp, Path, true);
        Dirty = false;
    }
}
