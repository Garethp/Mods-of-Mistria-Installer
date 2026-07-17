namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One lexical token: (Start, End) char offsets into the source string. Token
// text is read as a span on demand, so tokenizing allocates nothing per token.
public readonly record struct GmlToken(int Start, int End);
