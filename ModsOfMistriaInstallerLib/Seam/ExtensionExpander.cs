using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One registrant's line at one site, resolved but not yet spliced.
internal record RenderedLine(string Symbol, long Ordinal, bool Vacant, string Text, string Marker);

// One file's pending mutations, planned against the pre-splice text so that
// every anchor resolves against the same snapshot. Applied in descending
// offset order, so an earlier insertion never shifts a later one's offset.
internal record PlannedSplice(int At, string Block, string SiteId, bool IsAppend);

// Expands every [[extension]] point into the staged engine text. It assigns
// ordinals, renders each registrant's line per site, splices at the anchors.
// Fail-closed, batched into one SeamStagingException. With zero registrants
// and zero vacancies the output is byte-identical to the seam-only stager.
public static class ExtensionExpander
{
    // A registrant's line marker. The {site} component keeps markers unique
    // when one file carries two sites, so the already-present check never
    // trips on a sibling.
    public static string Marker(string pointId, string siteId, string symbol, bool vacant,
        string comment = "//") =>
        $"{comment} mmapi_ext:{pointId}:{siteId}:{symbol}" + (vacant ? ":vacant" : "");

    // The AppliedIds token recorded on every file a point touches, so install
    // summaries and the journal show extension activity beside seam ids.
    public static string AppliedId(string pointId) => $"ext:{pointId}";

    // Expand in place over the seam stager's output. `added` receives whole
    // new files (vacancy stubs, the registry) and stays apart from `staged`,
    // the edited-pristine set.
    public static ExtensionExpansion Expand(SeamCatalog catalog,
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger ledger,
        Dictionary<string, StagedFile> staged,
        IPristineSource pristine)
    {
        var expansion = new ExtensionExpansion();
        var problems = Run(catalog, regs, ledger, staged, pristine, apply: true, out _, expansion);
        if (problems.Count > 0) throw new SeamStagingException("extension staging failed", problems);
        return expansion;
    }

    // The read-only form --seam-check runs. It validates every point
    // zero-registrant against this build. Reports instead of throwing, and
    // mutates nothing. A game update that rewrites the ordinal enum or moves
    // a site anchor fails the check before it fails an install.
    public static IReadOnlyList<SeamProblem> Validate(SeamCatalog catalog,
        IReadOnlyDictionary<string, StagedFile> staged,
        IPristineSource pristine,
        out IReadOnlyDictionary<string, int> anchoredSites)
    {
        // A copy of the dictionary, so a point can never register a file into
        // the caller's stage. The StagedFile objects are shared, which is safe
        // because Run does not reach its mutation block when apply is false -
        // and ShouldValidateZeroRegistrantWithoutMutatingTheStage pins that.
        var scratch = staged.ToDictionary(f => f.Key, f => f.Value);
        var problems = Run(catalog, [], new MemoryExtensionLedger(), scratch, pristine,
            apply: false, out anchoredSites, new ExtensionExpansion());
        return problems;
    }

    private static List<SeamProblem> Run(SeamCatalog catalog,
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger ledger,
        Dictionary<string, StagedFile> staged,
        IPristineSource pristine,
        bool apply,
        out IReadOnlyDictionary<string, int> anchoredSites,
        ExtensionExpansion expansion)
    {
        List<SeamProblem> problems = [];
        Dictionary<string, int> anchored = [];
        List<ExtensionRegistryEntry> registry = [];
        List<ExtensionRegistryEntry> vacants = [];
        anchoredSites = anchored;
        if (catalog.Extensions.Count == 0) return problems;

        foreach (var point in catalog.Extensions)
        {
            anchored[point.Id] = 0;
            ExpandPoint(point, regs, ledger, staged, pristine, apply, problems, anchored,
                expansion, registry, vacants);
        }

        // The registry ships only when something is registered or a vacancy is
        // alive. The vacant table is what npc_is_unlocked's seam consults to
        // keep tombstones out of the journal, so a vacancy-only install still
        // needs it. With neither, no file, and the staged tree is what it would
        // have been without the mechanism.
        if (apply && problems.Count == 0 && registry.Count + vacants.Count > 0)
            expansion.Added[ExtensionRegistryRenderer.RegistryRel] =
                Encoding.UTF8.GetBytes(ExtensionRegistryRenderer.Render(registry, vacants));

        return problems;
    }

    private static void ExpandPoint(ExtensionPoint point,
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger ledger,
        Dictionary<string, StagedFile> staged,
        IPristineSource pristine,
        bool apply,
        List<SeamProblem> problems,
        Dictionary<string, int> anchored,
        ExtensionExpansion expansion,
        List<ExtensionRegistryEntry> registry,
        List<ExtensionRegistryEntry> vacants)
    {
        // load every file this point's sites touch, from the staged text when a
        // seam already edited it (extension sites anchor against the seamed
        // result), else from pristine
        Dictionary<string, StagedFile> files = [];
        var loadFailed = false;
        foreach (var file in point.Files)
        {
            if (staged.TryGetValue(file, out var already))
            {
                files[file] = already;
                continue;
            }

            var raw = pristine.Read(file);
            if (raw is null)
            {
                problems.Add(Problem(point, "", SeamProblemKind.MissingFile,
                    $"site file not found in pristine source: {file}", file));
                loadFailed = true;
                continue;
            }

            try
            {
                files[file] = StagingText.Load(raw);
            }
            catch (DecoderFallbackException exception)
            {
                problems.Add(Problem(point, "", SeamProblemKind.Decode,
                    $"pristine {file} is not UTF-8 ({exception.Message})", file));
                loadFailed = true;
            }
        }

        if (loadFailed) return;

        var enumSite = point.Sites.FirstOrDefault(s => s.Kind == ExtensionSiteKind.EnumMember);
        if (enumSite is null) return;  // the loader already reported this

        var scan = ScanOrdinalEnum(point, files[enumSite.File].Text, problems);
        if (scan is null) return;

        var entries = AssignOrdinals(point, regs, ledger, scan, problems, expansion);
        if (entries is null) return;

        // Render every site's lines against the pre-splice snapshot, then
        // splice. Rendering nothing (zero registrants, zero vacancies) leaves
        // every file untouched, which is the inertness invariant.
        Dictionary<string, List<PlannedSplice>> plans = [];
        foreach (var site in point.Sites)
        {
            var text = files[site.File].Text;
            var lines = RenderSite(point, site, entries);
            var at = ResolveSite(point, site, text, scan, problems, anchored);
            if (at is null || lines.Count == 0) continue;

            if (!CheckMarkers(point, site, text, lines, problems)) continue;

            var block = string.Concat(lines.Select(l => new string(' ', site.Indent) + l.Text + "\n"));
            if (site.Kind == ExtensionSiteKind.Append) block = "\n" + block;

            if (!plans.TryGetValue(site.File, out var planned))
            {
                planned = [];
                plans[site.File] = planned;
            }

            planned.Add(new PlannedSplice(at.Value, block, site.Id,
                site.Kind == ExtensionSiteKind.Append));
        }

        if (!apply) return;

        // Vacancy data, not just vacancy code. A vacant enum member with no
        // fiddle prototype crashes at Game-create, because the roster array is built
        // unconditionally for every ordinal and the Npc constructor
        // dereferences its prototype with no nullish guard. Generating a stub
        // per symbol preserves the archive's 1:1 member-to-data invariant, so
        // every engine call site keeps working, including ones added by a
        // future game build, which is why this beat redirecting the lookup.
        foreach (var entry in entries.Where(e => e.Registration is null))
            EmitVacancyFiles(point, entry, expansion.Added, pristine, problems);

        registry.AddRange(entries
            .Where(e => e.Registration is not null)
            .Select(e => new ExtensionRegistryEntry(point.Id, e.Symbol, e.Ordinal)));
        vacants.AddRange(entries
            .Where(e => e.Registration is null)
            .Select(e => new ExtensionRegistryEntry(point.Id, e.Symbol, e.Ordinal)));

        foreach (var (file, sitePlans) in plans.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var target = files[file];
            var text = target.Text;
            var planned = sitePlans;

            // Append sites normalise the tail to exactly one trailing newline
            // before their block goes on. Do that first, while the recorded
            // offsets still refer to the untouched head of the file. Keyed on
            // IsAppend rather than "offset == length" because an anchor site
            // at end-of-file resolves to the same offset and must not have the
            // file's tail rewritten under it.
            if (planned.Any(p => p.IsAppend))
            {
                var normalised = text.TrimEnd('\n') + "\n";
                planned = planned
                    .Select(p => p.IsAppend ? p with { At = normalised.Length }
                        : p.At > normalised.Length ? p with { At = normalised.Length }
                        : p)
                    .ToList();
                text = normalised;
            }

            foreach (var splice in planned.OrderByDescending(p => p.At).ThenBy(p => p.SiteId, StringComparer.Ordinal))
                text = text[..splice.At] + splice.Block + text[splice.At..];

            target.Text = text;
            if (!staged.ContainsKey(file)) staged[file] = target;
            if (!target.AppliedIds.Contains(AppliedId(point.Id))) target.AppliedIds.Add(AppliedId(point.Id));
        }
    }

    // One vacancy's stub data files. Live registrations get none, because the mod
    // ships its own real data, and a stub would overwrite it.
    private static void EmitVacancyFiles(ExtensionPoint point, ExtensionEntry entry,
        Dictionary<string, byte[]> added, IPristineSource pristine, List<SeamProblem> problems)
    {
        Dictionary<string, string> values = new()
        {
            [ExtensionPlaceholders.Symbol] = entry.Symbol,
            [ExtensionPlaceholders.Ordinal] = entry.Ordinal.ToString(),
        };

        foreach (var vacancyFile in point.VacancyFiles)
        {
            var rel = NormaliseArchivePath(ExtensionPlaceholders.Render(vacancyFile.Path, values));

            // the loader proved the template is safe. This proves the rendered
            // path is, because a symbol is data and data gets checked
            var unsafePath = Utils.PathSafety.PathProblem(rel, $"vacancy file for '{entry.Symbol}'");
            if (unsafePath is not null)
            {
                problems.Add(Problem(point, "", SeamProblemKind.Extension, unsafePath, rel));
                continue;
            }

            // A stub must never land on top of real content. An archive entry
            // it collides with is either a vanilla file or another registrant's
            // data, and overwriting either is worse than refusing.
            if (pristine.Has(rel))
            {
                problems.Add(Problem(point, "", SeamProblemKind.Extension,
                    $"vacancy file for '{entry.Symbol}' would overwrite the existing archive "
                    + $"entry {rel}", rel));
                continue;
            }

            if (added.ContainsKey(rel))
            {
                problems.Add(Problem(point, "", SeamProblemKind.Extension,
                    $"vacancy file for '{entry.Symbol}' collides with another generated file "
                    + $"at {rel}", rel));
                continue;
            }

            added[rel] = Encoding.UTF8.GetBytes(
                ExtensionPlaceholders.Render(vacancyFile.Content, values));
        }
    }

    // Catalog paths are archive-relative ("fiddle/npcs/x.toml"). Staged and
    // added keys carry the "assets/" prefix. Accept either, as the seam
    // catalog's `file` values do.
    private static string NormaliseArchivePath(string rel)
    {
        var path = rel.Replace('\\', '/').TrimStart('/');
        return path.StartsWith("assets/", StringComparison.Ordinal) ? path : $"assets/{path}";
    }

    // The ordinal enum, with the shape the ordinal maths depends on proven.
    // These are Target problems, not Extension ones, because the failure class is a
    // structural locator that stopped matching, i.e. the game changed shape.
    internal static GmlEnumScan? ScanOrdinalEnum(ExtensionPoint point, string text, List<SeamProblem> problems)
    {
        var scans = GmlScanner.ScanEnum(text, point.OrdinalEnum);
        if (scans.Count != 1)
        {
            problems.Add(Problem(point, "", SeamProblemKind.Target,
                $"enum '{point.OrdinalEnum}' declared {scans.Count}x in {point.File} (expected 1) - "
                + "the engine file changed; the extension point needs updating",
                point.File));
            return null;
        }

        var scan = scans[0];
        if (scan.Members.Count == 0)
        {
            problems.Add(Problem(point, "", SeamProblemKind.Target,
                $"enum '{point.OrdinalEnum}' in {point.File} has no members - expected a roster "
                + $"enum closing with the '{point.OrdinalSentinel}' sentinel",
                point.File));
            return null;
        }

        var last = scan.Members[^1];
        if (last.Name != point.OrdinalSentinel)
        {
            problems.Add(Problem(point, "", SeamProblemKind.Target,
                $"enum '{point.OrdinalEnum}' in {point.File} ends with '{last.Name}', not the "
                + $"declared sentinel '{point.OrdinalSentinel}' - generated members are inserted "
                + "immediately before the sentinel, so it must be last",
                point.File, StagingText.CountLines(text, last.Start)));
            return null;
        }

        // The ordinal maths assumes ordinal = index for base members. If the
        // game ever hands one an explicit value, fail loudly rather than
        // compute on an assumption that stopped holding.
        var explicitBase = scan.Members.SkipLast(1).Where(m => m.IsExplicit).ToList();
        if (explicitBase.Count > 0)
        {
            var first = explicitBase[0];
            problems.Add(Problem(point, "", SeamProblemKind.Target,
                $"enum '{point.OrdinalEnum}' in {point.File} gives base member(s) "
                + $"{string.Join(", ", explicitBase.Select(m => $"'{m.Name} = {m.ValueText}'"))} an "
                + "explicit value - ordinal assignment assumes base members are positional; "
                + "the engine changed and the extension point needs re-deriving",
                point.File, StagingText.CountLines(text, first.Start)));
            return null;
        }

        return scan;
    }

    // Ledger assignments plus new registrants, in ordinal order. Null when the
    // ledger and the game disagree badly enough that rendering would be wrong.
    private static List<ExtensionEntry>? AssignOrdinals(ExtensionPoint point,
        IReadOnlyList<ExtensionRegistration> regs,
        IExtensionLedger ledger,
        GmlEnumScan scan,
        List<SeamProblem> problems,
        ExtensionExpansion expansion)
    {
        var baseLen = scan.Members.Count - 1;
        var pointRegs = regs.Where(r => r.PointId == point.Id).ToList();

        // The collector guards same-mod duplicates, so a duplicate here is
        // two mods whose differently split prefixes compose the same symbol.
        // Fail closed naming both, because neither can safely own the member.
        foreach (var group in pointRegs
                     .GroupBy(r => r.Symbol, StringComparer.Ordinal)
                     .Where(g => g.Count() > 1))
        {
            problems.Add(Problem(point, "", SeamProblemKind.Extension,
                $"mods {string.Join(" and ", group.Select(r => $"'{r.ModId}'").Distinct())} both "
                + $"compose the symbol '{group.Key}' - differently split author and name prefixes "
                + "can collide, and neither mod can install while both claim the enum member",
                point.File));
            return null;
        }

        var live = pointRegs.ToDictionary(r => r.Symbol, r => r);
        var assigned = ledger.Assignments(point.Id)
            .OrderBy(a => a.Ordinal)
            .ToList();

        // Saves key this enum's data by the native name form, so a symbol
        // equal to a base member's native spelling would serialize to the
        // same save key as the vanilla member, and the reseed harvest would
        // classify that key as vanilla and never recover it. Two members,
        // one save identity. Reject it for ledgered and live symbols alike.
        var nativeBase = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var member in scan.Members.Take(baseLen))
            nativeBase.TryAdd(ExtensionSymbols.ToNativeName(member.Name), member.Name);
        foreach (var symbol in assigned.Select(a => a.Symbol).Concat(live.Keys).Distinct())
        {
            if (!nativeBase.TryGetValue(symbol, out var baseMember)) continue;
            problems.Add(Problem(point, "", SeamProblemKind.Extension,
                $"symbol '{symbol}' matches the native name of base member '{baseMember}' in "
                + $"'{point.OrdinalEnum}' - both would serialize to the same save key, so the "
                + "symbol cannot be allowed to exist",
                point.File));
            return null;
        }

        // The game grew the enum into an ordinal we already handed out. The
        // install normally repairs this before staging (the automatic
        // rebase), so reaching this check means that repair could not
        // run. Fail closed and point at the log.
        var collided = assigned.Where(a => a.Ordinal < baseLen).ToList();
        if (collided.Count > 0)
        {
            problems.Add(Problem(point, "", SeamProblemKind.Extension,
                $"the game's '{point.OrdinalEnum}' now has {baseLen} base members, which collides "
                + $"with assigned ordinal(s) {string.Join(", ", collided.Select(c => $"{c.Ordinal} ({c.Symbol})"))} - "
                + "the install repairs this automatically, so its failure to do so here is the real "
                + "problem; check the ordinal scan messages earlier in this log",
                point.File));
            return null;
        }

        List<ExtensionEntry> entries = [];
        HashSet<string> seen = [];
        foreach (var assignment in assigned)
        {
            if (!seen.Add(assignment.Symbol))
            {
                problems.Add(Problem(point, "", SeamProblemKind.Extension,
                    $"the ledger records symbol '{assignment.Symbol}' twice", point.File));
                return null;
            }

            entries.Add(new ExtensionEntry(
                assignment.Symbol,
                assignment.Ordinal,
                live.GetValueOrDefault(assignment.Symbol)));
        }

        // new symbols take the next ordinals in a deterministic order, so the
        // same mod set always lands the same assignment
        var next = Math.Max(assigned.Count > 0 ? assigned.Max(a => a.Ordinal) : baseLen - 1, baseLen - 1) + 1;
        foreach (var reg in live.Values
                     .Where(r => !seen.Contains(r.Symbol))
                     .OrderBy(r => r.Symbol, StringComparer.Ordinal))
        {
            var assignment = new ExtensionAssignment(reg.Symbol, next, reg.ModId);
            expansion.NewAssignments.Add(new ExtensionLedgerEntry(point.Id, assignment));
            entries.Add(new ExtensionEntry(reg.Symbol, next, reg));
            next++;
        }

        entries = entries.OrderBy(e => e.Ordinal).ToList();

        // Contiguity. The append-only rule implies it, but a hand-edited or
        // half-written ledger produces a hole, and a hole crashes the game
        // at launch ("34 did not match any NpcId"), before the main menu.
        // This turns that into a staging error naming the gap.
        var expected = baseLen;
        foreach (var entry in entries)
        {
            if (entry.Ordinal == expected)
            {
                expected++;
                continue;
            }

            problems.Add(Problem(point, "", SeamProblemKind.Extension,
                $"ordinal {expected} is unassigned but '{entry.Symbol}' holds {entry.Ordinal} - "
                + $"'{point.OrdinalEnum}' would have a hole, and the engine throws at launch when "
                + "reflection reaches an ordinal with no member (it never reaches the data layer). "
                + "The ledger is inconsistent in a way the automatic install-time rebase did not "
                + "repair; check the ordinal scan messages earlier in this log",
                point.File));
            return null;
        }

        return entries;
    }

    // One line per entry, in ordinal order. A vacancy renders the site's
    // vacancy template, which may be empty, meaning no line at this site.
    private static List<RenderedLine> RenderSite(ExtensionPoint point, ExtensionSite site,
        IReadOnlyList<ExtensionEntry> entries)
    {
        List<RenderedLine> lines = [];
        foreach (var entry in entries)
        {
            var vacant = entry.Registration is null;
            var template = vacant ? site.VacancyTemplate : site.Template;
            if (template.Trim().Length == 0) continue;

            Dictionary<string, string> values = new()
            {
                [ExtensionPlaceholders.Symbol] = entry.Symbol,
                [ExtensionPlaceholders.Ordinal] = entry.Ordinal.ToString(),
            };
            // a vacancy has no mod behind it, so only symbol and ordinal are
            // in scope, since the loader already proved its template names no more
            if (!vacant)
                foreach (var (key, value) in entry.Registration!.RenderedValues)
                    values[key] = value;

            var marker = Marker(point.Id, site.Id, entry.Symbol, vacant, site.Comment);
            lines.Add(new RenderedLine(entry.Symbol, entry.Ordinal, vacant,
                $"{ExtensionPlaceholders.Render(template, values)} {marker}", marker));
        }

        return lines;
    }

    // The char offset a site's block is inserted at, or null when the site
    // could not be resolved. Every site is resolved even with zero lines to
    // render, because anchor rot should fail the install, not wait for a registrant.
    private static int? ResolveSite(ExtensionPoint point, ExtensionSite site, string text,
        GmlEnumScan scan, List<SeamProblem> problems, Dictionary<string, int> anchored)
    {
        switch (site.Kind)
        {
            case ExtensionSiteKind.EnumMember:
                // immediately before the sentinel line. Generated members carry
                // explicit values, so GML resumes auto-numbering after them and
                // the sentinel needs no rewrite.
                anchored[point.Id]++;
                return GmlScanner.LineStart(text, scan.Members[^1].Start);

            case ExtensionSiteKind.Anchor:
            {
                var occurrences = StagingText.CountOccurrences(text, site.Anchor);
                if (occurrences != 1)
                {
                    var hint = StagingText.AnchorHint(site.Anchor, text);
                    var (line, context) = StagingText.ClosestContext(site.Anchor, text);
                    problems.Add(Problem(point, site.Id, SeamProblemKind.Anchor,
                        $"anchor matched {occurrences}x in {site.File} (expected 1) - the engine "
                        + "file changed; the extension point needs updating"
                        + (hint.Length > 0 ? $" ({hint})" : ""),
                        site.File, line, context, hint));
                    return null;
                }

                var start = text.IndexOf(site.Anchor, StringComparison.Ordinal);
                var end = start + site.Anchor.Length;
                if (!StagingText.OwnsItsLines(text, start, end))
                {
                    var anchorLine = StagingText.CountLines(text, start);
                    problems.Add(Problem(point, site.Id, SeamProblemKind.Anchor,
                        $"the anchor shares a line with other code in {site.File} - the "
                        + "insertion is line-wise, so that code would land on the wrong side "
                        + "of the generated block",
                        site.File, anchorLine, StagingText.NumberedExcerpt(text, anchorLine)));
                    return null;
                }

                anchored[point.Id]++;
                return site.Place == "before"
                    ? GmlScanner.LineStart(text, start)
                    : GmlScanner.NextLineStart(text, end);
            }

            case ExtensionSiteKind.Append:
                // A file ending inside an unterminated block comment would
                // swallow the appended block silently. The result installs clean and does
                // nothing. Refuse rather than emit into a comment.
                if (GmlScanner.EndsInsideBlockComment(text))
                {
                    problems.Add(Problem(point, site.Id, SeamProblemKind.Anchor,
                        $"{site.File} ends inside an unterminated block comment - an appended "
                        + "line would be swallowed by it and silently never take effect",
                        site.File));
                    return null;
                }

                anchored[point.Id]++;
                return text.Length;

            default:
                return null;
        }
    }

    // Marker discipline, the seam rule verbatim. A marker already present in
    // the file cannot identify this edit, whether it came from pristine or from
    // an earlier splice.
    private static bool CheckMarkers(ExtensionPoint point, ExtensionSite site, string text,
        IReadOnlyList<RenderedLine> lines, List<SeamProblem> problems)
    {
        var ok = true;
        foreach (var line in lines.Where(l => text.Contains(l.Marker, StringComparison.Ordinal)))
        {
            problems.Add(Problem(point, site.Id, SeamProblemKind.Marker,
                $"marker '{line.Marker}' already appears in {site.File} - it cannot identify "
                + "this edit",
                site.File));
            ok = false;
        }

        return ok;
    }

    private static SeamProblem Problem(ExtensionPoint point, string siteId, SeamProblemKind kind,
        string message, string file, int line = 0, string context = "", string hint = "")
    {
        var entryId = siteId.Length > 0 ? $"ext:{point.Id}:{siteId}" : $"ext:{point.Id}";
        return new SeamProblem($"extension '{point.Id}': {message}", kind, entryId, file, hint, line, context);
    }
}

// One ledger ordinal and the registration holding it, if any. A null
// Registration is a vacancy. The mod is uninstalled but the symbol keeps its
// enum member alive, so a save naming it still resolves.
internal record ExtensionEntry(string Symbol, long Ordinal, ExtensionRegistration? Registration);
