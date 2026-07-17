using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Mmapi;

// mmapi's hook engine, tested by running it. The whole framework (all 8 .gml
// files, one namespace, the way the boot's global-script compile sees them) is
// concatenated with a prelude of assertion helpers and one test body, then
// executed on the pinned fabricator VM.
//
// The floor, not the authority: the shipped game runs its own engine build, so
// these prove the dispatch logic on the pinned VM and no more.
[TestFixture]
public class DispatchRuntimeTest
{
    private static void AssertBodyPasses(string name)
    {
        var result = GmlProbe.RunWithFramework(
        [
            GmlProbe.Fixture("gml_prelude.gml"),
            GmlProbe.Fixture(Path.Combine("dispatch_runtime", name)),
        ]);

        GmlProbe.AssertAllPass(result, name);
    }

    [Test]
    public void ShouldDispatchEventsToEveryHandler()
    {
        // Every event handler runs, in order, once per emit; the return value
        // is discarded; duplicates from one mod do not compound.
        AssertBodyPasses("dispatch_event.gml");
    }

    [Test]
    public void ShouldChainFilterValues()
    {
        // The value chains; undefined declines and keeps the current value;
        // false and 0 are real values that thread through.
        AssertBodyPasses("dispatch_filter.gml");
    }

    [Test]
    public void ShouldVetoOnlyOnFalseGuards()
    {
        // Only false vetoes, and it short-circuits; a guard that says nothing
        // allows. If this fails, mods silently stop players doing things.
        AssertBodyPasses("dispatch_guard.gml");
    }

    [Test]
    public void ShouldStopAtTheFirstOverrideAnswer()
    {
        // The first non-undefined answer wins and stops the chain; false and 0
        // are answers, not passes.
        AssertBodyPasses("dispatch_override.gml");
    }

    [Test]
    public void ShouldIsolateAThrowingHandler()
    {
        // The framework's central promise: a throwing handler is contained per
        // kind, the chain continues, the error is charged to the mod that threw,
        // and an unguarded seam survives it.
        AssertBodyPasses("dispatch_isolation.gml");
    }

    [Test]
    public void ShouldReportHandlerFailuresRateLimited()
    {
        // The other half of isolation: the failure is counted exactly and
        // logged once per 60, so a handler throwing every frame stays visible
        // without drowning the log. Isolation that loses the error is silence.
        AssertBodyPasses("dispatch_error_reporting.gml");
    }

    [Test]
    public void ShouldIsolateModCallsOutsideTheDispatchers()
    {
        // The two places outside the four dispatchers where mmapi calls into
        // mod code: mmapi_run_installs and mmapi_hotkeys_poll. A throwing
        // installer must not stop the mods behind it from ever registering.
        AssertBodyPasses("mod_call_isolation.gml");
    }

    [Test]
    public void ShouldOrderHandlersDeterministically()
    {
        // Priority (lower first), stable by registration at ties, before/after
        // edges on top, and a contradiction falls back rather than hanging.
        AssertBodyPasses("dispatch_ordering.gml");
    }
}
