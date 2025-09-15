using Esprima;
using Esprima.Ast;
using Esprima.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

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
            JObject expr = (JObject)obj.SelectToken("$.expr", true);
            return new ExpressionStatement((Expression)this.ToExpression(expr));
        }
        else if (stmt_type == "Function")
        {
            Identifier ident = new Identifier(obj.SelectToken("$.name.value", true).ToString());
            List<Node> parameters = new List<Node>();
            BlockStatement body = (BlockStatement)this.ToStatement((JObject)obj.SelectToken("$.body", true));

            //var ret = new FunctionDeclaration(ident, NodeList.Create(parameters), body, false, false, false);
            var ret = new FunctionDeclaration(ident, NodeList.Create(parameters), body, false, false, false);

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
            // Returns function `__async()`;
            return null;
        }
        else if (stmt_type == "Free")
        {
            // Returns function `__free()`;
            return null;
        }
        else if (stmt_type == "If")
        {
            JObject condition = (JObject)obj.SelectToken("$.condition");
            JObject then_branch = (JObject)obj.SelectToken("$.then_branch");
            JObject else_branch = (JObject)obj.SelectToken("$.else_branch");

            var condition_expr = this.ToExpression(condition);
            var then_branch_stmt = (Statement)this.ToStatement(then_branch);
            var else_branch_stmt = (Statement)this.ToStatement(else_branch);

            return new IfStatement(condition_expr, then_branch_stmt, else_branch_stmt);
        }
        else if (stmt_type == "Return")
        {
            JObject expr = (JObject)obj.SelectToken("$.value");
            return new ReturnStatement(this.ToExpression(expr));
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

        string expr_type = obj["expr_type"].ToString();
        if (expr_type == "Literal")
        {
            string token_type = obj.SelectToken("$.value.token_type", true).ToString();

            if (token_type == "True")
            {
                return new Literal(true, "true");
            }
            else if (token_type == "False")
            {
                return new Literal(false, "false");
            }
            else if (token_type == "Number")
            {
                var value = obj.SelectToken("$.value.Value", true);
                return new Literal((double)value, value.ToString());
            }
            else if (token_type == "String")
            {
                var value = obj.SelectToken("$.value.value", true);
                return new Literal(value.ToString(), value.ToString());
            } else
            {
                throw new Exception($"unknown token type for a Literal Expression: {token_type}");
            }
        }
        else if (expr_type == "Named")
        {
            string ident = obj.SelectToken("$.name.value", true).ToString();
            return new Identifier(ident);
        }
        else if (expr_type == "Unary")
        {
            return null;
        }
        else if (expr_type == "Binary")
        {
            JObject left = (JObject)obj.SelectToken("$.left");
            string operator_name = obj.SelectToken("$.operator.token_type", true).ToString();
            JObject right = (JObject)obj.SelectToken("$.right");

            BinaryOperator op;
            switch (operator_name)
            {
                case "DoubleEqual":
                    op = BinaryOperator.Equal; break;
                case "Plus":
                    op = BinaryOperator.Plus; break;
                default:
                    op = BinaryOperator.Greater; break;
            }

            var left_expr = this.ToExpression(left);
            var right_expr = this.ToExpression(right);
            return new BinaryExpression(op, left_expr, right_expr);
        }
        else if (expr_type == "Logical")
        {
            return null;
        }
        else if (expr_type == "Assign")
        {
            return null;
        }
        else if (expr_type == "Call")
        {
            Expression callee = this.ToExpression((JObject)obj.SelectToken("$.call", true));
            JArray args = (JArray)obj.SelectToken("$.args", true);
            List<Expression> args_list = args.Select(arg => this.ToExpression((JObject)arg)).ToList();
            return new CallExpression(callee, NodeList.Create(args_list), false);
        }
        else if (expr_type == "Grouping")
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

        // FIXME: Output all programs.
        return container.Programs[0].ToJavaScriptString();
    }
}