using Esprima;
using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public class MistDecompiler
{
    public string Decompile(string input)
    {
        var text = File.ReadAllText(input);

        var data = JObject.Parse(text);
        
        var functions = NodeList.Create(data.Properties().Select(Statement (p) =>
        {
            return new FunctionDeclaration(
                new Identifier(p.Name.Replace(".mist", "")),
                [],
                new BlockStatement([]),
                false, 
                false, 
                false
            );
        }));
        
        return new Script(functions, false).ToString();
    }
}