using Esprima.Ast;
using Esprima.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using JsonWriter = Newtonsoft.Json.JsonWriter;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public class MistConverter : JsonConverter<Module>
{
    public override Module? ReadJson(JsonReader reader, Type objectType, Module? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        // This, to my understanding, load the entire JSON tree.
        var token = JToken.Load(reader);

        if (token is not JObject obj) throw new NotImplementedException();

        return new Module(NodeList.Create<Statement>(obj.Properties().Select(property =>
        {
            return new FunctionDeclaration(new Identifier(property.Name.Replace(".mist", "")), [], new BlockStatement(
                NodeList.Create<Statement>(property.Value.Select(statement =>
                {
                    if (statement is not JObject statementObject)
                        throw new NotImplementedException($"Expected statement type: {statement.Type}");

                    return ToStatement(statementObject);
                }))
            ), false, false, false);
        })));
    }

    public static Statement ToStatement(JObject obj)
    {
        return StatementDecompiler.Decompile(obj);
    }

    public static Expression ToExpression(JObject obj)
    {
        return ExpressionDecompiler.Decompile(obj);
    }

    public override void WriteJson(JsonWriter writer, Module? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class MistDecompiler
{
    public string Decompile(string mist)
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new MistConverter());

        var container = JsonConvert.DeserializeObject<Module>(mist, settings);
        if (container is null)
            throw new Exception("Unable to convert Mist File");

        return container.ToJavaScriptString(true).Replace("throw", "throwFunc");
    }
}