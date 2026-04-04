using Esprima;
using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public class MistCompiler
{
    public JObject Compile(string contents)
    {
        var parser = new JavaScriptParser();
        var script = parser.ParseScript(contents);

        var mist = new JObject();

        foreach (var statement in script.Body)
        {
            if (statement is not FunctionDeclaration functionDeclaration)
                throw new Exception("All top level tokens must be function declarations");

            var compiledFunction = new JArray(functionDeclaration.Body.Body.ToList().Select(Encoder.EncodeJS));

            mist.Add($"{functionDeclaration.Id}.mist", compiledFunction);
            var a = 1 + 1;
        }

        return mist;
    }
}