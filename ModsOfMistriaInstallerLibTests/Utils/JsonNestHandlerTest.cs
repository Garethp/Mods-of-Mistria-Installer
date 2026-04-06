using Garethp.ModsOfMistriaInstallerLib.Utils;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Utils;

[TestFixture]
public class JsonNestHandlerTest
{
    [Test]
    public void ShouldAutomaticallyNestObjects()
    {
        var original = new JObject {
            { "a", new JObject {
                { "b", "foo" }
            } }
        };
        
        var expected = new JObject
        {
            { "a/b", "foo" }
        };

        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAutomaticallyNestObjectsAtMultipleLevels()
    {
        var original = new JObject
        {
            { "a", new JObject {
                { "b", 
                    new JObject { { "c", "foo" } }
                }
            } }
        };
        
        var expected = new JObject
        {
            { "a/b", new JObject {
                { "c", "foo" }
            } },
            { "a/b/c", "foo" }
        };

        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAutomaticallyNestArrays()
    {
        var original = new JObject
        {
            { "a", new JArray(new List<JToken> {
                new JValue("b"),
                new JObject {
                    { "c", new JObject {
                        { "foo", "bar" }
                    } }
                }
            })}
        };
        
        var expected = new JObject
        {
            { "a/0", "b" },
            { "a/1", new JObject {
                { "c", new JObject {
                    { "foo", "bar" }
                } }
            } },
            { "a/1/c", new JObject {
                { "foo", "bar" }
            } },
            { "a/1/c/foo", "bar" }
        };

        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldRemoveInvalidObjectNests()
    {
        var original = new JObject
        {
            { "a", new JObject {
                { "b", "1" }
            }},
            { "a/b", "1" },
            { "a/c", "2" }
        };
        
        var expected = new JObject
        {
            { "a", new JObject {
                { "b", "1" }
            }},
            { "a/b", "1" },
        };
        
        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldRemoveInvalidDeepObjectNests()
    {
        var original = new JObject
        {
            { "a", new JObject {
                { "b", new JObject {
                    { "c", "1" }
                } }
            }},
            { "a/b", new JObject { { "c", "1" } } },
            { "a/b/c", "1" },
            { "a/b/d", "2" }
        };
        
        var expected = new JObject
        {
            { "a", new JObject {
                { "b", new JObject {
                    { "c", "1" }
                } }
            }},
            { "a/b", new JObject { { "c", "1" } } },
            { "a/b/c", "1" },
        };
        
        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldRemoveInvalidArrayNests()
    {
        var original = new JObject
        {
            { "a", new JArray { "1", "2" } },
            { "a/0", "1" },
            { "a/1", "2" },
            { "a/2", "3" }
        };
        
        var expected = new JObject
        {
            { "a", new JArray { "1", "2" } },
            { "a/0", "1" },
            { "a/1", "2" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldRemoveInvalidDeepArrayNests()
    {
        var original = new JObject
        {
            { "a", new JArray
            {
                new JObject { { "b", "1" }}
            } },
            { "a/0", new JObject
            {
                { "b", "1" }
            } },
            { "a/0/b", "1" },
            { "a/0/b/c", "2" }
        };
        
        var expected = new JObject
        {
            { "a", new JArray
            {
                new JObject { { "b", "1" }}
            } },
            { "a/0", new JObject
            {
                { "b", "1" }
            } },
            { "a/0/b", "1" },
        };
        
        Assert.That(JsonNestHandler.NestTokens(original, new JObject()), new MatchesJsonConstraint(expected));
    }
}