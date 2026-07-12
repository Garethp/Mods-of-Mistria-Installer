using System.Text;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

/// <summary>
/// Applies manifest-declared, exact-text patches to existing GML sources.
/// All patches for a mod are prepared in memory before any target is written.
/// </summary>
public sealed class GMLPatchInstaller(
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier fileModifier)
    : Installer(fileNameUidMapping)
{
    private static readonly Regex ValidPatchId = new(
        "^[A-Za-z0-9._-]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        var definitions = mod.GetGmlPatches();
        if (definitions.Count == 0) return;

        var patches = PreparePatches(mod, definitions);
        var targets = LoadTargets(patches);

        // Apply in manifest order so a later patch can deliberately anchor to
        // text introduced by an earlier patch in the same target.
        foreach (var patch in patches)
        {
            var target = targets[patch.Target];

            if (ContainsMarker(target.Content, patch.BeginMarker) ||
                ContainsMarker(target.Content, patch.EndMarker))
            {
                throw new InvalidOperationException(
                    $"GML patch '{patch.Definition.Id}' is already present in '{patch.Target}'.");
            }

            var matches = FindMatches(target.Content, patch.Anchor);
            if (matches.Count != patch.Definition.ExpectedMatches)
            {
                throw new InvalidOperationException(
                    $"GML patch '{patch.Definition.Id}' expected " +
                    $"{patch.Definition.ExpectedMatches} exact anchor match(es) in '{patch.Target}', " +
                    $"but found {matches.Count}. No GML files were changed.");
            }

            target.Content = ApplyPatch(target.Content, patch, matches);
        }

        // No validation or matching remains after this point. Each target is
        // committed exactly once, even when it has several sequential patches.
        foreach (var target in targets.Values)
            fileModifier.Write(target.Destination, RestoreNewLines(target.Content, target.NewLine));

        foreach (var patch in patches)
            reportStatus($"Applied GML patch {patch.Definition.Id}: {patch.Target}", "");
    }

    private static List<PreparedPatch> PreparePatches(
        IMod mod,
        IReadOnlyCollection<GmlPatchDefinition> definitions)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var patches = new List<PreparedPatch>(definitions.Count);

        foreach (var definition in definitions)
        {
            ValidatePatchId(definition.Id);
            if (!ids.Add(definition.Id))
            {
                throw new InvalidDataException(
                    $"Duplicate GML patch id '{definition.Id}' in mod '{mod.GetId()}'.");
            }

            if (definition.Operation is not ("insert_after" or "insert_before" or "replace_exact"))
            {
                throw new InvalidDataException(
                    $"GML patch '{definition.Id}' has unsupported operation '{definition.Operation}'.");
            }

            if (definition.ExpectedMatches < 1)
            {
                throw new InvalidDataException(
                    $"GML patch '{definition.Id}' must expect at least one anchor match.");
            }

            var target = ValidateTargetPath(definition.Target, definition.Id);
            var anchorPath = ValidateRelativePath(definition.AnchorFile, "anchorFile", definition.Id);
            var contentPath = ValidateRelativePath(definition.ContentFile, "contentFile", definition.Id);
            var anchor = ReadRequiredSource(mod, anchorPath, "anchor", definition.Id);
            var content = ReadRequiredSource(mod, contentPath, "content", definition.Id);
            var beginMarker = $"// MOMI_GML_PATCH_BEGIN {definition.Id}";
            var endMarker = $"// MOMI_GML_PATCH_END {definition.Id}";

            patches.Add(new PreparedPatch(
                definition,
                target,
                NormalizeNewLines(anchor),
                NormalizeNewLines(content),
                beginMarker,
                endMarker));
        }

        return patches;
    }

    private Dictionary<string, TargetState> LoadTargets(IEnumerable<PreparedPatch> patches)
    {
        var targets = new Dictionary<string, TargetState>(StringComparer.OrdinalIgnoreCase);

        foreach (var patch in patches)
        {
            if (targets.ContainsKey(patch.Target)) continue;

            var destination = DestinationPath(patch.Target);
            if (!fileModifier.Exists(destination))
            {
                throw new FileNotFoundException(
                    $"GML patch target '{patch.Target}' does not exist in the game assets.",
                    destination);
            }

            var original = fileModifier.Read(destination);
            targets.Add(patch.Target, new TargetState(
                destination,
                DetectNewLine(original),
                NormalizeNewLines(original)));
        }

        return targets;
    }

    private static string ApplyPatch(
        string source,
        PreparedPatch patch,
        IReadOnlyList<int> matches)
    {
        var block = BuildMarkedBlock(patch);
        var output = new StringBuilder(source.Length + block.Length * matches.Count);
        var cursor = 0;

        foreach (var match in matches)
        {
            output.Append(source, cursor, match - cursor);

            switch (patch.Definition.Operation)
            {
                case "insert_before":
                    AppendLineBlock(output, block, patch.Anchor[0]);
                    output.Append(patch.Anchor);
                    break;

                case "insert_after":
                    output.Append(patch.Anchor);
                    AppendLineBlock(
                        output,
                        block,
                        CharacterAtOrNull(source, match + patch.Anchor.Length));
                    break;

                case "replace_exact":
                    AppendLineBlock(
                        output,
                        block,
                        CharacterAtOrNull(source, match + patch.Anchor.Length));
                    break;
            }

            cursor = match + patch.Anchor.Length;
        }

        output.Append(source, cursor, source.Length - cursor);
        return output.ToString();
    }

    private static string BuildMarkedBlock(PreparedPatch patch)
    {
        var output = new StringBuilder(
            patch.BeginMarker.Length + patch.Content.Length + patch.EndMarker.Length + 2);
        output.Append(patch.BeginMarker).Append('\n').Append(patch.Content);
        if (!patch.Content.EndsWith('\n')) output.Append('\n');
        output.Append(patch.EndMarker);
        return output.ToString();
    }

    private static void AppendLineBlock(StringBuilder output, string block, char? following)
    {
        if (output.Length > 0 && output[^1] != '\n') output.Append('\n');
        output.Append(block);
        if (following is not null and not '\n') output.Append('\n');
    }

    private static List<int> FindMatches(string source, string anchor)
    {
        var matches = new List<int>();
        var offset = 0;

        while (offset <= source.Length - anchor.Length)
        {
            var match = source.IndexOf(anchor, offset, StringComparison.Ordinal);
            if (match < 0) break;

            matches.Add(match);
            offset = match + anchor.Length;
        }

        return matches;
    }

    private static bool ContainsMarker(string source, string marker) =>
        source.Split('\n').Any(line => line.Trim() == marker);

    private static void ValidatePatchId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ValidPatchId.IsMatch(id))
        {
            throw new InvalidDataException(
                "GML patch ids must be nonempty and contain only ASCII letters, digits, '.', '_', or '-'.");
        }
    }

    private static string ValidateTargetPath(string path, string patchId)
    {
        var normalized = ValidateRelativePath(path, "target", patchId);
        var segments = normalized.Split('/');

        if (segments.Length < 2 ||
            !segments[0].Equals("gml", StringComparison.OrdinalIgnoreCase) ||
            !normalized.EndsWith(".gml", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"GML patch '{patchId}' target must be a .gml file beneath the gml/ directory.");
        }

        return $"gml/{string.Join('/', segments.Skip(1))}";
    }

    private static string ValidateRelativePath(string path, string field, string patchId)
    {
        if (string.IsNullOrWhiteSpace(path) || path.IndexOf('\0') >= 0)
        {
            throw new InvalidDataException(
                $"GML patch '{patchId}' has an empty or invalid {field} path.");
        }

        var normalized = path.Replace('\\', '/');
        if (Path.IsPathRooted(path) || normalized.StartsWith('/') ||
            Regex.IsMatch(normalized, "^[A-Za-z]:", RegexOptions.CultureInvariant))
        {
            throw new InvalidDataException(
                $"GML patch '{patchId}' {field} path must be relative.");
        }

        var segments = normalized.Split('/');
        if (segments.Any(segment => segment is "" or "." or ".."))
        {
            throw new InvalidDataException(
                $"GML patch '{patchId}' {field} path contains an unsafe path segment.");
        }

        return string.Join('/', segments);
    }

    private static string ReadRequiredSource(IMod mod, string path, string kind, string patchId)
    {
        if (!mod.FileExists(path))
        {
            throw new FileNotFoundException(
                $"GML patch '{patchId}' {kind} file does not exist: {path}",
                path);
        }

        var source = mod.ReadFile(path);
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new InvalidDataException(
                $"GML patch '{patchId}' {kind} file is empty: {path}");
        }

        return source;
    }

    private static string DetectNewLine(string source) =>
        source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static string NormalizeNewLines(string source) =>
        source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string RestoreNewLines(string source, string newLine) =>
        newLine == "\n" ? source : source.Replace("\n", newLine, StringComparison.Ordinal);

    private static char? CharacterAtOrNull(string source, int index) =>
        index < source.Length ? source[index] : null;

    private sealed record PreparedPatch(
        GmlPatchDefinition Definition,
        string Target,
        string Anchor,
        string Content,
        string BeginMarker,
        string EndMarker);

    private sealed class TargetState(string destination, string newLine, string content)
    {
        public string Destination { get; } = destination;
        public string NewLine { get; } = newLine;
        public string Content { get; set; } = content;
    }
}
