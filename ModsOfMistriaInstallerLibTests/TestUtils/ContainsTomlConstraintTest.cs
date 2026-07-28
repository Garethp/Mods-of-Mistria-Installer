using Newtonsoft.Json.Linq;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

[TestFixture]
public class ContainsTomlConstraintTest
{
    [Test]
    public void ShouldMatchShallowContains()
    {
        var complete = new TomlTable
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new TomlTable
        {
            { "a", 1 }
        };
        
        Assert.That(complete, new ContainsTomlConstraint(partial));
    }

    [Test]
    public void ShouldNotMatchShallowNonContains()
    {
        var complete = new TomlTable
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new TomlTable
        {
            { "c", 3 }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsTomlConstraint(partial)));
    }

    [Test]
    public void ShouldNotMatchDifferentTypes()
    {
        var complete = new TomlTable
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new TomlTable
        {
            { "a", "1" }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsTomlConstraint(partial)));
    }
    
    [Test]
    public void ShouldNotMatchDifferentValues()
    {
        var complete = new TomlTable
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new TomlTable
        {
            { "a", 2 }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsTomlConstraint(partial)));
    }

    [Test]
    public void ShouldMatchArraysContaining()
    {
        var complete = new TomlTable
        {
            {
                "array", new TomlArray { 1, 2, 3 }
            }
        };

        var partial = new TomlTable
        {
            { "array", new TomlArray { 1, 2 } }
        };
        
        Assert.That(complete, new ContainsTomlConstraint(partial));
    }
    
    [Test]
    public void ShouldNotMatchDifferentArrays()
    {
        var complete = new TomlTable
        {
            {
                "array", new TomlArray { 1, 2, 3 }
            }
        };

        var partial = new TomlTable
        {
            { "array", new TomlArray { 1, "2" } }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsTomlConstraint(partial)));
    }
    
    [Test]
    public void ShouldMatchDeepContains()
    {
        var complete = new TomlTable
        {
            {
                "a", new TomlTable
                {
                    { "b", new TomlTable
                    {
                        { "c", 3 },
                        { "d", 4 },
                        { "e", 5 }
                    }}
                }
            }
        };

        var partial = new TomlTable
        {
            {
                "a", new TomlTable
                {
                    { "b", new TomlTable
                    {
                        { "d", 4 }
                    }}
                }
            }
        };
        
        Assert.That(complete, new ContainsTomlConstraint(partial));
    }
}