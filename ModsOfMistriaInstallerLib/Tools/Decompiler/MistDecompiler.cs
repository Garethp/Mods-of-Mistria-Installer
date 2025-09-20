using Esprima.Ast;
using Esprima.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

/// <summary>
/// A container representing the top level JSON object in __mist__.json.
/// </summary>
public class MistContainer
{
    public List<MistProgram> Programs;
}

public class MistProgram
{
    public string Name;
    // Esprima's Program is an abstract partial class.
    public Module Program;

    public string ToJavaScriptString()
    {
        // We could directly use Program.ToJavaScriptString(true)
        // if we want a writer to use OS dependent newline.
        StringBuilder sb = new StringBuilder();
        using (var writer = new StringWriter(sb))
        {
            //writer.NewLine = "\n";
            Program.WriteJavaScript(writer, true);
        }
        return sb.ToString();
    }
}

public class MistContainerConverter : JsonConverter<MistContainer>
{
    public override MistContainer? ReadJson(JsonReader reader, Type objectType, MistContainer? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // This, to my understanding, load the entire JSON tree.
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
                mist.Program = new Module(NodeList.Create(statements.Select(stmt => (Statement)this.ToStatement((JObject)stmt))));

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
            if (obj.ContainsKey("expr_type"))
            {
                throw new Exception("expected a statement but found an expression.");
            }
            else if (obj.ContainsKey("token_type"))
            {
                throw new Exception("expected a statement but found a token.");
            }
            throw new Exception("expected a statement but found JSON objectg without 'stmt_type' key");
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
            return new FunctionDeclaration(ident, NodeList.Create(parameters), body, false, false, false);
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
            // Returns function `__async(() => { statementN; })`.
            JObject body = (JObject)obj.SelectToken("$.body");

            var func_name = new Identifier("__async");
            List<Node> arrow_args_list = new();
            var arrow_func = new ArrowFunctionExpression(NodeList.Create(arrow_args_list), (StatementListItem)this.ToStatement(body), false, false, false);
            List<Expression> args = new();
            args.Add(arrow_func);

            // Wrap in ExpressionStatement because Simultaneous itself is a statement.
            return new ExpressionStatement((Expression)new CallExpression(func_name, NodeList.Create(args), false));
        }
        else if (stmt_type == "Free")
        {
            // Returns function `__free(() => { statementN; })`
            // A function call to __free with an arrow function as argument.
            JObject stmt = (JObject)obj.SelectToken("$.stmt");

            var func_name = new Identifier("__free");
            List<Node> arrow_args_list = new();

            ArrowFunctionExpression arrow_func;
            if (stmt.ContainsKey("stmt_type"))
            {
                var typ = stmt.Value<string>("stmt_type") ?? "";
                if (typ == "Expr")
                {
                    var expr = (ExpressionStatement)this.ToStatement(stmt);
                    arrow_func = new ArrowFunctionExpression(NodeList.Create(arrow_args_list), expr.Expression, true, false, false);
                } else
                {
                    Statement[] stmts = { (Statement)this.ToStatement(stmt) };
                    NodeList<Statement> statements = NodeList.Create(stmts);
                    arrow_func = new ArrowFunctionExpression(NodeList.Create(arrow_args_list), new BlockStatement(statements), false, false, false);
                }
            }
            else
            {
                throw new Exception("unexpected token type");
            }


            List<Expression> args = new();
            args.Add(arrow_func);

            // Wrap in ExpressionStatement because Free itself is a statement.
            return new ExpressionStatement((Expression)new CallExpression(func_name, NodeList.Create(args), false));
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

    public Expression ToExpression(JObject obj)
    {
        if (!obj.ContainsKey("expr_type"))
        {
            if (obj.ContainsKey("stmt_type"))
            {
                throw new Exception("expected an expression but found a statement.");
            }
            else if (obj.ContainsKey("token_type"))
            {
                throw new Exception("expected an expression but found a token.");
            }
            throw new Exception("expected a statement but found JSON objectg without 'expr_type' key");
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
            }
            else
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
            string operator_name = obj.SelectToken("$.operator.token_type", true).ToString();
            JObject right = (JObject)obj.SelectToken("$.right");

            UnaryOperator op;
            switch (operator_name)
            {
                case "Minus":
                    op = UnaryOperator.Minus; break;
                case "Bang":
                    op = UnaryOperator.LogicalNot; break;
                default:
                    throw new Exception($"unknown binary operator: {operator_name}");
            }

            var right_expr = this.ToExpression(right);
            return new UnaryExpression(op, right_expr);
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
                case "BangEqual":
                    op = BinaryOperator.NotEqual; break;
                case "LessEqual":
                    op = BinaryOperator.LessOrEqual; break;
                case "Less":
                    op = BinaryOperator.Less; break;
                case "GreaterEqual":
                    op = BinaryOperator.GreaterOrEqual; break;
                case "Greater":
                    op = BinaryOperator.Greater; break;
                case "Plus":
                    op = BinaryOperator.Plus; break;
                case "Minus":
                    op = BinaryOperator.Minus; break;
                case "Star":
                    op = BinaryOperator.Times; break;
                case "Slash":
                    op = BinaryOperator.Divide; break;
                default:
                    throw new Exception($"unknown binary operator: {operator_name}");
            }

            var left_expr = this.ToExpression(left);
            var right_expr = this.ToExpression(right);
            return new BinaryExpression(op, left_expr, right_expr);
        }
        else if (expr_type == "Logical")
        {
            JObject left = (JObject)obj.SelectToken("$.left");
            string operator_name = obj.SelectToken("$.operator.token_type", true).ToString();
            JObject right = (JObject)obj.SelectToken("$.right");

            BinaryOperator op;
            switch (operator_name)
            {
                case "And":
                    op = BinaryOperator.LogicalAnd; break;
                case "Or":
                    op = BinaryOperator.LogicalOr; break;
                default:
                    throw new Exception($"unknown logical operator: {operator_name}");
            }

            var left_expr = this.ToExpression(left);
            var right_expr = this.ToExpression(right);
            return new LogicalExpression(op, left_expr, right_expr);
        }
        else if (expr_type == "Assign")
        {
            JObject name = (JObject)obj.SelectToken("$.name");
            JObject value = (JObject)obj.SelectToken("$.value");
            var name_expr = this.ToExpression(name);
            var value_expr = this.ToExpression(value);
            return new AssignmentExpression(AssignmentOperator.Assign, name_expr, value_expr);
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
            JObject expr = (JObject)obj.SelectToken("$.expr");
            // Esprima does not have explicit grouping in its AST, however, it appears to render it out properly.
            return this.ToExpression(expr);
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