using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One seamed engine file, simulated in memory before anything is written.
public class StagedFile(string text, string eol)
{
    // \n-normalised seamed content
    public string Text { get; internal set; } = text;

    // the pristine file's line endings: "\n" or "\r\n"
    public string Eol { get; } = eol;

    internal List<string> AppliedIds { get; } = [];

    // entries applied, in order
    public IReadOnlyList<string> EntryIds => AppliedIds;

    // UTF-8, no BOM, \n → Eol
    public byte[] Encode() =>
        Encoding.UTF8.GetBytes(Eol == "\n" ? Text : Text.Replace("\n", Eol));
}
