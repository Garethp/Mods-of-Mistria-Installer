namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One assignment whose target path starts at "global". A deeper write
// (global.name.field = x) reports as non-bare: it mutates the root's contents
// without replacing it.
public record GlobalWrite(
    string Name,  // the root identifier written under global
    int Start,    // char offset of the "global" token
    bool Bare);   // the target is exactly global.NAME, no deeper path
