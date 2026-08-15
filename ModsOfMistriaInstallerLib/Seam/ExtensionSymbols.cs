using System.Text;
using System.Text.RegularExpressions;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// The one definition of the symbol shape, shared by the collector, the
// ledger, and the reseed harvesters. A security boundary, since symbols land in
// {{symbol}} templates as generated GML, so nothing outside the shape may
// install or recover.
public static class ExtensionSymbols
{
    // Kept assembled-regex-safe, because the archive marker pattern embeds this core
    // between its own anchors.
    public const string ShapeCore = "[a-z][a-z0-9_]{0,80}";

    public static readonly Regex Shape = new($@"\A{ShapeCore}\z", RegexOptions.Compiled);

    // The engine's member-to-string convention observed in save data and
    // world-fact keys. It is lowercase, with an underscore before any interior
    // uppercase run (single-word members simply lowercase). Extension
    // symbols are already lowercase and round-trip unchanged.
    public static string ToNativeName(string member)
    {
        var builder = new StringBuilder(member.Length + 4);
        for (var i = 0; i < member.Length; i++)
        {
            var c = member[i];
            if (char.IsUpper(c) && i > 0 && !char.IsUpper(member[i - 1]))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}
