namespace Garethp.ModsOfMistriaInstallerLib.Seam;

// One abstraction over every pristine read: the backup archive, an arbitrary
// build zip for verify, and the in-memory trees the tests use. Entries are
// "assets/"-prefixed, matching seam-catalog `file` values, so an entry path
// round-trips 1:1.
public interface IPristineSource
{
    bool Has(string entry);

    // Null when the entry is not in the pristine source: a missing entry is a
    // batched staging problem (a stale catalog against a new build), not an
    // exception at the read site.
    byte[]? Read(string entry);

    // Every assets/gml/**/*.gml entry, ordinal-sorted. The call-rewrite pass
    // considers the whole engine tree, not just the files anchored entries name.
    IReadOnlyList<string> GmlFiles();
}
