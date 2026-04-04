using Esprima.Ast;
using Esprima.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using JsonWriter = Newtonsoft.Json.JsonWriter;

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

public class MistContainerConverter : JsonConverter<Module>
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

    public Statement ToStatement(JObject obj)
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
        switch (stmt_type)
        {
            case "Block":
            {
                JArray stmts = (JArray)obj.SelectToken("$.stmts", true);
                NodeList<Statement> statements =
                    NodeList.Create(stmts.Select(stmt => (Statement)this.ToStatement((JObject)stmt)));
                return new BlockStatement(statements);
            }
            case "Expr":
            {
                JObject expr = (JObject)obj.SelectToken("$.expr", true);
                return new ExpressionStatement(ToExpression(expr));
            }
            case "Function":
            {
                Identifier ident = new Identifier(obj.SelectToken("$.name.value", true).ToString());

                JArray params_ = (JArray)obj.SelectToken("$.params", true);
                NodeList<Node> parameters = NodeList.Create<Node>(params_.Select<JToken, Node>(param =>
                {
                    if (param is not JObject paramObject ||
                        !paramObject.ContainsKey("token_type") ||
                        paramObject["token_type"]!.ToString() != "Identifier" ||
                        !paramObject.ContainsKey("value"))
                    {
                        throw new Exception($"Expected function parameter");
                    }

                    if (paramObject.ContainsKey("default_value") && paramObject["default_value"]!.ToString() != "null")
                    {
                        if (paramObject["default_value"] is not JObject defaultValueObject)
                            throw new Exception("Unexpected parameter default value");

                        return new AssignmentPattern(new Identifier(paramObject["value"]!.ToString()),
                            ToExpression(defaultValueObject));
                    }

                    return new Identifier(paramObject["value"]!.ToString());
                }));
                
                BlockStatement body = (BlockStatement) ToStatement((JObject)obj.SelectToken("$.body", true));
                if (obj["resolve"] is JObject resolve)
                {
                    var resolveBody = ToStatement(resolve);

                    var arrow_func = new ArrowFunctionExpression(
                        new NodeList<Node>(),
                        resolveBody,
                        false,
                        false,
                        false
                    );
                    
                    var resolveCall = new ReturnStatement(
                        new CallExpression(
                            new Identifier("__resolve"), 
                            NodeList.Create(new List<Expression> { arrow_func }),
                            false
                        )
                    );

                    body = new BlockStatement(NodeList.Create(body.Body.Append(resolveCall).ToList()));
                }
                
                return new FunctionDeclaration(ident, parameters, body, false, false, false);
            }
            case "Var":
            {
                Identifier ident = new Identifier(obj.SelectToken("$.name.value", true).ToString());
                Expression expr = this.ToExpression((JObject)obj.SelectToken("$.initializer", true));

                List<VariableDeclarator> declarators = new List<VariableDeclarator>();
                declarators.Add(new VariableDeclarator(ident, expr));
                return new VariableDeclaration(NodeList.Create(declarators), VariableDeclarationKind.Var);
            }
            case "Simultaneous":
            {
                // Returns function `__async(() => { statementN; })`.
                JObject body = (JObject)obj.SelectToken("$.body");
                Node bodyStatement = ToStatement(body);
                if (bodyStatement is not BlockStatement bodyBlock || bodyBlock.Body.Count == 0)
                {
                    throw new Exception("Unexpected simultaneous body");
                }

                List<Expression> args = bodyBlock.Body.Select<Statement, Expression>(arrowBody =>
                {
                    var statements = NodeList.Create(new List<Statement> { arrowBody });

                    StatementListItem arrowFuncBody = statements switch
                    {
                        [BlockStatement arrowFuncBlockStatement] => arrowFuncBlockStatement,
                        [ExpressionStatement { Expression: Expression arrowFuncExpression }] => arrowFuncExpression,
                        [IfStatement] => new BlockStatement(statements),
                        _ => throw new Exception("Unexpected arrow body")
                    };
                    
                    return new ArrowFunctionExpression(
                        new NodeList<Node>(),
                        arrowFuncBody,
                        true,
                        false,
                        false
                    );
                }).ToList();

                return new ExpressionStatement(new CallExpression(new Identifier("__async"), NodeList.Create(args),
                    false));
            }
            case "Free":
            {
                // Returns function `__free(() => { statementN; })`
                // A function call to __free with an arrow function as argument.
                JObject stmt = (JObject)obj.SelectToken("$.stmt");

                var func_name = new Identifier("__free");
                List<Node> arrow_args_list = new();

                ArrowFunctionExpression arrow_func;
                if (!stmt.ContainsKey("stmt_type"))
                {
                    throw new Exception("unexpected token type");
                }

                var typ = stmt.Value<string>("stmt_type") ?? "";
                if (typ == "Expr")
                {
                    // Unpack the inner expression since Esprima will try to convert the body of
                    // ArrowFunctionExpression to Expression if any Body is not BlockStatement.
                    var expr = (ExpressionStatement)ToStatement(stmt);
                    arrow_func = new ArrowFunctionExpression(NodeList.Create(arrow_args_list), expr.Expression,
                        true,
                        false, false);
                }
                else
                {
                    var stmts = NodeList.Create( new List<Statement> { ToStatement(stmt) });
                    StatementListItem body = stmts switch
                    {
                        [BlockStatement arrowFuncBlockStatement] => arrowFuncBlockStatement,
                        [ExpressionStatement { Expression: Expression arrowFuncExpression }] => arrowFuncExpression,
                        [IfStatement] => new BlockStatement(stmts),
                        _ => throw new Exception("Unexpected arrow body")
                    };

                    arrow_func = new ArrowFunctionExpression(
                        new NodeList<Node>(),
                        body,
                        true,
                        false,
                        false
                    );
                }

                List<Expression> args = new();
                args.Add(arrow_func);

                // Wrap in ExpressionStatement because Free itself is a statement.
                return new ExpressionStatement(new CallExpression(func_name, NodeList.Create(args), false));
            }
            case "If":
            {
                JObject condition = (JObject)obj.SelectToken("$.condition");
                JObject then_branch = (JObject)obj.SelectToken("$.then_branch");

                var condition_expr = ToExpression(condition);
                var then_branch_stmt = ToStatement(then_branch);

                if (obj["else_branch"] is null ||
                    (obj["else_branch"] is JValue elseValue && elseValue.Value!.ToString() == "null"))
                {
                    return new IfStatement(condition_expr, then_branch_stmt, null);
                }

                JObject else_branch = (JObject)obj.SelectToken("$.else_branch");
                var else_branch_stmt = ToStatement(else_branch);

                return new IfStatement(condition_expr, then_branch_stmt, else_branch_stmt);
        }
            case "Return":
            {
                if (obj["value"] is JValue rawValue && rawValue.ToString() == "null")
                    return new ReturnStatement(null);

                if (obj["value"] is not JObject valueObject)
                    throw new Exception($"Unexpected return value type: {obj["value"].Type}");

                return new ReturnStatement(ToExpression(valueObject));
            }
            case "Resolve":
            {
                if (obj["stmts"] is not JObject statementsObject)
                    throw new Exception("Resolve statement must come with child statements");
                
                var statements = NodeList.Create(new List<Statement> { ToStatement(statementsObject) });

                StatementListItem arrowFuncBody = statements switch
                {
                    [BlockStatement arrowFuncBlockStatement] => arrowFuncBlockStatement,
                    [ExpressionStatement { Expression: Expression arrowFuncExpression }] => arrowFuncExpression,
                    [IfStatement] => new BlockStatement(statements),
                    _ => throw new Exception("Unexpected arrow body")
                };
                
                var arrow_func = new ArrowFunctionExpression(
                    new NodeList<Node>(),
                    arrowFuncBody,
                    false,
                    false,
                    false
                );
                
                return new ReturnStatement(
                    new CallExpression(
                        new Identifier("__resolve"), 
                        NodeList.Create(new List<Expression> { arrow_func }),
                        false
                    )
                );
            }
            default:
                throw new Exception($"Unexpected statement type: {stmt_type}");
        }
    }

    public Expression ToExpression(JObject obj)
    {
        if (!obj.ContainsKey("expr_type"))
        {
            if (obj.ContainsKey("stmt_type"))
            {
                throw new Exception("expected an expression but found a statement.");
            }

            if (obj.ContainsKey("token_type"))
            {
                throw new Exception("expected an expression but found a token.");
            }

            throw new Exception("expected a statement but found JSON objectg without 'expr_type' key");
        }

        string expr_type = obj["expr_type"].ToString();
        switch (expr_type)
        {
            case "Literal":
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
                    // Esprima will use the raw to render the String literal, so we add back the double quotes.
                    return new Literal(value.ToString(), $"\"{value.ToString()}\"");
                }
                else
                {
                    throw new Exception($"unknown token type for a Literal Expression: {token_type}");
                }
            }
            case "Named":
            {
                string ident = obj.SelectToken("$.name.value", true).ToString();
                return new Identifier(ident);
            }
            case "Unary":
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
            case "Binary":
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
            case "Logical":
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
            case "Assign":
            {
                JObject name = (JObject)obj.SelectToken("$.name");
                JObject value = (JObject)obj.SelectToken("$.value");
                var name_expr = this.ToExpression(name);
                var value_expr = this.ToExpression(value);
                return new AssignmentExpression(AssignmentOperator.Assign, name_expr, value_expr);
            }
            case "Call":
            {
                Expression callee = this.ToExpression((JObject)obj.SelectToken("$.call", true));
                JArray args = (JArray)obj.SelectToken("$.args", true);
                List<Expression> args_list = args.Select(arg => this.ToExpression((JObject)arg)).ToList();
                return new CallExpression(callee, NodeList.Create(args_list), false);
            }
            case "Grouping":
            {
                JObject expr = (JObject)obj.SelectToken("$.expr");
                // Esprima does not have explicit grouping in its AST, however, it appears to render it out properly.

                return new CallExpression(
                    new Identifier("__group"),
                    NodeList.Create(new List<Expression>() { ToExpression(expr) }),
                     false
                );
            }
            default:
                return null;
        }
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
        settings.Converters.Add(new MistContainerConverter());

        var container = JsonConvert.DeserializeObject<Module>(mist, settings);
        if (container is null)
            throw new Exception("Unable to convert Mist File");

        return container.ToJavaScriptString(true).Replace("throw", "throwFunc");
    }

    public string DecompileFile(string input)
    {
        var text = File.ReadAllText(input);

        return Decompile(text);
    }
}