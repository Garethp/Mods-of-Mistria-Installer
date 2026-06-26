using System.Text.Json;
using System.Text.Json.Nodes;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Operations;

// Applies MOMI patch semantics when merging mod files into game files.
//
// TOML directives (on a table value):
//   MOMIaction = "remove"  → delete the key from the destination
//   MOMIaction = "merge"   → force a recursive merge even when the default would replace
//
// Table-array directives (on an entry inside a [[table.array]]):
//   MOMIidentify = { key = value, ... }  → find the matching destination entry
//   MOMIaction   = "remove"              → remove the matched entry
//   (no action)                          → merge into the matched entry, or append if not found
public static class MOMIOperations
{
    // ── TOML ──────────────────────────────────────────────────────────────────

    public static void MergeTomlTables(TomlTable destination, TomlTable source)
    {
        foreach (var (key, sourceValue) in source)
        {
            if (sourceValue is TomlTable sourceTable)
            {
                if (IsRemoveCommand(sourceTable))
                {
                    destination.Remove(key);
                    continue;
                }

                if (IsMergeCommand(sourceTable))
                {
                    if (destination.TryGetValue(key, out var destVal) && destVal is TomlTable destTable)
                    {
                        var copy = CloneTable(sourceTable);
                        copy.Remove("MOMIidentify");
                        copy.Remove("MOMIaction");
                        MergeTomlTables(destTable, copy);
                    }
                    continue;
                }
            }

            if (!destination.TryGetValue(key, out var destinationValue))
            {
                destination[key] = CloneValue(sourceValue);
                continue;
            }

            if (sourceValue is TomlTable st && destinationValue is TomlTable dt)
                MergeTomlTables(dt, st);
            else if (sourceValue is TomlTableArray sa && destinationValue is TomlTableArray da)
                MergeTomlTableArrays(da, sa);
            else
                destination[key] = CloneValue(sourceValue);
        }
    }

    private static void MergeTomlTableArrays(TomlTableArray destination, TomlTableArray source)
    {
        foreach (var sourceEntry in source)
        {
            if (sourceEntry.TryGetValue("MOMIidentify", out var identObj) &&
                identObj is TomlTable identifier)
            {
                var match = destination.FirstOrDefault(x => MatchesIdentifier(x, identifier));

                if (match is not null)
                {
                    var action = sourceEntry.TryGetValue("MOMIaction", out var act)
                        ? act?.ToString()
                        : null;

                    if (action is "remove" or "delete")
                    {
                        destination.Remove(match);
                        continue;
                    }

                    var copy = CloneTable(sourceEntry);
                    copy.Remove("MOMIidentify");
                    copy.Remove("MOMIaction");
                    MergeTomlTables(match, copy);
                    continue;
                }
            }

            destination.Add(CloneTable(sourceEntry));
        }
    }

    private static bool MatchesIdentifier(TomlTable candidate, TomlTable identifier)
    {
        foreach (var (key, expectedValue) in identifier)
        {
            if (!candidate.TryGetValue(key, out var actualValue)) return false;
            if (!TomlValuesEqual(actualValue, expectedValue))     return false;
        }
        return true;
    }

    private static bool TomlValuesEqual(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        if (left is TomlTable lt && right is TomlTable rt)
            return MatchesIdentifier(lt, rt) && MatchesIdentifier(rt, lt);

        return Equals(left, right);
    }

    private static bool IsRemoveCommand(TomlTable table) =>
        (table.TryGetValue("MOMIaction", out var a) && a?.ToString() is "remove") ||
        (table.TryGetValue("MOMI", out var m) && m is TomlTable inner &&
         inner.TryGetValue("op", out var op) && op?.ToString() is "remove");

    private static bool IsMergeCommand(TomlTable table) =>
        (table.TryGetValue("MOMIaction", out var a) && a?.ToString() is "merge") ||
        (table.TryGetValue("MOMI", out var m) && m is TomlTable inner &&
         inner.TryGetValue("op", out var op) && op?.ToString() is "merge");

    private static TomlTable CloneTable(TomlTable table)
    {
        var clone = new TomlTable();
        foreach (var (key, value) in table)
            clone[key] = CloneValue(value);
        return clone;
    }

    private static object? CloneValue(object? value) =>
        value is TomlTable t ? CloneTable(t) : value;

    // ── JSON ──────────────────────────────────────────────────────────────────

    public static void MergeJsonObjects(JsonObject destination, JsonObject source)
    {
        foreach (var property in destination)
        {
            if (property.Value is null) continue;

            if (!source.TryGetPropertyValue(property.Key, out var sourceValue))
            {
                source[property.Key] = property.Value.DeepClone();
                continue;
            }

            switch (property.Value)
            {
                case JsonObject dObj when sourceValue is JsonObject sObj:
                    MergeJsonObjects(dObj, sObj);
                    break;
                case JsonArray dArr when sourceValue is JsonArray sArr:
                    foreach (var item in dArr)
                        sArr.Add(item?.DeepClone());
                    break;
                default:
                    source[property.Key] = property.Value.DeepClone();
                    break;
            }
        }
    }
}
