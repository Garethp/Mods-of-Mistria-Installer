using Garethp.ModsOfMistriaInstallerLib.Seam;
using Garethp.ModsOfMistriaInstallerLib.Store;

namespace ModsOfMistriaInstallerLibTests.Store;

// The ledger is append-only and fail-closed. Losing or silently resetting it
// reassigns ordinals, and a symbol that stops resolving crashes a save load
// that names it, so every failure here is loud.
[TestFixture]
public class ExtensionLedgerStoreTest
{
    private string _root = "";

    [SetUp]
    public void CreateTempDir()
    {
        _root = Path.Combine(Path.GetTempPath(), "momi_ledger_" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void RemoveTempDir()
    {
        Directory.Delete(_root, true);
    }

    private string LedgerPath => Path.Combine(_root, ExtensionLedgerStore.FileName);

    [Test]
    public void ShouldLoadEmptyWhenNoLedgerExists()
    {
        var ledger = ExtensionLedgerStore.Load(_root);

        Assert.That(ledger.Assignments("roster"), Is.Empty);
        Assert.That(ledger.Dirty, Is.False);
    }

    [Test]
    public void ShouldRoundTripAssignments()
    {
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Assign("roster", new ExtensionAssignment("mod_wren", 35, "author.mod"));
        Assert.That(ledger.Dirty, Is.True);
        ledger.Save();

        var reloaded = ExtensionLedgerStore.Load(_root);

        Assert.That(reloaded.Assignments("roster").Select(a => (a.Symbol, a.Ordinal, a.ModId)),
            Is.EqualTo(new[]
            {
                ("mod_luna", 34, "author.mod"),
                ("mod_wren", 35, "author.mod"),
            }));
        Assert.That(reloaded.Dirty, Is.False);
    }

    [Test]
    public void ShouldNotBeDirtyUntilSomethingIsAssigned()
    {
        // the install writes only when something changed
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Save();

        var reloaded = ExtensionLedgerStore.Load(_root);
        Assert.That(reloaded.Dirty, Is.False);
        Assert.That(reloaded.Assignments("roster"), Is.Empty);
    }

    [Test]
    public void ShouldKeepAnEntryWhoseModIsGone()
    {
        // the tombstone. This is what keeps the enum member alive for a save
        // that still names the symbol
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Save();

        var reloaded = ExtensionLedgerStore.Load(_root);

        Assert.That(reloaded.Assignments("roster").Single().Symbol, Is.EqualTo("mod_luna"));
    }

    [Test]
    public void ShouldWriteAStableFileSortedByOrdinal()
    {
        // a ledger that reshuffles its own lines makes a real reassignment
        // impossible to spot by eye
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_wren", 35, "author.mod"));
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Save();
        var first = File.ReadAllText(LedgerPath);

        ExtensionLedgerStore.Load(_root).Save();

        Assert.That(File.ReadAllText(LedgerPath), Is.EqualTo(first));
        Assert.That(first.IndexOf("mod_luna", StringComparison.Ordinal),
            Is.LessThan(first.IndexOf("mod_wren", StringComparison.Ordinal)));
    }

    [Test]
    public void ShouldFailLoudlyOnACorruptLedger()
    {
        File.WriteAllText(LedgerPath, "{ not json");

        var exception = Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));

        Assert.That(exception!.Message, Does.Contain("corrupt"));
        // restoring stays the better option, but the message must also say
        // deletion is recoverable, because delete-and-reseed is a validated
        // recovery and the old text steered users away from it
        Assert.That(exception.Message, Does.Contain("Restore it from a backup"));
        Assert.That(exception.Message, Does.Contain("deleting the file is recoverable"));
    }

    [Test]
    public void ShouldFailLoudlyOnAnEntryMissingItsOrdinal()
    {
        File.WriteAllText(LedgerPath,
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"mod_luna"}]}}}""");

        var exception = Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));

        Assert.That(exception!.Message, Does.Contain("no symbol or no usable ordinal"));
    }

    [Test]
    public void ShouldRefuseAFutureVersion()
    {
        File.WriteAllText(LedgerPath, """{"version":2,"points":{}}""");

        var exception = Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));

        Assert.That(exception!.Message, Does.Contain("a newer MOMI wrote it"));
    }

    [Test]
    public void ShouldTreatAMissingVersionAsCorruptRatherThanNewer()
    {
        // version 0 means the field is absent, and blaming "a newer MOMI" would
        // send the user hunting for an installer that does not exist
        File.WriteAllText(LedgerPath, """{"points":{}}""");

        var exception = Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));

        Assert.That(exception!.Message, Does.Contain("corrupt"));
        Assert.That(exception.Message, Does.Not.Contain("a newer MOMI wrote it"));
    }

    [Test]
    public void ShouldWrapMalformedFieldTypesInTheCorruptMessage()
    {
        // a raw InvalidCastException or FormatException carries none of the
        // recovery guidance, so every malformed shape must land in the same
        // corrupt-ledger message
        var cases = new[]
        {
            """{"version":1,"points":[]}""",
            """{"version":1,"points":{"roster":"nope"}}""",
            """{"version":1,"points":{"roster":{"assigned":"nope"}}}""",
            """{"version":1,"points":{"roster":{"assigned":["nope"]}}}""",
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"mod_luna","ordinal":"teal"}]}}}""",
        };

        foreach (var content in cases)
        {
            File.WriteAllText(LedgerPath, content);
            var exception = Assert.Throws<InvalidOperationException>(
                () => ExtensionLedgerStore.Load(_root), content);
            Assert.That(exception!.Message, Does.Contain("corrupt"), content);
        }
    }

    [Test]
    public void ShouldRejectALedgerSymbolOutsideTheSharedShape()
    {
        // the harvesters treat the symbol shape as a security boundary, and
        // the ledger file is the third entry point into generated GML, so it
        // holds the same line
        File.WriteAllText(LedgerPath,
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"Bad Symbol\"","ordinal":34}]}}}""");

        var exception = Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));

        Assert.That(exception!.Message, Does.Contain("outside the symbol alphabet"));
    }

    [Test]
    public void ShouldRejectANegativeOrdinal()
    {
        File.WriteAllText(LedgerPath,
            """{"version":1,"points":{"roster":{"assigned":[{"symbol":"mod_luna","ordinal":-3}]}}}""");

        Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));
    }

    [Test]
    public void ShouldSaveThroughATempFileAndLeaveNoResidue()
    {
        // write-then-rename, so a crash mid-write may truncate only the temp
        // file, never the ledger, and a clean save leaves no temp behind
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Save();

        Assert.That(File.Exists(LedgerPath), Is.True);
        Assert.That(File.Exists(LedgerPath + ".tmp"), Is.False);
        Assert.That(ExtensionLedgerStore.Load(_root).Assignments("roster"), Has.Count.EqualTo(1));
    }

    [Test]
    public void ShouldNotDirtyOnANoOpRebase()
    {
        // every install runs the rebase. One that moves nothing must not
        // rewrite the file, or the unprotected write runs on every install
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Save();
        Assert.That(ledger.Dirty, Is.False);

        ledger.Rebase("roster", [new ExtensionAssignment("mod_luna", 34, "author.mod")]);

        Assert.That(ledger.Dirty, Is.False);
    }

    [Test]
    public void ShouldDirtyOnARebaseThatMoves()
    {
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Save();

        ledger.Rebase("roster", [new ExtensionAssignment("mod_luna", 35, "author.mod")]);

        Assert.That(ledger.Dirty, Is.True);
        Assert.That(ledger.Assignments("roster").Single().Ordinal, Is.EqualTo(35));
    }

    [Test]
    public void ShouldReattributeARecoveredSymbolToItsReturningMod()
    {
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "recovered"));
        ledger.Save();
        Assert.That(ledger.Dirty, Is.False);

        ledger.Reattribute("roster", "mod_luna", "author.mod");

        Assert.That(ledger.Dirty, Is.True);
        Assert.That(ledger.Assignments("roster").Single().ModId, Is.EqualTo("author.mod"));
    }

    [Test]
    public void ShouldNotDirtyOnAReattributionThatChangesNothing()
    {
        var ledger = ExtensionLedgerStore.Load(_root);
        ledger.Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        ledger.Save();

        ledger.Reattribute("roster", "mod_luna", "author.mod");
        ledger.Reattribute("roster", "mod_absent", "author.mod");
        ledger.Reattribute("other_point", "mod_luna", "author.mod");

        Assert.That(ledger.Dirty, Is.False);
    }

    [Test]
    public void ShouldNotWriteAnythingWhenLoadFails()
    {
        File.WriteAllText(LedgerPath, "{ not json");

        Assert.Throws<InvalidOperationException>(() => ExtensionLedgerStore.Load(_root));

        Assert.That(File.ReadAllText(LedgerPath), Is.EqualTo("{ not json"));
    }

    [Test]
    public void ShouldDriveTheExpanderAsAnIExtensionLedger()
    {
        var ledger = ExtensionLedgerStore.Load(_root);

        Assert.That(ledger, Is.InstanceOf<IExtensionLedger>());
        ((IExtensionLedger)ledger).Assign("roster", new ExtensionAssignment("mod_luna", 34, "author.mod"));
        Assert.That(((IExtensionLedger)ledger).Assignments("roster"), Has.Count.EqualTo(1));
    }
}
