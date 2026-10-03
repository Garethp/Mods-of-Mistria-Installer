using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Operations;
using Garethp.ModsOfMistriaInstallerLib.Seam;

namespace ModsOfMistriaInstallerLibTests.Operations;

// The framework call sweep against fabricated build trees. A call resolves
// through an engine definition, through the engine's own use of a native, or
// through a framework definition, and everything else reports with its file and
// line. Member calls resolve on their object and stay outside the sweep.
[TestFixture]
public class FrameworkReadsTest
{
    private const string Tree =
        "function engine_helper(value) {\n"
        + "    var names = struct_get_names(value);\n"
        + "    return array_length(names);\n"
        + "}\n";

    private static MemoryPristineSource Pristine(string text) =>
        new(new Dictionary<string, byte[]>
        {
            ["assets/gml/scripts/Engine.gml"] = Encoding.UTF8.GetBytes(text),
        });

    private static IReadOnlyList<FrameworkReadProblem> Sweep(string payload) =>
        FrameworkReads.Unresolved(Pristine(Tree), [("mmapi_test.gml", payload)]);

    [Test]
    public void ShouldResolveThroughDefinitionsNativesAndOwnFunctions()
    {
        // engine_helper is defined by the tree, struct_get_names is proven a
        // native by the tree's own call, and own_helper is defined by the
        // framework in both declaration and assignment form
        var payload = "function mmapi_a() {\n"
                      + "    engine_helper(1);\n"
                      + "    struct_get_names({});\n"
                      + "    own_helper();\n"
                      + "    assigned_helper();\n"
                      + "}\n"
                      + "function own_helper() {\n}\n"
                      + "assigned_helper = function() {\n}\n";

        Assert.That(Sweep(payload), Is.Empty);
    }

    [Test]
    public void ShouldReportAnUnresolvedCallOncePerName()
    {
        var payload = "function mmapi_a() {\n"
                      + "    gone_native(1);\n"
                      + "    gone_native(2);\n"
                      + "}\n";

        var problems = Sweep(payload);

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0].Name, Is.EqualTo("gone_native"));
        Assert.That(problems[0].File, Is.EqualTo("mmapi_test.gml"));
        Assert.That(problems[0].Line, Is.EqualTo(2));
    }

    [Test]
    public void ShouldSkipMemberCallsAndKeywords()
    {
        // thing.member_fn resolves on the object, and statement keywords
        // prove nothing either way
        var payload = "function mmapi_a(thing) {\n"
                      + "    if (thing != undefined) {\n"
                      + "        return thing.member_fn(1);\n"
                      + "    }\n"
                      + "}\n";

        Assert.That(Sweep(payload), Is.Empty);
    }

    [Test]
    public void ShouldExemptAMarkedSiteButNotTheSameNameElsewhere()
    {
        // the exemption is per site, so a capability probe opts out where it
        // happens and an unmarked call to the same name still reports
        var payload = "function mmapi_a() {\n"
                      + "    try { maybe_native(); } catch (err) {}  // framework-reads-exempt: probed\n"
                      + "    maybe_native();\n"
                      + "}\n";

        var problems = Sweep(payload);

        Assert.That(problems, Has.Count.EqualTo(1));
        Assert.That(problems[0].Line, Is.EqualTo(3));
    }

    [Test]
    public void ShouldSweepNothingForAnEmptyPayload()
    {
        Assert.That(FrameworkReads.Unresolved(Pristine(Tree), []), Is.Empty);
    }
}
