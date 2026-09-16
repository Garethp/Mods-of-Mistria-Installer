namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One abstraction over every pristine read: the backup archive, an arbitrary
// build zip for the seam check, and the in-memory trees the tests use. Entries are
// "assets/"-prefixed, matching seam-catalog `file` values, so an entry path
// round-trips 1:1.
public interface IPristineSource
{
    bool Has(string entry);

    // Matching archive-relative paths, sorted.
    IReadOnlyList<string> Entries(string prefix, string suffix);

    // Null when the entry is not in the pristine source: a missing entry is a
    // batched staging problem (a stale catalog against a new build), not an
    // exception at the read site.
    byte[]? Read(string entry);

    // Every assets/gml/**/*.gml entry, ordinal-sorted. The call-rewrite pass
    // considers the whole engine tree, not just the files anchored entries name.
    IReadOnlyList<string> GmlFiles();

    // Every asset identifier this source can enumerate, meaning sprite, room,
    // and object names. Null when the source carries no asset inventory, so the
    // asset checks stand down rather than guess.
    IReadOnlySet<string>? AssetNames();
}
