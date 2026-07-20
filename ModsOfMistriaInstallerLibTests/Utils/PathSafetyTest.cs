using Garethp.ModsOfMistriaInstallerLib.Utils;

namespace ModsOfMistriaInstallerLibTests.Utils;

[TestFixture]
public class PathSafetyTest
{
    [Test]
    public void ShouldAcceptAPathUnderAssets()
    {
        Assert.That(PathSafety.PathProblem("assets/gml/objects/Game.gml", "test `file`"), Is.Null);
    }

    [Test]
    public void ShouldRejectATraversalSegment()
    {
        Assert.That(PathSafety.PathProblem("assets/../evil.gml", "test `file`"), Does.Contain("'..'"));
        Assert.That(PathSafety.PathProblem("assets/./x.gml", "test `file`"), Does.Contain("'.'"));
    }

    [Test]
    public void ShouldRejectADriveLetter()
    {
        Assert.That(PathSafety.PathProblem("C:/evil.gml", "test `file`"), Does.Contain("colon"));
    }

    [Test]
    public void ShouldRejectAnAlternateDataStreamColon()
    {
        Assert.That(PathSafety.PathProblem("assets/gml/A.gml:stream", "test `file`"), Does.Contain("colon"));
    }

    [Test]
    public void ShouldRejectABackslash()
    {
        Assert.That(PathSafety.PathProblem(@"assets\gml\A.gml", "test `file`"), Does.Contain("backslash"));
    }

    [Test]
    public void ShouldRejectAnAbsolutePath()
    {
        Assert.That(PathSafety.PathProblem("/assets/gml/A.gml", "test `file`"), Does.Contain("absolute"));
    }

    [Test]
    public void ShouldRejectAnEmptyPath()
    {
        Assert.That(PathSafety.PathProblem("", "test `file`"), Does.Contain("empty"));
        Assert.That(PathSafety.PathProblem("   ", "test `file`"), Does.Contain("empty"));
    }

    [Test]
    public void ShouldRejectAPathOutsideAssets()
    {
        Assert.That(PathSafety.PathProblem("gml/A.gml", "test `file`"), Does.Contain("not under assets/"));
    }

    [Test]
    public void ShouldNameTheSourceInTheProblem()
    {
        Assert.That(PathSafety.PathProblem("gml/A.gml", "manifest `added`"), Does.StartWith("manifest `added`:"));
    }
}
