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