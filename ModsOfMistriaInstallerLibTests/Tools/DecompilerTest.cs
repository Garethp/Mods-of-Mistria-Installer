using Esprima.Utils;
using Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModsOfMistriaInstallerLibTests.Tools;

[TestFixture]
public class DecompilerTest
{

    [SetUp]
    public void SetUp()
    {
    }

    [Test]
    public void ShouldDecompileEmptyFunction()
    {
        string ast = """
{
    "stmt_type": "Function",
    "name": {"token_type": "Identifier", "value": "go_to_bed"},
    "params": [],
    "body": {
        "stmt_type": "Block",
        "stmts": []
    },
    "resolve": "null"
}
""";
        string expected_js = "function go_to_bed(){}";

        MistContainerConverter mistContainerConverter = new MistContainerConverter();
        JObject obj = (JObject)JToken.Parse(ast);
        string decompiled_js = mistContainerConverter.ToStatement(obj).ToJavaScriptString();

        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }

    [Test]
    public void ShouldDecompileFunction()
    {
        string ast = """
{
    "stmt_type": "Function",
    "name": {"token_type": "Identifier", "value": "go_to_bed"},
    "params": [],
    "body": {
        "stmt_type": "Block",
        "stmts": [
            {
                "stmt_type": "Return", 
                "value": {
                    "expr_type": "Call", 
                    "call": {
                        "expr_type": "Named", 
                        "name": {
                            "token_type": "Identifier", 
                            "value": "__await_ari_animation"
                        }
                    }, 
                    "args": []
                }
            }
        ]
    },
    "resolve": "null"
}
""";
        string expected_js = "function go_to_bed(){return __await_ari_animation()}";

        MistContainerConverter mistContainerConverter = new MistContainerConverter();
        JObject obj = (JObject)JToken.Parse(ast);
        string decompiled_js = mistContainerConverter.ToStatement(obj).ToJavaScriptString();

        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }

    [Test]
    public void ShouldDecompileExpressionStatement()
    {
        string ast = """
{
    "stmt_type": "Expr",
    "expr": {
        "expr_type": "Call",
        "call": {
            "expr_type": "Named",
            "name": {
                "token_type": "Identifier",
                "value": "camera_follow"
            }
        },
        "args": [
            {
                "expr_type": "Named",
                "name": {"token_type": "Identifier", "value": "ari"}
            }
        ]
    }
}
""";
        string expected_js = "camera_follow(ari)";

        MistContainerConverter mistContainerConverter = new MistContainerConverter();
        JObject obj = (JObject)JToken.Parse(ast);
        string decompiled_js = mistContainerConverter.ToStatement(obj).ToJavaScriptString();
        Assert.That(decompiled_js, Is.EqualTo(expected_js));

        ast = """
            
{
    "stmt_type": "Expr", 
    "expr": {
        "expr_type": "Call", 
        "call": {
            "expr_type": "Named", 
            "name": {"token_type": "Identifier", "value": "face"}
        }, 
        "args": [
            {
                "expr_type": "Named", 
                "name": {"token_type": "Identifier", "value": "ari"}
            }, 
            {
                "expr_type": "Named", 
                "name": {"token_type": "Identifier", "value": "south"}
            }
        ]
    }
}
""";
        // FIXME: We may need our own Writer to add space after the comma.
        expected_js = "face(ari,south)";

        obj = (JObject)JToken.Parse(ast);
        decompiled_js = mistContainerConverter.ToStatement(obj).ToJavaScriptString();


        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }

    [Test]
    public void ShouldDecompileIfStatement()
    {
        string ast = """
{
    "stmt_type": "If", 
    "condition": {
        "expr_type": "Binary", 
        "left": {
            "expr_type": "Call", 
            "call": {
                "expr_type": "Named", 
                "name": {
                    "token_type": "Identifier", 
                    "value": "get_response"
                }
            }, 
            "args": []
        }, 
        "operator": {"token_type": "DoubleEqual"}, 
        "right": {
            "expr_type": "Literal", 
            "value": {"token_type": "Number", "Value": 0.0}
        }
    }, 
    "then_branch": {
        "stmt_type": "Block", 
        "stmts": [
            {
                "stmt_type": "Expr", 
                "expr": {
                    "expr_type": "Call", 
                    "call": {
                        "expr_type": "Named", 
                        "name": {
                            "token_type": "Identifier", 
                            "value": "close_textbox"
                        }
                    }, 
                    "args": []
                }
            }
        ]
        }, 
    "else_branch": {
        "stmt_type": "Block", 
        "stmts": [
            {
                "stmt_type": "Expr", 
                "expr": {
                    "expr_type": "Call", 
                    "call": {
                        "expr_type": "Named", 
                        "name": {
                            "token_type": "Identifier", 
                            "value": "close_textbox"
                        }
                    }, 
                    "args": []
                }
            }
        ]
    }
}
""";
        string expected_js = "if(get_response()==0){close_textbox()}else{close_textbox()}";

        MistContainerConverter mistContainerConverter = new MistContainerConverter();
        JObject obj = (JObject)JToken.Parse(ast);
        string decompiled_js = mistContainerConverter.ToStatement(obj).ToJavaScriptString();
        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }

    [Test]
    public void ShouldDecompileReturnStatement()
    {
        string ast = """
{
    "stmt_type": "Return", 
    "value": {
        "expr_type": "Call", 
        "call": {
            "expr_type": "Named", 
            "name": {
                "token_type": "Identifier", 
                "value": "__await_ari_animation"
            }
        }, 
        "args": []
    }
}
""";
        string expected_js = "return __await_ari_animation()";

        MistContainerConverter mistContainerConverter = new MistContainerConverter();
        JObject obj = (JObject)JToken.Parse(ast);
        string decompiled_js = mistContainerConverter.ToStatement(obj).ToJavaScriptString();
        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }

    [Test]
    public void ShouldDecompileBasicBinaryExpression()
    {
        string ast = """
{
    "expr_type": "Binary", 
    "left": {
        "expr_type": "Literal", 
        "value": {"token_type": "True"}
    }, 
    "operator": {"token_type": "DoubleEqual"}, 
    "right": {
        "expr_type": "Literal", 
        "value": {"token_type": "False"}
    }
}
""";
        string expected_js = "true==false";

        MistContainerConverter mistContainerConverter = new MistContainerConverter();
        JObject obj = (JObject)JToken.Parse(ast);
        string decompiled_js = mistContainerConverter.ToExpression(obj).ToJavaScriptString();
        Assert.That(decompiled_js, Is.EqualTo(expected_js));

        ast = """
{
    "expr_type": "Logical", 
    "left": {
        "expr_type": "Literal", 
        "value": {"token_type": "True"}
    }, 
    "operator": {"token_type": "And"},
    "right": {
        "expr_type": "Literal", 
        "value": {"token_type": "False"}
    }
}
""";
        expected_js = "true&&false";

        mistContainerConverter = new MistContainerConverter();
        obj = (JObject)JToken.Parse(ast);
        decompiled_js = mistContainerConverter.ToExpression(obj).ToJavaScriptString();
        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }

    [Test]
    [Ignore("The file day_zero.json was not committed. The expected_js is shorter than the actual result. Not all decompilations are implemented.")]
    public void ShouldDecompileMistAst()
    {
        var decompiler = new MistDecompiler();
        string decompiled_js = decompiler.Decompile("..\\..\\..\\..\\mists\\day_zero.json");

        string expected_js = """
function go_to_bed()
{
    var x = __get_new_day_spawn_x();
    var y = __get_new_day_spawn_y();
    camera_follow(ari);
    simultaneous
    {
        walk(ari, x, y);
        set_move_speed(ari, 0.5);
    }
    face(ari, south);
    wait(1);
    request_music_stop(1);
    simultaneous
    {
        play_sound(""Music/Jingles/GoToSleep"");
        {
            animate(ari, blink);
            wait(0.2);
            freeze_ari();
        }
        fade_out(8);
    }
}
""";
        
        Assert.That(decompiled_js, Is.EqualTo(expected_js));
    }
}