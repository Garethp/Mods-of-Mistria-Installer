using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Parsing;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class Toml
{
    public static TomlTable ParseToml(string content) =>
        TomlSerializer.Deserialize<TomlTable>(content);

    // Builds the document model from the event parser instead of
    // TomlSerializer: the serializer replaces a table array when its [[name]]
    // group re-opens later in the document, where the TOML spec appends. The
    // seam catalog interleaves [[hook]] and [[seam]] groups, so it needs the
    // spec behaviour.
    public static TomlTable ParseDocument(string content)
    {
        var parser = TomlParser.Create(content, new TomlSerializerOptions());
        TomlTable? root = null;
        Stack<(object Container, string? Key)> stack = [];
        string? pendingKey = null;

        while (parser.MoveNext())
        {
            var kind = parser.Current.Kind;
            switch (kind)
            {
                case TomlParseEventKind.PropertyName:
                    pendingKey = parser.GetPropertyName();
                    break;
                case TomlParseEventKind.StartTable:
                    stack.Push((new TomlTable(), pendingKey));
                    pendingKey = null;
                    break;
                case TomlParseEventKind.StartArray:
                    stack.Push((new List<object?>(), pendingKey));
                    pendingKey = null;
                    break;
                case TomlParseEventKind.EndTable:
                {
                    var (container, key) = stack.Pop();
                    if (stack.Count == 0) root = (TomlTable)container;
                    else Assign(stack.Peek().Container, key, container);
                    break;
                }
                case TomlParseEventKind.EndArray:
                {
                    var (container, key) = stack.Pop();
                    var items = (List<object?>)container;
                    // a group of tables → table array (so re-opened [[name]]
                    // groups aggregate); anything else → value array
                    object array;
                    if (items.Count > 0 && items.All(item => item is TomlTable))
                    {
                        var tables = new TomlTableArray();
                        foreach (var item in items) tables.Add((TomlTable)item!);
                        array = tables;
                    }
                    else
                    {
                        var values = new TomlArray();
                        foreach (var item in items) values.Add(item);
                        array = values;
                    }

                    Assign(stack.Peek().Container, key, array);
                    break;
                }
                case TomlParseEventKind.String:
                    Assign(stack.Peek().Container, TakeKey(ref pendingKey), parser.GetString());
                    break;
                case TomlParseEventKind.Integer:
                    Assign(stack.Peek().Container, TakeKey(ref pendingKey), parser.Current.GetInt64());
                    break;
                case TomlParseEventKind.Float:
                    Assign(stack.Peek().Container, TakeKey(ref pendingKey), parser.Current.GetDouble());
                    break;
                case TomlParseEventKind.Boolean:
                    Assign(stack.Peek().Container, TakeKey(ref pendingKey), parser.Current.GetBoolean());
                    break;
                case TomlParseEventKind.DateTime:
                    Assign(stack.Peek().Container, TakeKey(ref pendingKey), parser.Current.GetTomlDateTime());
                    break;
            }
        }

        if (parser.HasErrors)
            throw new FormatException($"TOML parse failed: {parser.Diagnostics}");
        return root ?? new TomlTable();
    }

    private static string? TakeKey(ref string? pendingKey)
    {
        var key = pendingKey;
        pendingKey = null;
        return key;
    }

    private static void Assign(object parent, string? key, object? value)
    {
        if (parent is List<object?> list)
        {
            list.Add(value);
            return;
        }

        var table = (TomlTable)parent;
        if (value is TomlTableArray added && key is not null
            && table.TryGetValue(key, out var existing) && existing is TomlTableArray target)
        {
            foreach (var item in added) target.Add(item);
            return;
        }

        table[key ?? throw new InvalidOperationException("value with no property name")] = value!;
    }

    public static bool Compare(object aObject, object bObject)
    {
        // This is to account for the behavior of `[TomlSingleOrArray]`
        if (aObject is TomlArray aArray1 && bObject is not TomlArray && aArray1.Count == 1)
            return Compare(aArray1.First(), bObject);
        if (bObject is TomlArray bArray1 && aObject is not TomlArray && bArray1.Count == 1)
            return Compare(aObject, bArray1.First());

        switch (aObject)
        {
            case TomlTable aTable:
            {
                if (bObject is not TomlTable bTable)
                {
                    return false;
                }

                if (aTable.Count != bTable.Count)
                {
                    return false;
                }

                foreach (var key in aTable.Keys)
                {
                    if (!bTable.ContainsKey(key))
                        return false;

                    if (!Compare(aTable[key], bTable[key]))
                    {
                        return false;
                    }
                }

                break;
            }
            case string aString:
            {
                if (bObject is not string bString)
                    return false;

                return aString == bString;
            }
            case TomlArray aArray:
            {
                if (bObject is not TomlArray bArray)
                    return false;

                if (aArray.Count != bArray.Count)
                    return false;

                for (var i = 0; i < aArray.Count; i++)
                {
                    if (!Compare(aArray[i], bArray[i]))
                        return false;
                }

                return true;
            }
            case long aLong:
                if (bObject is not long bLong)
                    return false;

                return aLong == bLong;
            case double aDouble:
                if (bObject is not double bDouble)
                    return false;

                return aDouble == bDouble;
            default:
                return false;
        }

        return true;
    }
}
