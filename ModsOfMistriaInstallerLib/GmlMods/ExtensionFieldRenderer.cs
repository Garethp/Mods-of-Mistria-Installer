using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace Garethp.ModsOfMistriaInstallerLib.GmlMods;

// Renders a registration's TOML value into the GML the expander splices.
// This is the security boundary. `identifier` admits a single identifier only and
// strings are escaped, so a hostile registration cannot inject code.
public static class ExtensionFieldRenderer
{
    private static readonly Regex IdentifierRegex = new(@"\A[A-Za-z_][A-Za-z0-9_]*\z");

    // The GML spelling of `raw` for this field's type, or null with `problem`
    // set. Null is always a mod-content failure, never a crash.
    public static string? Render(ExtensionField field, object? raw, out string problem)
    {
        problem = "";
        if (raw is null)
        {
            problem = $"field '{field.Name}' is missing";
            return null;
        }

        switch (field.Type)
        {
            case ExtensionFieldType.Identifier:
            {
                if (raw is not string text)
                {
                    problem = $"field '{field.Name}' must be a string naming a GML identifier";
                    return null;
                }

                var trimmed = text.Trim();
                if (!IdentifierRegex.IsMatch(trimmed))
                {
                    problem = $"field '{field.Name}' value '{text}' is not a GML identifier "
                              + "(letters, digits and underscore, not starting with a digit) - "
                              + "this value is spliced into engine code as a bare token, so its "
                              + "shape is a correctness constraint, not style";
                    return null;
                }

                return trimmed;
            }

            case ExtensionFieldType.String:
            {
                if (raw is not string text)
                {
                    problem = $"field '{field.Name}' must be a string";
                    return null;
                }

                return Quote(text);
            }

            case ExtensionFieldType.Int:
            {
                // TOML gives a long for an integer. Anything else (including a
                // float or a numeric-looking string) is a type mismatch, not
                // something to coerce
                if (raw is not long value)
                {
                    problem = $"field '{field.Name}' must be an integer";
                    return null;
                }

                return value.ToString(CultureInfo.InvariantCulture);
            }

            case ExtensionFieldType.Bool:
            {
                if (raw is not bool value)
                {
                    problem = $"field '{field.Name}' must be true or false";
                    return null;
                }

                return value ? "true" : "false";
            }

            default:
                problem = $"field '{field.Name}' has an unsupported type";
                return null;
        }
    }

    // A GML string literal. The backslash goes first, or every escape this
    // adds would itself be re-escaped. Control characters are spelled as
    // escapes rather than passed through, so a value carrying a newline can
    // never break out of its line and become code.
    private static string Quote(string text)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in text)
        {
            switch (c)
            {
                case '\\': quoted.Append("\\\\"); break;
                case '"': quoted.Append("\\\""); break;
                case '\n': quoted.Append("\\n"); break;
                case '\r': quoted.Append("\\r"); break;
                case '\t': quoted.Append("\\t"); break;
                default:
                    if (char.IsControl(c)) quoted.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:X4}");
                    else quoted.Append(c);
                    break;
            }
        }

        return quoted.Append('"').ToString();
    }
}
