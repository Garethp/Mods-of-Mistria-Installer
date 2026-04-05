using Garethp.ModsOfMistriaInstallerLib.Utils;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Utils;

[TestFixture]
public class JsonNestHandlerTest
{
    [Test]
    public void ShouldAutomaticallyNestObjects()
    {
        var original = new JObject
        {
            {
                "a", new JObject
                {
                    { "b", "foo" }
                }
            }
        };
        
        var expected = new JObject
        {
            { "a/b", "foo" }
        };

        Assert.That(JsonNestHandler.NestTokens(original), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAutomaticallyNestObjectsAtMultipleLevels()
    {
        var original = new JObject
        {
            {
                "a", new JObject
                {
                    {
                        "b", new JObject
                        {
                            { "c", "foo" }
                        }
                    }
                }
            }
        };
        
        var expected = new JObject
        {
            {
                "a/b", new JObject
                {
                    { "c", "foo" }
                }
            },
            { "a/b/c", "foo" }
        };

        Assert.That(JsonNestHandler.NestTokens(original), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAutomaticallyNestArrays()
    {
        var original = new JObject
        {
            {
                "a", new JArray(new List<JToken>
                {
                    new JValue("b"),
                    new JObject
                    {
                        {
                            "c", new JObject
                            {
                                { "foo", "bar" }
                            }
                        }
                    }
                })
            }
        };
        
        var expected = new JObject
        {
            { "a/0", "b" },
            {
                "a/1", new JObject
                {
                    {
                        "c", new JObject
                        {
                            { "foo", "bar" }
                        }
                    }
                }
            },
            {
                "a/1/c", new JObject
                {
                    { "foo", "bar" }
                }
            },
            { "a/1/c/foo", "bar" }
        };

        Assert.That(JsonNestHandler.NestTokens(original), new ContainsJsonConstraint(expected));
    }
}