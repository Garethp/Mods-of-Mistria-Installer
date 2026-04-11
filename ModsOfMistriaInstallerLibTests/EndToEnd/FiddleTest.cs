using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class FiddleTest
{
    private MockInstaller _installer = new MockInstaller(
        [new FiddleGenerator()],
        [new FiddleInstaller()]
    );

    private MockFileModifier _fileModifier;

    [SetUp]
    public void Setup()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            { "__fiddle__.json", new JObject().ToString() }
        });
    }

    [Test]
    public void ShouldEnsureExtraObjectsAndItems()
    {
        var mod = new MockMod(new Dictionary<string, string>()
        {
            { "fiddle/fiddle.json", new JObject().ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            {
                "extras", new JObject
                {
                    { "objects", new JArray() },
                    { "items", new JArray() }
                }
            },
            { "extras/items", new JArray() },
            { "extras/objects", new JArray() },
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldMergeItems()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject
                {
                    {
                        "a", new JObject
                        {
                            { "b", "2" }
                        }
                    }
                }.ToString()
            }
        });
        
        var mod = new MockMod(new Dictionary<string, string>()
        {
            { "fiddle/test1.json", new JObject
            {
                { "a", new JObject
                {
                    { "c", "3" }
                }}
            }.ToString() },
            { "fiddle/test2.json", new JObject
            {
                { "d", "4" }
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "a", new JObject {
                { "b", "2" },
                { "c", "3" }
            }},
            { "d", "4" }
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAllowUsingNullsToRemoveKeys()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject
                {
                    { "a", new JObject {
                        { "b", "1" },
                        { "c", "2"}
                    } }
                }.ToString()
            }
        });
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/test1.json", new JObject
            {
                { "a", new JObject
                {
                    { "b", null }
                }}
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(new JObject
        {
            { "a", new JObject {
                { "b", null },
                { "c", "2" }
            }}
        }));
    }
    
    [Test]
    public void ShouldAllowUsingNullsToEmptyArrays()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject
                {
                    { "a", new JArray { "1", "2", "3" } },
                    // { "a/0", "1" },
                    // { "a/1", "2" },
                    // { "a/2", "3" }
                }.ToString()
            }
        });
    
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/test1.json", new JObject { { "a", null } }.ToString() },
            { "fiddle/test2.json", new JObject
            {
                { "a", new JArray { "4", "5" }}
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new MatchesJsonConstraint(new JObject
        {
            { "a", new JArray { "4", "5" }},
            {
                "extras", new JObject
                {
                    { "objects", new JArray() },
                    { "items", new JArray() }
                }
            },
            // { "a/0", "4" },
            // { "a/1", "5" },
            { "extras/items", new JArray() },
            { "extras/objects", new JArray() },
        }));
    }

    [Ignore("Temporarily disabling nesting")]
    [Test]
    public void ShouldAutomaticallyNestObjects()
    {
        var mod = new MockMod(new Dictionary<string, string>()
        {
            {
                "fiddle/fiddle.json", new JObject
                {
                    {
                        "a", new JObject
                        {
                            { "b", "foo" }
                        }
                    }
                }.ToString()
            }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "a/b", "foo" }
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Ignore("Temporarily disabling nesting")]
    [Test]
    public void ShouldAutomaticallyNestObjectsAtMultipleLevels()
    {
        var mod = new MockMod(new Dictionary<string, string>()
        {
            {
                "fiddle/fiddle.json", new JObject
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
                }.ToString()
            }
        });

        _installer.InstallMods([mod], _fileModifier);

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

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Ignore("Temporarily disabling nesting")]
    [Test]
    public void ShouldAutomaticallyNestArrays()
    {
        var mod = new MockMod(new Dictionary<string, string>()
        {
            {
                "fiddle/fiddle.json", new JObject
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
                }.ToString()
            }
        });

        _installer.InstallMods([mod], _fileModifier);

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

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Ignore("Temporarily disabling nesting")]
    [Test]
    public void ShouldOnlyNestNewItems()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject
                {
                    {
                        "a", new JObject
                        {
                            { "b", "foo" }
                        }
                    }
                }.ToString()
            }
        });

        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/fiddle.json", new JObject
            {
                { "c", new JObject { { "d", "4" } } }
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);
        
        Assert.That(
            _fileModifier.GetFile("__fiddle__.json"), 
            Is.Not.Matches(new ContainsJsonConstraint(new JObject {
            { "a/b", "foo" }
        })));
        
        Assert.That(
            _fileModifier.GetFile("__fiddle__.json"), 
            new ContainsJsonConstraint(new JObject {
                { "c/d", "4" }
            }));
    }

    [Test]
    public void ShouldMergeArraysByDefault()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject {
                    { "a", new JArray { "1", "2", "3" } }
                }.ToString()
            }
        });

        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/fiddle.json", new JObject
            {
                { "a", new JArray { "4", "5" } }
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "a", new JArray { "4", "5", "3" } }
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldAllowSettingArraysToBeConcatted()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject {
                    { "a", new JArray { "1", "2", "3" } }
                }.ToString()
            }
        });

        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/fiddle.json", new JObject
            {
                { "__arrayMergeSetting", "Add" },
                { "a", new JArray { "4", "5" } }
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "a", new JArray { "1", "2", "3", "4", "5" } },
            {
                "extras", new JObject
                {
                    { "objects", new JArray() },
                    { "items", new JArray() }
                }
            },
            // { "a/0", "1" },
            // { "a/1", "2" },
            // { "a/2", "3" },
            // { "a/3", "4" },
            // { "a/4", "5" },
            { "extras/items", new JArray() },
            { "extras/objects", new JArray() },
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new MatchesJsonConstraint(expected));
    }

    [Test]
    public void ShouldAllowArrayReplacement()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject {
                    { "a", new JArray { "1", "2", "3" } }
                }.ToString()
            }
        });

        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/fiddle.json", new JObject
            {
                { "__arrayMergeSetting", "Replace" },
                { "a", new JArray { "4", "5" } }
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "a", new JArray { "4", "5" } },
            {
                "extras", new JObject
                {
                    { "objects", new JArray() },
                    { "items", new JArray() }
                }
            },
            // { "a/0", "4" },
            // { "a/1", "5" },
            { "extras/items", new JArray() },
            { "extras/objects", new JArray() },
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new MatchesJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldAllowArrayMergePerFile()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            {
                "__fiddle__.json", new JObject {
                    { "a", new JArray { "1", "2", "3" } }
                }.ToString()
            }
        });

        var mod = new MockMod(new Dictionary<string, string>
        {
            { "fiddle/fiddle1.json", new JObject
            {
                { "__arrayMergeSetting", "Add" },
                { "a", new JArray { "4", "5" } }
            }.ToString() },
            { "fiddle/fiddle2.json", new JObject
            {
                { "__arrayMergeSetting", "Merge" },
                { "a", new JArray { "6", "7" } }
            }.ToString() }
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "a", new JArray { "6", "7", "3", "4", "5" } },
            {
                "extras", new JObject
                {
                    { "objects", new JArray() },
                    { "items", new JArray() }
                }
            },
            // { "a/0", "6" },
            // { "a/1", "7" },
            // { "a/2", "3" },
            // { "a/3", "4" },
            // { "a/4", "5" },
            { "extras/items", new JArray() },
            { "extras/objects", new JArray() },
        };

        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new MatchesJsonConstraint(expected));
    }
}