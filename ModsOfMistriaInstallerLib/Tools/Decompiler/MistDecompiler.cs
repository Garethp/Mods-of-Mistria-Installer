using Esprima;
using Esprima.Ast;
using Esprima.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public class MistContainer
{
    public List<MistProgram> Programs;
}

public class MistProgram
{
    public string Name;
    public NodeList<Node> Statements;

    public string ToJavaScriptString()
    {
        string result = "";
        foreach (var stmt in Statements)
        {
            if (stmt != null)
            {
                result += AstToJavaScript.ToJavaScriptString(stmt);
            }
        }
        return result;
    }
}

public class MistContainerConverter : JsonConverter<MistContainer>
{
    public override MistContainer? ReadJson(JsonReader reader, Type objectType, MistContainer? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Object)
        {
            Debug.WriteLine("found object");

            MistContainer container = new MistContainer();
            container.Programs = new List<MistProgram>();

            JObject obj = (JObject)token;
            foreach (var prop in obj.Properties())
            {
                Debug.WriteLine($"{prop.Name}");
                JToken statements = prop.Value;

                List<Node> nodes = new List<Node>();

                MistProgram mist = new MistProgram();
                mist.Name = prop.Name.ToString();
                mist.Statements = NodeList.Create(statements.Select(stmt => this.ToStatement((JObject)stmt)));

                container.Programs.Add(mist);
            }

            return container;
        }
        else
        {
            Debug.WriteLine("sth else");
            return null;
        }
    }

    public Node? ToStatement(JObject obj)
    {
        if (!obj.ContainsKey("stmt_type"))
        {
            return null;
        }

        string stmt_type = obj["stmt_type"].ToString();
        if (stmt_type == "Block")
        {
            JArray stmts = (JArray)obj.SelectToken("$.stmts", true);
            NodeList<Statement> statements = NodeList.Create(stmts.Select(stmt => (Statement)this.ToStatement((JObject)stmt)));
            return new BlockStatement(statements);
        }
        else if (stmt_type == "Expr")
        {
            return null;
        }
        else if (stmt_type == "Function")
        {
            Identifier ident = new Identifier(obj.SelectToken("$.name.value", true).ToString());
            List<Node> parameters = new List<Node>();
            BlockStatement body = (BlockStatement)this.ToStatement((JObject)obj.SelectToken("$.body", true));

            //var ret = new FunctionDeclaration(ident, NodeList.Create(parameters), body, false, false, false);
            var ret = new FunctionDeclaration(ident, NodeList.Create(parameters), new BlockStatement(NodeList.Create(new List<Statement>())), false, false, false);

            string tmp = ret.ToJavaScriptString();

            return ret;
        }
        else if (stmt_type == "Var")
        {
            Identifier ident = new Identifier(obj.SelectToken("$.name.value", true).ToString());
            Expression expr = this.ToExpression((JObject)obj.SelectToken("$.initializer", true));

            List<VariableDeclarator> declarators = new List<VariableDeclarator>();
            declarators.Add(new VariableDeclarator(ident, expr));
            return new VariableDeclaration(NodeList.Create(declarators), VariableDeclarationKind.Var);
        }
        else if (stmt_type == "Simultaneous")
        {
            return null;
        }
        else if (stmt_type == "Free")
        {
            return null;
        }
        else if (stmt_type == "If")
        {
            return null;
        }
        else if (stmt_type == "Return")
        {
            return null;
        }
        return null;
    }

    public Node? ToToken(JObject obj)
    {
        if (!obj.ContainsKey("token_type"))
        {
            return null;
        }

        string token_type = obj["token_type"].ToString();
        if (token_type == "Identifier")
        {
            return null;
        }
        return null;
    }

    public Expression ToExpression(JObject obj)
    {
        if (!obj.ContainsKey("expr_type"))
        {
            return null;
        }

        string token_type = obj["expr_type"].ToString();
        if (token_type == "Literal")
        {
            return null;
        }
        else if (token_type == "Named")
        {
            string ident = obj.SelectToken("$.name.value", true).ToString();
            return new Identifier(ident);
        }
        else if (token_type == "Unary")
        {
            return null;
        }
        else if (token_type == "Binary")
        {
            return null;
        }
        else if (token_type == "Logical")
        {
            return null;
        }
        else if (token_type == "Assign")
        {
            return null;
        }
        else if (token_type == "Call")
        {
            Expression callee = this.ToExpression((JObject)obj.SelectToken("$.call", true));
            JArray args = (JArray)obj.SelectToken("$.args", true);
            List<Expression> args_list = args.Select(arg => this.ToExpression((JObject)arg)).ToList();
            return new CallExpression(callee, NodeList.Create(args_list), false);
        }
        else if (token_type == "Grouping")
        {
            return null;
        }
        return null;
    }

    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, MistContainer? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class MistDecompiler
{
    public string Decompile(string input)
    {
        var text = File.ReadAllText(input);

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new MistContainerConverter());

        MistContainer container = JsonConvert.DeserializeObject<MistContainer>(text, settings);

        return container.Programs[0].ToJavaScriptString();
    }
}