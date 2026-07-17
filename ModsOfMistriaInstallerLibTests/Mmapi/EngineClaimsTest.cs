using ModsOfMistriaInstallerLibTests.TestUtils;

namespace ModsOfMistriaInstallerLibTests.Mmapi;

// The engine-behaviour claims the mmapi framework is built on, checked rather
// than remembered. The carried mmapi.gml documents two VM quirks that shaped
// its architecture:
//
//   Quirk 1  a try/catch inside a function called with arguments cannot catch
//            exceptions from its callees. This was the entire justification for
//            the __mmapi_guarded_call trampoline.
//   Quirk 2  a value returned through a call frame can compare equal to both
//            undefined and false. mmapi_check_guards tests `== undefined`
//            before `== false` on the strength of it.
//
// Both were observed once, in prose, and trusted forever; neither had a test,
// so nobody would notice if an engine update fixed them. These probes run the
// claims against the VM at the rev the checker pins. They are the floor, not
// the authority: the shipped game runs its own engine build.
//
// A failure here is a real finding about the pinned VM, not a broken test: the
// quirk came back, and the workaround it justified is load-bearing again.
[TestFixture]
public class EngineClaimsTest
{
    // The probes run bare, with no framework: they test the VM, not mmapi.
    private static void AssertProbePasses(string name)
    {
        var probe = GmlProbe.Fixture(Path.Combine("engine_claims", name));

        GmlProbe.AssertAllPass(GmlProbe.RunGml(probe), name);
    }

    [Test]
    public void ShouldNotReproduceTheGuardedCallQuirk()
    {
        // Quirk 1 does not reproduce: a try/catch in a function called with
        // arguments catches its callees' exceptions, in all 10 shapes including
        // mmapi_emit's own structure without the trampoline. If this fails, the
        // quirk is back and __mmapi_guarded_call is load-bearing again.
        AssertProbePasses("quirk1_guarded_call.gml");
    }

    [Test]
    public void ShouldNotCompareUndefinedEqualToFalse()
    {
        // Quirk 2 does not reproduce: an undefined returned through a call
        // frame compares equal to undefined and not to false, so
        // mmapi_check_guards' ordering is defensive rather than load-bearing.
        AssertProbePasses("quirk2_undefined_false.gml");
    }
}
