using Esprima;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public class MistCompiler
{
    public string Compile(string input)
    {
        if (!File.Exists(input))
        {
            throw new Exception("File not found: " + input);
        }

        var contents = File.ReadAllText(input);

        var parser = new JavaScriptParser();
        var script = parser.ParseScript(contents);
        
        return "";
    }
}