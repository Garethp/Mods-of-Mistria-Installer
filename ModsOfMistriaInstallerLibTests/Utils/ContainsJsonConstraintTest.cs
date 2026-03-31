using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Utils;

[TestFixture]
public class ContainsJsonConstraintTest
{
    [Test]
    public void ShouldMatchShallowContains()
    {
        var complete = new JObject
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new JObject
        {
            { "a", 1 }
        };
        
        Assert.That(complete, new ContainsJsonConstraint(partial));
    }

    [Test]
    public void ShouldNotMatchShallowNonContains()
    {
        var complete = new JObject
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new JObject
        {
            { "c", 3 }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsJsonConstraint(partial)));
    }

    [Test]
    public void ShouldNotMatchDifferentTypes()
    {
        var complete = new JObject
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new JObject
        {
            { "a", "1" }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsJsonConstraint(partial)));
    }
    
    [Test]
    public void ShouldNotMatchDifferentValues()
    {
        var complete = new JObject
        {
            { "a", 1 },
            { "b", 2 }
        };

        var partial = new JObject
        {
            { "a", 2 }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsJsonConstraint(partial)));
    }

    [Test]
    public void ShouldMatchArraysContaining()
    {
        var complete = new JObject
        {
            {
                "array", new JArray { 1, 2, 3 }
            }
        };

        var partial = new JObject
        {
            { "array", new JArray { 1, 2 } }
        };
        
        Assert.That(complete, new ContainsJsonConstraint(partial));
    }
    
    [Test]
    public void ShouldNotMatchDifferentArrays()
    {
        var complete = new JObject
        {
            {
                "array", new JArray { 1, 2, 3 }
            }
        };

        var partial = new JObject
        {
            { "array", new JArray { 1, "2" } }
        };
        
        Assert.That(complete, Is.Not.Matches(new ContainsJsonConstraint(partial)));
    }
    
    [Test]
    public void ShouldMatchDeepContains()
    {
        var complete = new JObject
        {
            {
                "a", new JObject
                {
                    { "b", new JObject
                    {
                        { "c", 3 },
                        { "d", 4 },
                        { "e", 5 }
                    }}
                }
            }
        };

        var partial = new JObject
        {
            {
                "a", new JObject
                {
                    { "b", new JObject
                    {
                        { "d", 4 }
                    }}
                }
            }
        };
        
        Assert.That(complete, new ContainsJsonConstraint(partial));
    }
}