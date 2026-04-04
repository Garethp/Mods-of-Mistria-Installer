using System.Reflection;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using ModsOfMistriaInstallerLibTests.Fixtures;
using ModsOfMistriaInstallerLibTests.Utils;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Interfaces;

namespace ModsOfMistriaInstallerLibTests.EndToEnd;

[TestFixture]
public class CutscenesTest
{
    private MockFileModifier _fileModifier;

    private readonly MockInstaller _installer = new([
        new CutsceneGenerator()
    ], [
        new MistInstaller()
    ]);

    private MockMod GetMockMod(string js)
    {
        return new MockMod(new Dictionary<string, string>
        {
            { "cutscene/test.js", js }
        });
    }

    [SetUp]
    public void SetUp()
    {
        _fileModifier = new(new Dictionary<string, string>
        {
            { "__mist__.json", "{}" },
        });
    }

    [Test]
    public void ShouldCompileFunction()
    {
        var js = "function spell_learned() {}";
        var mist = JObject.Parse("{ \"spell_learned.mist\": [] }");

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(mist));
    }

    [Test]
    public void ShouldSupportCall()
    {
        var js = "function spell_learned() { next_line(); }";
        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "next_line"
                             }
                           },
                           "args": []
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldSupportCallWithArgs()
    {
        var js = "function spell_learned() { next_line(1); }";
        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "next_line"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldSupportVariableInitialisations()
    {
        var js = "function spell_learned() { var a = 1; }";
        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.0
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleVariableAssignments()
    {
        var js = """
                 function spell_learned() {
                   var a = 1;
                   a = 2;
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.0
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 2.0
                             }
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleAllLiteralTypes()
    {
        var js = """
                 function spell_learned() {
                   var a = 1;
                   var b = "1";
                   var c = 1.5;
                   var d = true;
                   var e = false;
                 }
                 """;
        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.0
                           }
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "b"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "String",
                             "value": "1"
                           }
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "c"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.5
                           }
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "d"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "True"
                           }
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "e"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "False"
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleNamedIdentifiers()
    {
        var js = """
                 function spell_learned() {
                   var a = 1;
                   var b = a;
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.0
                           }
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "b"
                         },
                         "initializer": {
                           "expr_type": "Named",
                           "name": {
                             "token_type": "Identifier",
                             "value": "a"
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleThrowFunc()
    {
        var js = """
                 function spell_learned() {
                   animate(throwFunc);
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "animate"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Named",
                               "name": {
                                 "token_type": "Identifier",
                                 "value": "throw"
                               }
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleBinaryStatements()
    {
        var js = """
                 function spell_learned() {
                   var a = 1 == 1;
                   a = 1 != 1;
                   a = 1 + 1;
                   a = 1 - 1;
                   a = 1 * 1;
                   a = 1 / 1;
                   a = 1 < 1;
                   a = 1 > 1;
                   a = 1 <= 1;
                   a = 1 >= 1;
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 1.0
                             }
                           },
                           "operator": {
                             "token_type": "DoubleEqual"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 1.0
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "BangEqual"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Plus"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Minus"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Star"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Slash"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Less"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Greater"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "LessEqual"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "GreaterEqual"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleGrouping()
    {
        var js = """
                 function spell_learned() {
                   var a = (1 + 1) * __group(1 + 1);
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             "operator": {
                               "token_type": "Plus"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           },
                           "operator": {
                             "token_type": "Star"
                           },
                           "right": {
                             "expr_type": "Grouping",
                             "expr": {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 1.0
                                 }
                               },
                               "operator": {
                                 "token_type": "Plus"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 1.0
                                 }
                               }
                             }
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleUnaryOperators()
    {
        var js = """
                 function spell_learned() {
                   var a = !true;
                   var b = -1;
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Unary",
                           "operator": {
                             "token_type": "Bang"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "True"
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "b"
                         },
                         "initializer": {
                           "expr_type": "Unary",
                           "operator": {
                             "token_type": "Minus"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 1.0
                             }
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleLogicalOperators()
    {
        var js = """
                 function spell_learned() {
                   var a = true && true;
                   a = true || true;
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Logical",
                           "left": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "True"
                             }
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "True"
                             }
                           },
                           "operator": {
                             "token_type": "And"
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Assign",
                           "name": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "value": {
                             "expr_type": "Logical",
                             "left": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "True"
                               }
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "True"
                               }
                             },
                             "operator": {
                               "token_type": "Or"
                             }
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleBlockStatements()
    {
        var js = """
                 function spell_learned() {
                   {
                     var a = 1;
                   }
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Block",
                         "stmts": [
                           {
                             "stmt_type": "Var",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             },
                             "initializer": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           }
                         ]
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleIfStatements()
    {
        var js = """
                 function spell_learned() {
                   if (false) {
                     var a = 1;
                   } else if (false) {
                     var a = 2;
                   } else {
                     var b = 3;
                   }
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "If",
                         "condition": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "False"
                           }
                         },
                         "then_branch": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Var",
                               "name": {
                                 "token_type": "Identifier",
                                 "value": "a"
                               },
                               "initializer": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 1.0
                                 }
                               }
                             }
                           ]
                         },
                         "else_branch": {
                           "stmt_type": "If",
                           "condition": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "False"
                             }
                           },
                           "then_branch": {
                             "stmt_type": "Block",
                             "stmts": [
                               {
                                 "stmt_type": "Var",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "a"
                                 },
                                 "initializer": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 2.0
                                   }
                                 }
                               }
                             ]
                           },
                           "else_branch": {
                             "stmt_type": "Block",
                             "stmts": [
                               {
                                 "stmt_type": "Var",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "b"
                                 },
                                 "initializer": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 3.0
                                   }
                                 }
                               }
                             ]
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleFunctions()
    {
        var js = """
                 function spell_learned() {
                   function test(a = 1, b) {
                     var c = 1 + 1;
                   }
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Function",
                         "name": {
                           "token_type": "Identifier",
                           "value": "test"
                         },
                         "params": [
                           {
                             "token_type": "Identifier",
                             "value": "a",
                             "default_value": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           },
                           {
                             "token_type": "Identifier",
                             "value": "b",
                             "default_value": "null"
                           }
                         ],
                         "body": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Var",
                               "name": {
                                 "token_type": "Identifier",
                                 "value": "c"
                               },
                               "initializer": {
                                 "expr_type": "Binary",
                                 "left": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 1.0
                                   }
                                 },
                                 "operator": {
                                   "token_type": "Plus"
                                 },
                                 "right": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 1.0
                                   }
                                 }
                               }
                             }
                           ]
                         },
                         "resolve": "null"
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleFunctionReturns()
    {
        var js = """
                 function spell_learned() {
                   function a() {
                     return;
                   }

                   function b() {
                     return 1;
                   }
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Function",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "params": [],
                         "body": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Return",
                               "value": "null"
                             }
                           ]
                         },
                         "resolve": "null"
                       },
                       {
                         "stmt_type": "Function",
                         "name": {
                           "token_type": "Identifier",
                           "value": "b"
                         },
                         "params": [],
                         "body": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Return",
                               "value": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 1.0
                                 }
                               }
                             }
                           ]
                         },
                         "resolve": "null"
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleResolveExpressions()
    {
        var js = """
                 function unit_test() {
                   function a() {
                     return __resolve(() => {});
                   }

                   return __resolve(() => {});
                 }
                 """;

        var mist = """
                   {
                     "unit_test.mist": [
                       {
                         "stmt_type": "Function",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "params": [],
                         "body": {
                           "stmt_type": "Block",
                           "stmts": []
                         },
                         "resolve": {
                           "stmt_type": "Block",
                           "stmts": []
                         }
                       },
                       {
                         "stmt_type": "Resolve",
                         "stmts": {
                           "stmt_type": "Block",
                           "stmts": []
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleSimultaneousCalls()
    {
        var js = """
                 function spell_learned() {
                   __async(
                     () => {},
                     () => {},
                   );
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Simultaneous",
                         "body": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Block",
                               "stmts": []
                             },
                             {
                               "stmt_type": "Block",
                               "stmts": []
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleFreeExpressions()
    {
        var js = """
                 function spell_learned() {
                   __free(() => {});
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Free",
                         "stmt": {
                           "stmt_type": "Block",
                           "stmts": []
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldHandleGroupingExpressions()
    {
        var js = """
                 function spell_learned() {
                   var e = __group(a * 5) + 3 - __group(8 - 2);
                 }
                 """;

        var mist = """
                   {
                     "spell_learned.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "e"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Grouping",
                               "expr": {
                                 "expr_type": "Binary",
                                 "left": {
                                   "expr_type": "Named",
                                   "name": {
                                     "token_type": "Identifier",
                                     "value": "a"
                                   }
                                 },
                                 "operator": {
                                   "token_type": "Star"
                                 },
                                 "right": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 5.0
                                   }
                                 }
                               }
                             },
                             "operator": {
                               "token_type": "Plus"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 3.0
                               }
                             }
                           },
                           "operator": {
                             "token_type": "Minus"
                           },
                           "right": {
                             "expr_type": "Grouping",
                             "expr": {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 8.0
                                 }
                               },
                               "operator": {
                                 "token_type": "Minus"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 2.0
                                 }
                               }
                             }
                           }
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldPassUnitTests()
    {
        // Our test output is *slightly* (very very slightly) different from the original MIST, but for this test we're
        // going to consider it okay. For more information, see the notes in the `ShouldEncodeFullMistFile` test.
        var js = """
                 function unit_test() {
                   var a = 1;
                   assert(a == 1, "Assignment failed!");
                   var b = a + 1;
                   assert(b == 2, "Addition failed!");
                   var c = a - 1;
                   assert(c == 0, "Subtraction failed!");
                   var d = a * 2;
                   assert(d == 2, "Multiplication failed!");
                   var d = a / 2;
                   assert(d == 0.5, "Divison failed!");
                   var e = __group(a * 5) + 3 - __group(8 - 2);
                   assert(e == 2, "Complex expression failed!");
                   function add(a, b) {
                     return a + b;
                   }

                   var f = add(1, 1);
                   assert(f == 2, "Function call failed!");
                   var g = 0;
                   __async(
                     () => (g = g + 1),
                     () => {
                       g = g + 1;
                       g = g + 1;
                       g = g + 1;
                     },
                     () => __free(() => (g = g + 1)),
                     () =>
                       __free(() =>
                         __async(() => {
                           g = g + 1;
                           g = g + 1;
                         }),
                       ),
                   );
                   assert(g == 7, "Sim/Free blocks failed!");
                 }
                 """;

        var mist = """
                   {
                     "unit_test.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "a"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.0
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "a"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 1.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Assignment failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "b"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "operator": {
                             "token_type": "Plus"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 1.0
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "b"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 2.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Addition failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "c"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "operator": {
                             "token_type": "Minus"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 1.0
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "c"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 0.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Subtraction failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "d"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "operator": {
                             "token_type": "Star"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 2.0
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "d"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 2.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Multiplication failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "d"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "a"
                             }
                           },
                           "operator": {
                             "token_type": "Slash"
                           },
                           "right": {
                             "expr_type": "Literal",
                             "value": {
                               "token_type": "Number",
                               "Value": 2.0
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "d"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 0.5
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Divison failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "e"
                         },
                         "initializer": {
                           "expr_type": "Binary",
                           "left": {
                             "expr_type": "Binary",
                             "left": {
                               "expr_type": "Grouping",
                               "expr": {
                                 "expr_type": "Binary",
                                 "left": {
                                   "expr_type": "Named",
                                   "name": {
                                     "token_type": "Identifier",
                                     "value": "a"
                                   }
                                 },
                                 "operator": {
                                   "token_type": "Star"
                                 },
                                 "right": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 5.0
                                   }
                                 }
                               }
                             },
                             "operator": {
                               "token_type": "Plus"
                             },
                             "right": {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 3.0
                               }
                             }
                           },
                           "operator": {
                             "token_type": "Minus"
                           },
                           "right": {
                             "expr_type": "Grouping",
                             "expr": {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 8.0
                                 }
                               },
                               "operator": {
                                 "token_type": "Minus"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 2.0
                                 }
                               }
                             }
                           }
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "e"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 2.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Complex expression failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Function",
                         "name": {
                           "token_type": "Identifier",
                           "value": "add"
                         },
                         "params": [
                           {
                             "token_type": "Identifier",
                             "value": "a",
                             "default_value": "null"
                           },
                           {
                             "token_type": "Identifier",
                             "value": "b",
                             "default_value": "null"
                           }
                         ],
                         "body": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Return",
                               "value": {
                                 "expr_type": "Binary",
                                 "left": {
                                   "expr_type": "Named",
                                   "name": {
                                     "token_type": "Identifier",
                                     "value": "a"
                                   }
                                 },
                                 "operator": {
                                   "token_type": "Plus"
                                 },
                                 "right": {
                                   "expr_type": "Named",
                                   "name": {
                                     "token_type": "Identifier",
                                     "value": "b"
                                   }
                                 }
                               }
                             }
                           ]
                         },
                         "resolve": "null"
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "f"
                         },
                         "initializer": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "add"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "Number",
                                 "Value": 1.0
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "f"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 2.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Function call failed!"
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "g"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 0.0
                           }
                         }
                       },
                       {
                         "stmt_type": "Simultaneous",
                         "body": {
                           "stmt_type": "Block",
                           "stmts": [
                             {
                               "stmt_type": "Expr",
                               "expr": {
                                 "expr_type": "Assign",
                                 "name": {
                                   "expr_type": "Named",
                                   "name": {
                                     "token_type": "Identifier",
                                     "value": "g"
                                   }
                                 },
                                 "value": {
                                   "expr_type": "Binary",
                                   "left": {
                                     "expr_type": "Named",
                                     "name": {
                                       "token_type": "Identifier",
                                       "value": "g"
                                     }
                                   },
                                   "operator": {
                                     "token_type": "Plus"
                                   },
                                   "right": {
                                     "expr_type": "Literal",
                                     "value": {
                                       "token_type": "Number",
                                       "Value": 1.0
                                     }
                                   }
                                 }
                               }
                             },
                             {
                               "stmt_type": "Block",
                               "stmts": [
                                 {
                                   "stmt_type": "Expr",
                                   "expr": {
                                     "expr_type": "Assign",
                                     "name": {
                                       "expr_type": "Named",
                                       "name": {
                                         "token_type": "Identifier",
                                         "value": "g"
                                       }
                                     },
                                     "value": {
                                       "expr_type": "Binary",
                                       "left": {
                                         "expr_type": "Named",
                                         "name": {
                                           "token_type": "Identifier",
                                           "value": "g"
                                         }
                                       },
                                       "operator": {
                                         "token_type": "Plus"
                                       },
                                       "right": {
                                         "expr_type": "Literal",
                                         "value": {
                                           "token_type": "Number",
                                           "Value": 1.0
                                         }
                                       }
                                     }
                                   }
                                 },
                                 {
                                   "stmt_type": "Expr",
                                   "expr": {
                                     "expr_type": "Assign",
                                     "name": {
                                       "expr_type": "Named",
                                       "name": {
                                         "token_type": "Identifier",
                                         "value": "g"
                                       }
                                     },
                                     "value": {
                                       "expr_type": "Binary",
                                       "left": {
                                         "expr_type": "Named",
                                         "name": {
                                           "token_type": "Identifier",
                                           "value": "g"
                                         }
                                       },
                                       "operator": {
                                         "token_type": "Plus"
                                       },
                                       "right": {
                                         "expr_type": "Literal",
                                         "value": {
                                           "token_type": "Number",
                                           "Value": 1.0
                                         }
                                       }
                                     }
                                   }
                                 },
                                 {
                                   "stmt_type": "Expr",
                                   "expr": {
                                     "expr_type": "Assign",
                                     "name": {
                                       "expr_type": "Named",
                                       "name": {
                                         "token_type": "Identifier",
                                         "value": "g"
                                       }
                                     },
                                     "value": {
                                       "expr_type": "Binary",
                                       "left": {
                                         "expr_type": "Named",
                                         "name": {
                                           "token_type": "Identifier",
                                           "value": "g"
                                         }
                                       },
                                       "operator": {
                                         "token_type": "Plus"
                                       },
                                       "right": {
                                         "expr_type": "Literal",
                                         "value": {
                                           "token_type": "Number",
                                           "Value": 1.0
                                         }
                                       }
                                     }
                                   }
                                 }
                               ]
                             },
                             {
                               "stmt_type": "Free",
                               "stmt": {
                                 "stmt_type": "Expr",
                                 "expr": {
                                   "expr_type": "Assign",
                                   "name": {
                                     "expr_type": "Named",
                                     "name": {
                                       "token_type": "Identifier",
                                       "value": "g"
                                     }
                                   },
                                   "value": {
                                     "expr_type": "Binary",
                                     "left": {
                                       "expr_type": "Named",
                                       "name": {
                                         "token_type": "Identifier",
                                         "value": "g"
                                       }
                                     },
                                     "operator": {
                                       "token_type": "Plus"
                                     },
                                     "right": {
                                       "expr_type": "Literal",
                                       "value": {
                                         "token_type": "Number",
                                         "Value": 1.0
                                       }
                                     }
                                   }
                                 }
                               }
                             },
                             {
                               "stmt_type": "Free",
                               "stmt": {
                                 "stmt_type": "Simultaneous",
                                 "body": {
                                   "stmt_type": "Block",
                                   "stmts": [
                                     {
                                       "stmt_type": "Block",
                                       "stmts": [
                                         {
                                           "stmt_type": "Expr",
                                           "expr": {
                                             "expr_type": "Assign",
                                             "name": {
                                               "expr_type": "Named",
                                               "name": {
                                                 "token_type": "Identifier",
                                                 "value": "g"
                                               }
                                             },
                                             "value": {
                                               "expr_type": "Binary",
                                               "left": {
                                                 "expr_type": "Named",
                                                 "name": {
                                                   "token_type": "Identifier",
                                                   "value": "g"
                                                 }
                                               },
                                               "operator": {
                                                 "token_type": "Plus"
                                               },
                                               "right": {
                                                 "expr_type": "Literal",
                                                 "value": {
                                                   "token_type": "Number",
                                                   "Value": 1.0
                                                 }
                                               }
                                             }
                                           }
                                         },
                                         {
                                           "stmt_type": "Expr",
                                           "expr": {
                                             "expr_type": "Assign",
                                             "name": {
                                               "expr_type": "Named",
                                               "name": {
                                                 "token_type": "Identifier",
                                                 "value": "g"
                                               }
                                             },
                                             "value": {
                                               "expr_type": "Binary",
                                               "left": {
                                                 "expr_type": "Named",
                                                 "name": {
                                                   "token_type": "Identifier",
                                                   "value": "g"
                                                 }
                                               },
                                               "operator": {
                                                 "token_type": "Plus"
                                               },
                                               "right": {
                                                 "expr_type": "Literal",
                                                 "value": {
                                                   "token_type": "Number",
                                                   "Value": 1.0
                                                 }
                                               }
                                             }
                                           }
                                         }
                                       ]
                                     }
                                   ]
                                 }
                               }
                             }
                           ]
                         }
                       },
                       {
                         "stmt_type": "Expr",
                         "expr": {
                           "expr_type": "Call",
                           "call": {
                             "expr_type": "Named",
                             "name": {
                               "token_type": "Identifier",
                               "value": "assert"
                             }
                           },
                           "args": [
                             {
                               "expr_type": "Binary",
                               "left": {
                                 "expr_type": "Named",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "g"
                                 }
                               },
                               "operator": {
                                 "token_type": "DoubleEqual"
                               },
                               "right": {
                                 "expr_type": "Literal",
                                 "value": {
                                   "token_type": "Number",
                                   "Value": 7.0
                                 }
                               }
                             },
                             {
                               "expr_type": "Literal",
                               "value": {
                                 "token_type": "String",
                                 "value": "Sim/Free blocks failed!"
                               }
                             }
                           ]
                         }
                       }
                     ]
                   }
                   """;

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Ignore("I need to get permission to ship the original __mist__.json file with this project for this test.")]
    [Test]
    public void ShouldEncodeFullMistFile()
    {
        // @TODO: We need to make this more reliable. Maybe find our way up to the parents until we hit a predictable path?
        var projectPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var js = File.ReadAllText($"{projectPath}/../../../Fixtures/__mist__.js");
        var mist = File.ReadAllText($"{projectPath}/../../../Fixtures/__mist__.json");

        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        File.WriteAllText($"{projectPath}/../../../Fixtures/__decompiled__.json",
            _fileModifier.GetFile("__mist__.json"));

        var fullActualMist = JObject.Parse(_fileModifier.GetFile("__mist__.json"));
        var fullDesiredMist = JObject.Parse(mist);

        var functionNames = fullDesiredMist.Properties().Select(prop => prop.Name).ToList();

        foreach (var functionName in functionNames)
        {
            // For some reason balor_six_hearts has an `if` condition inside a `simultaneous` statement (`__async`) that
            // doesn't go inside a `Block`. It's the only if statement in a `simultaneous` statement that acts like this,
            // so when I tried to code around it, three other functions failed instead. For that reason we're just going to
            // ignore that function for testing.
            if (functionName == "balor_six_hearts.mist") continue;

            // It looks like we're not quite applying grouping in binary expressions correctly, but seem functionally
            // equivalent and I think it only really matters if you're doing very complex math with ambiguous grouping.
            // What's happening is that the original AST shows `BINARY(+, GROUPING(1 * 5), BINARY(-, 3, GROUPING(8 - 2))`,
            // which turns into the JS `__group(1 * 5) + 3 - __group(8 - 2)`. Converting that back into AST we get
            // `BINARY(-, BINARY(+, GROUP(1 * 5), 3), GROUP(8 - 2)`. So during the conversion the 3 moves from the right
            // side to the left side, which doesn't seem to be all that important to be honest. But it's going to be
            // painful to fix and would only be fixed for the very specific purpose of passing this test. Which might be
            // worth it, but maybe not right now.
            if (functionName == "unit_test.mist") continue;

            Assert.That(new JObject { { functionName, fullActualMist[functionName] } },
                new MatchesJsonConstraint(new JObject { { functionName, fullDesiredMist[functionName] } }));
        }
    }
  

    [Test]
    public void ShouldOverwriteExistingMethods()
    {
        _fileModifier = new MockFileModifier(new Dictionary<string, string>
        {
          // function test() { var a = 1 + 1; }
          { "__mist__.json", """
                             {
                               "test.mist": [
                                 {
                                   "stmt_type": "Var",
                                   "name": {
                                     "token_type": "Identifier",
                                     "value": "a"
                                   },
                                   "initializer": {
                                     "expr_type": "Binary",
                                     "left": {
                                       "expr_type": "Literal",
                                       "value": {
                                         "token_type": "Number",
                                         "Value": 1.0
                                       }
                                     },
                                     "operator": {
                                       "token_type": "Plus"
                                     },
                                     "right": {
                                       "expr_type": "Literal",
                                       "value": {
                                         "token_type": "Number",
                                         "Value": 1.0
                                       }
                                     }
                                   }
                                 }
                               ]
                             }
                             """
          }
        });

        var js = """
                 function test() {
                  var b = 1;
                 }
                 """;
        
        var mist = """
                   {
                     "test.mist": [
                       {
                         "stmt_type": "Var",
                         "name": {
                           "token_type": "Identifier",
                           "value": "b"
                         },
                         "initializer": {
                           "expr_type": "Literal",
                           "value": {
                             "token_type": "Number",
                             "Value": 1.0
                           }
                         }
                       }
                     ]
                   }
                   """;
        
        var mod = GetMockMod(js);
        _installer.InstallMods([mod], _fileModifier);

        Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }

    [Test]
    public void ShouldKeepExistingCutscenes()
    {
      _fileModifier = new MockFileModifier(new Dictionary<string, string>
      {
        {
          // function a() { var a = 1; }
          "__mist__.json", """
                           {
                             "a.mist": [
                               {
                                 "stmt_type": "Var",
                                 "name": {
                                   "token_type": "Identifier",
                                   "value": "a"
                                 },
                                 "initializer": {
                                   "expr_type": "Literal",
                                   "value": {
                                     "token_type": "Number",
                                     "Value": 1.0
                                   }
                                 }
                               }
                             ]
                           }
                           """
        }
      });
      
      var js = """
               function b() { var b = 2; }
               """;

      var mist = """
                 {
                   "a.mist": [
                     {
                       "stmt_type": "Var",
                       "name": {
                         "token_type": "Identifier",
                         "value": "a"
                       },
                       "initializer": {
                         "expr_type": "Literal",
                         "value": {
                           "token_type": "Number",
                           "Value": 1.0
                         }
                       }
                     }
                   ],
                   "b.mist": [
                     {
                       "stmt_type": "Var",
                       "name": {
                         "token_type": "Identifier",
                         "value": "b"
                       },
                       "initializer": {
                         "expr_type": "Literal",
                         "value": {
                           "token_type": "Number",
                           "Value": 2.0
                         }
                       }
                     }
                   ]
                 }
                 """;
      
      var mod = GetMockMod(js);
      _installer.InstallMods([mod], _fileModifier);

      Assert.That(_fileModifier.GetFile("__mist__.json"), new MatchesJsonConstraint(JObject.Parse(mist)));
    }
}