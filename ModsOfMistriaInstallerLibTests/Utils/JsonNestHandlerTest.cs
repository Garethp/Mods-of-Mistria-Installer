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

        Assert.That(JsonNestHandler.NestTokens(original, original), new ContainsJsonConstraint(expected));
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

        Assert.That(JsonNestHandler.NestTokens(original, original), new ContainsJsonConstraint(expected));
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

        Assert.That(JsonNestHandler.NestTokens(original, original), new ContainsJsonConstraint(expected));
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
        
        Assert.That(JsonNestHandler.NestTokens(original, original), new MatchesJsonConstraint(expected));
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
        
        Assert.That(JsonNestHandler.NestTokens(original, original), new MatchesJsonConstraint(expected));
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
        
        Assert.That(JsonNestHandler.NestTokens(original, original), new MatchesJsonConstraint(expected));
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
        
        Assert.That(JsonNestHandler.NestTokens(original, original), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldOnlyAddNestedKeysFromReferenceJSON()
    {
        var full = new JObject
        {
            { "a", new JObject { { "b", "1" } } },
            { "c", new JArray { "2" } },
            { "d", new JObject { { "e", "3" } } },
            { "f", new JArray { "4" } }
        };

        var reference = new JObject
        {
            { "d", new JObject { { "e", "3" } } },
            { "f", new JArray { "4" } }
        };

        var expected = new JObject
        {
            { "a", new JObject { { "b", "1" } } },
            { "c", new JArray { "2" } },
            { "d", new JObject { { "e", "3" } } },
            { "f", new JArray { "4" } },
            { "d/e", "3" },
            { "f/0", "4" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, reference), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldOnlyAddDeepNestedKeysFromReferenceJSON()
    {
        var full = new JObject
        {
            { "deep", new JObject {
                { "a", new JObject { { "b", "1" } } },
                { "c", new JArray { "2" } },
                { "d", new JObject { { "e", "3" } } },
                { "f", new JArray { "4" } }
            } }
        };

        var reference = new JObject
        {
            { "deep", new JObject {
                { "d", new JObject { { "e", "3" } } },
                { "f", new JArray { "4" } }
            }}
        };

        var expected = new JObject
        {
            { "deep", new JObject {
                { "a", new JObject { { "b", "1" } } },
                { "c", new JArray { "2" } },
                { "d", new JObject { { "e", "3" } } },
                { "f", new JArray { "4" } }
            } },
            { "deep/d", new JObject { { "e", "3" } } },
            { "deep/d/e", "3" },
            { "deep/f", new JArray { "4" } },
            { "deep/f/0", "4" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, reference), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldCheckReferenceObjectType()
    {
        var full = new JObject
        {
            { "a", "1" }
        };

        var reference = new JObject
        {
            { "a", new JArray() }
        };
        
        Assert.Throws<Exception>(delegate { JsonNestHandler.NestTokens(full, reference); });
    }

    [Test]
    public void ShouldHandleCheckingNestedKeysOfLongArrays()
    {
        var full = new JObject
        {
            { "a", new JArray { "1" } },
            { "a/0", "1" },
            { "a/10", "9" }
        };

        var expected = new JObject
        {
            { "a", new JArray { "1" } },
            { "a/0", "1" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, full), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldHandleCheckingNestedKeysThatStartWithNumbers()
    {
        var full = new JObject
        {
            { "a", new JObject { { "7:00PM", "a" } } },
            { "a/7:00PM", "a" }
        };

        var expected = new JObject
        {
            { "a", new JObject { { "7:00PM", "a" } } },
            { "a/7:00PM", "a" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, full), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldHandleCheckingDoorKeys()
    {
        var full = new JObject
        {
            { "doors", new JObject {
                { "town/town exit", "town exit" }
            } }
        };
        
        var expected = new JObject
        {
            { "doors", new JObject {
                { "town/town exit", "town exit" }
            } },
            { "doors/town/town exit", "town exit" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, full), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldHandleCheckingDoorKeysWithApostrophes()
    {
        var full = new JObject
        {
            { "doors", new JObject {
                { "town/town's exit", "town exit" }
            } }
        };
        
        var expected = new JObject
        {
            { "doors", new JObject {
                { "town/town's exit", "town exit" }
            } },
            { "doors/town/town's exit", "town exit" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, full), new MatchesJsonConstraint(expected));
    }

    [TestCase("items")]
    [TestCase("object_prototypes")]
    [TestCase("fonts")]
    public void ShouldNotNestTopLevelKey(string topLevelKey)
    {
        var full = new JObject
        {
            { topLevelKey, new JObject {
                { "test", "test" }
            } }
        };
        
        var expected = new JObject
        {
            { topLevelKey, new JObject {
                { "test", "test" }
            } }
        };

        var nested = JsonNestHandler.NestTokens(full, full);
        
        Assert.That(nested, new MatchesJsonConstraint(expected));
        Assert.That(
            nested, Is.Not.Matches(new ContainsJsonConstraint(new JObject
            {
                { $"{topLevelKey}/test", "test" }
            }))
        );
    }

    [Test]
    public void ShouldHandleObjectsWithNumberKeys()
    {
        var full = new JObject
        {
            {
                "a", new JObject
                {
                    { "0", "foo" },
                    { "5", "bar" }
                }
            }
        };

        var expected = new JObject
        {
            {
                "a", new JObject
                {
                    { "0", "foo" },
                    { "5", "bar" }
                }
            },
            { "a/0", "foo" },
            { "a/5", "bar" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, full), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldHandleBothArraysAndObjectsWithNumberKeys()
    {
        var full = new JObject
        {
            {
                "a", new JArray
                {
                    new JObject { { "0", "foo" } },
                    new JObject { { "5", "bar" } }
                }
            }
        };

        var expected = new JObject
        {
            {
                "a", new JArray
                {
                    new JObject { { "0", "foo" } },
                    new JObject { { "5", "bar" } }
                }
            },
            { "a/0", new JObject { { "0", "foo" } } },
            { "a/1", new JObject { { "5", "bar" } } },
            { "a/0/0", "foo" },
            { "a/1/5", "bar" }
        };
        
        Assert.That(JsonNestHandler.NestTokens(full, full), new MatchesJsonConstraint(expected));
    }
}