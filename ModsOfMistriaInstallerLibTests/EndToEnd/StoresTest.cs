using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Installer.UMT;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.TestUtils;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class StoresTest
{
    private MockInstaller _installer = new MockInstaller(
        [new StoreGenerator()],
        [new FiddleInstaller()]
    );

    private MockFileModifier _fileModifier;

    [SetUp]
    public void Setup()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            { "__fiddle__.json", new JObject
            {
                { "stores", new JObject
                {
                    { "general", new JObject
                    {
                        { "name", "general" },
                        { "categories", new JArray
                        {
                            new JObject
                            {
                                { "icon", "existing" },
                                { "constant_stock", new JArray() }
                            }
                        } }
                    } }
                }}
            }.ToString() }
        });
    }
    
    [Test]
    public void ShouldAllowAddingNewCategories()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "categories", new JArray
                {
                    new JObject
                    {
                        { "store", "general" },
                        { "icon_name", "new_icon" },
                        { "sprite", "new_icon" }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "new_icon" }
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAddNewCategorySprites()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "categories", new JArray
                {
                    new JObject
                    {
                        { "store", "general" },
                        { "icon_name", "new_icon" },
                        { "sprite", "images/sprite.png" }
                    }
                } }
            }.ToString()}
        });

        var information = _installer.InstallMods([mod], _fileModifier);

        Assert.That(information.Sprites, Contains.Key(mod.GetId()));
        Assert.That(
            information.Sprites[mod.GetId()],
            Has.Some.Matches((SpriteData sprite) =>
                sprite is
                {
                    Name: "new_icon",
                    Location: "images/sprite.png",
                    IsUiSprite: true
                }
            )
        );
    }

    [Test]
    public void ShouldCheckStoreExistsWhenAddingNewCategories()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "categories", new JArray
                {
                    new JObject
                    {
                        { "store", "non-existing" },
                        { "icon_name", "new_icon" },
                        { "sprite", "new_icon" }
                    }
                } }
            }.ToString()}
        });
        
        var exception = Assert.Throws<Exception>(() => _installer.InstallMods([mod], _fileModifier));
        Assert.That(exception.Message, Contains.Substring("Could not add category new_icon to non-existing because non-existing does not exist"));
    }

    [Test]
    public void ShouldSetTargetSelectionsForNewCategory()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "categories", new JArray
                {
                    new JObject
                    {
                        { "store", "general" },
                        { "icon_name", "new_icon" },
                        { "sprite", "images/sprite.png" },
                        { "target_selections", 5 }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "new_icon" },
                            { "target_selections", 5 },
                            { "random_stock", new JArray() }
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldSetTargetSelectionsForExistingCategory()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            { "__fiddle__.json", new JObject
            {
                { "stores", new JObject
                {
                    { "general", new JObject
                    {
                        { "name", "general" },
                        { "categories", new JArray
                        {
                            new JObject
                            {
                                { "icon", "existing" }
                            }
                        } }
                    } }
                }}
            }.ToString() }
        });
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "categories", new JArray
                {
                    new JObject
                    {
                        { "store", "general" },
                        { "icon_name", "existing" },
                        { "sprite", "images/sprite.png" },
                        { "target_selections", 5 }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "target_selections", 5 },
                            { "random_stock", new JArray() }
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAcceptLegacyRandomSelectionsKey()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "categories", new JArray
                {
                    new JObject
                    {
                        { "store", "general" },
                        { "icon_name", "new_icon" },
                        { "sprite", "images/sprite.png" },
                        { "random_selections", 5 }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "new_icon" },
                            { "target_selections", 5 },
                            { "random_stock", new JArray() }
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAllowAddingItemsToStoreCategory()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "constant_stock", new JArray
                            {
                                new JObject { { "item", "seed_turnip" } }
                            }}
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldCheckStoreExistsWhenAddingNewItems()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "non-existing" },
                        { "category", "existing" }
                    }
                } }
            }.ToString()}
        });
        
        var exception = Assert.Throws<Exception>(() => _installer.InstallMods([mod], _fileModifier));
        Assert.That(exception.Message, Does.StartWith("Failed adding item to the non-existing existing category because non-existing does not exist"));
    }

    [Test]
    public void ShouldCheckCategoryExistsWhenAddingNewItems()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "non-existing" }
                    }
                } }
            }.ToString()}
        });
        
        var exception = Assert.Throws<Exception>(() => _installer.InstallMods([mod], _fileModifier));
        Assert.That(exception.Message, Does.StartWith("Failed adding item to the general non-existing category because non-existing does not exist"));

    }

    [Test]
    public void ShouldAllowAddingItemToRandomStock()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" },
                        { "random_stock", true }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "random_stock", new JArray
                            {
                                new JObject { { "item", "seed_turnip" } }
                            }}
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldEnsureRandomStockWhenAddingRandomStockItem()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
            { "__fiddle__.json", new JObject
            {
                { "stores", new JObject
                {
                    { "general", new JObject
                    {
                        { "name", "general" },
                        { "categories", new JArray
                        {
                            new JObject
                            {
                                { "icon", "existing" }
                            }
                        } }
                    } }
                }}
            }.ToString() }
        });
        
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" },
                        { "random_stock", true }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "random_stock", new JArray
                            {
                                new JObject { { "item", "seed_turnip" } }
                            }}
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldAddNonSeasonalItemsToConstantStock()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "constant_stock", new JArray
                            {
                                new JObject { { "item", "seed_turnip" } }
                            }}
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [TestCase("spring")]
    [TestCase("summer")]
    [TestCase("fall")]
    [TestCase("winter")]
    public void ShouldAllowAddingSeasonalStockItems(string season)
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" },
                        { "season", season }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "seasonal", new JObject
                            {
                                { season, new JArray
                                {
                                    new JObject { { "item", "seed_turnip" } }
                                }}
                            }}
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }

    [Test]
    public void ShouldEnsureSeasonsWhenAddingSeasonalItems()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" },
                        { "season", "spring" }
                    }
                } }
            }.ToString()}
        });

        _installer.InstallMods([mod], _fileModifier);

        var expected = new JObject
        {
            { "stores", new JObject
            {
                { "general", new JObject
                {
                    { "categories", new JArray
                    {
                        new JObject
                        {
                            { "icon", "existing" },
                            { "seasonal", new JObject
                            {
                                { "spring", new JArray() },
                                { "summer", new JArray() },
                                { "fall", new JArray() },
                                { "winter", new JArray() }
                            }}
                        }
                    }}
                }}
            }}
        };
        
        Assert.That(_fileModifier.GetFile("__fiddle__.json"), new ContainsJsonConstraint(expected));
    }
    
    [Test]
    public void ShouldCheckSeasonExistsWhenAddingSeasonalItems()
    {
        var mod = new MockMod(new Dictionary<string, string>
        {
            { "stores/store.json", new JObject
            {
                { "items", new JArray
                {
                    new JObject
                    {
                        { "item", "seed_turnip" },
                        { "store", "general" },
                        { "category", "existing" },
                        { "season", "not-a-season" }
                    }
                } }
            }.ToString()}
        });

        var exception = Assert.Throws<Exception>(delegate { _installer.InstallMods([mod], _fileModifier); });
        Assert.That(exception.Message, Is.EqualTo("Season not-a-season does not exist in general existing"));
    }
}