using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public class StatementDecompiler
{
    public static Statement Decompile(JObject statementObject)
    {
        if (!statementObject.ContainsKey("stmt_type"))
            throw new Exception("Unexpected Statement type");

        var statementType = statementObject["stmt_type"]!.ToString();
        switch (statementType)
        {
            case "Block":
                return DecompileBlockStatement(statementObject);
            case "Expr":
                return DecompileExprStatement(statementObject);
            case "Function":
            {
                Identifier ident = new Identifier(statementObject.SelectToken("$.name.value", true).ToString());

                JArray params_ = (JArray)statementObject.SelectToken("$.params", true);
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
                            MistConverter.ToExpression(defaultValueObject));
                    }

                    return new Identifier(paramObject["value"]!.ToString());
                }));

                BlockStatement body = (BlockStatement)Decompile((JObject)statementObject.SelectToken("$.body", true));
                if (statementObject["resolve"] is JObject resolve)
                {
                    var resolveBody = Decompile(resolve);

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
                return DecompileVarStatement(statementObject);
            case "Simultaneous":
            {
                // Returns function `__async(() => { statementN; })`.
                JObject body = (JObject)statementObject.SelectToken("$.body");
                Node bodyStatement = Decompile(body);
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
                JObject stmt = (JObject)statementObject.SelectToken("$.stmt");

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
                    var expr = (ExpressionStatement)Decompile(stmt);
                    arrow_func = new ArrowFunctionExpression(NodeList.Create(arrow_args_list), expr.Expression,
                        true,
                        false, false);
                }
                else
                {
                    var stmts = NodeList.Create(new List<Statement> { Decompile(stmt) });
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
                JObject condition = (JObject)statementObject.SelectToken("$.condition");
                JObject then_branch = (JObject)statementObject.SelectToken("$.then_branch");

                var condition_expr = MistConverter.ToExpression(condition);
                var then_branch_stmt = Decompile(then_branch);

                if (statementObject["else_branch"] is null ||
                    (statementObject["else_branch"] is JValue elseValue && elseValue.Value!.ToString() == "null"))
                {
                    return new IfStatement(condition_expr, then_branch_stmt, null);
                }

                JObject else_branch = (JObject)statementObject.SelectToken("$.else_branch");
                var else_branch_stmt = Decompile(else_branch);

                return new IfStatement(condition_expr, then_branch_stmt, else_branch_stmt);
            }
            case "Return":
            {
                if (statementObject["value"] is JValue rawValue && rawValue.ToString() == "null")
                    return new ReturnStatement(null);

                if (statementObject["value"] is not JObject valueObject)
                    throw new Exception($"Unexpected return value type: {statementObject["value"].Type}");

                return new ReturnStatement(MistConverter.ToExpression(valueObject));
            }
            case "Resolve":
            {
                if (statementObject["stmts"] is not JObject statementsObject)
                    throw new Exception("Resolve statement must come with child statements");

                var statements = NodeList.Create(new List<Statement> { Decompile(statementsObject) });

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
                throw new Exception($"Unexpected statement type: {statementType}");
        }
    }

    private static Statement DecompileBlockStatement(JObject blockObject)
    {
        if (!blockObject.ContainsKey("stmts"))
            throw new Exception("Block Statement has no statements");

        if (blockObject["stmts"] is not JArray statements)
            throw new Exception("Block Statement has an unexpected stmts value");

        return new BlockStatement(NodeList.Create(statements.Select(statement =>
        {
            if (statement is not JObject statementObject)
                throw new Exception("Unexpected block statement type");

            return Decompile(statementObject);
        })));
    }

    private static Statement DecompileExprStatement(JObject statementObject)
    {
        if (!statementObject.ContainsKey("expr") || statementObject["expr"] is not JObject exprObject)
            throw new Exception("Expression statement has no expr");

        return new ExpressionStatement(MistConverter.ToExpression(exprObject));
    }

    private static Statement DecompileFunctionStatement(JObject statementObject)
    {
        throw new NotImplementedException();
    }

    private static Statement DecompileVarStatement(JObject statementObject)
    {
        if (statementObject["name"] is not JObject || statementObject["name"]["value"] is not JValue name)
            throw new Exception("Var Statement has no name");
        
        
        var initialiserObject = statementObject["initializer"];
        if (initialiserObject is not null && initialiserObject is not JObject)
            throw new Exception("Var has an unexpected initialiser");

        
        var initialiser = MistConverter.ToExpression(initialiserObject as JObject);

        return new VariableDeclaration(
            NodeList.Create(new List<VariableDeclarator>
            {
                new VariableDeclarator(new Identifier(name.ToString()), initialiser)
            }),
            VariableDeclarationKind.Var
        );
    }
}