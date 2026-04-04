using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public static class StatementDecompiler
{
    public static Statement Decompile(JObject statementObject)
    {
        if (statementObject.SelectToken("$.stmt_type") is not JValue statementType)
            throw new Exception("Statement has no statement type");

        return $"{statementType}" switch
        {
            "Block" => DecompileBlockStatement(statementObject),
            "Expr" => DecompileExprStatement(statementObject),
            "Function" => DecompileFunctionStatement(statementObject),
            "Var" => DecompileVarStatement(statementObject),
            "If" => DecompileIfStatement(statementObject),
            "Simultaneous" => DecompileSimultaneousStatement(statementObject),
            "Free" => DecompileFreeStatement(statementObject),
            "Return" => DecompileReturnStatement(statementObject),
            "Resolve" => DecompileResolveStatement(statementObject),
            _ => throw new Exception($"Unexpected statement type: {statementType}")
        };
    }

    private static Statement DecompileBlockStatement(JObject blockObject)
    {
        if (blockObject.SelectToken("$.stmts") is not JArray statements)
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
        if (statementObject.SelectToken("$.expr") is not JObject expression)
            throw new Exception("Expression statement has no expression");

        return new ExpressionStatement(MistConverter.ToExpression(expression));
    }

    private static Statement DecompileFunctionStatement(JObject statementObject)
    {
        if (statementObject.SelectToken("$.name.value") is not JValue name)
            throw new Exception("Function Statement has no name");

        if (statementObject.SelectToken("$.params") is not JArray parameterArray)
            throw new Exception("Function Statement has no parameters");

        if (statementObject.SelectToken("$.body") is not JObject bodyObject)
            throw new Exception("Function Statement has no body");

        var parameters = NodeList.Create(parameterArray.Select<JToken, Node>(param =>
        {
            if (param is not JObject paramObject ||
                !paramObject.ContainsKey("token_type") ||
                paramObject["token_type"]!.ToString() != "Identifier" ||
                paramObject.SelectToken("$.value") is not JValue parameterName)
            {
                throw new Exception("Expected function parameter");
            }

            if (paramObject.ContainsKey("default_value") && paramObject["default_value"]!.ToString() != "null")
            {
                if (paramObject.SelectToken("$.default_value") is not JObject defaultValueObject)
                    throw new Exception("Unexpected parameter default value");

                return new AssignmentPattern(
                    new Identifier($"{parameterName}"),
                    MistConverter.ToExpression(defaultValueObject)
                );
            }

            return new Identifier($"{parameterName}");
        }));

        if (Decompile(bodyObject) is not BlockStatement body)
            throw new Exception("Function body is not a BlockStatement");

        if (statementObject["resolve"] is JObject resolve)
        {
            var resolveCall = new ReturnStatement(
                new CallExpression(
                    new Identifier("__resolve"),
                    NodeList.Create(new List<Expression>
                    {
                        new ArrowFunctionExpression(
                            new NodeList<Node>(),
                            Decompile(resolve),
                            false,
                            false,
                            false
                        )
                    }),
                    false
                )
            );

            body = new BlockStatement(NodeList.Create(body.Body.Append(resolveCall).ToList()));
        }

        return new FunctionDeclaration(
            new Identifier($"{name}"),
            parameters,
            body,
            false,
            false,
            false
        );
    }

    private static Statement DecompileVarStatement(JObject statementObject)
    {
        if (statementObject.SelectToken("$.name.value") is not JValue name)
            throw new Exception("Var Statement has no name");
        
        var initialiserObject = statementObject["initializer"];
        if (initialiserObject is not null && initialiserObject is not JObject)
            throw new Exception("Var has an unexpected initialiser");
        
        var initialiser = MistConverter.ToExpression(initialiserObject as JObject);

        return new VariableDeclaration(
            NodeList.Create(new List<VariableDeclarator>
            {
                new VariableDeclarator(new Identifier($"{name}"), initialiser)
            }),
            VariableDeclarationKind.Var
        );
    }

    private static Statement DecompileIfStatement(JObject statementObject)
    {
        if (statementObject.SelectToken("$.condition") is not JObject condition)
            throw new Exception("If Statement has no condition");

        if (statementObject.SelectToken("$.then_branch") is not JObject thenBranch)
            throw new Exception("If Statement has then branch");

        var conditionExpression = MistConverter.ToExpression(condition);
        var thenBranchStatement = Decompile(thenBranch);

        if (statementObject["else_branch"] is null ||
            (statementObject["else_branch"] is JValue elseValue && elseValue.Value!.ToString() == "null"))
        {
            return new IfStatement(conditionExpression, thenBranchStatement, null);
        }

        if (statementObject.SelectToken("$.else_branch") is not JObject elseBranch)
            throw new Exception("If Statement has else branch");

        return new IfStatement(conditionExpression, thenBranchStatement, Decompile(elseBranch));
    }

    private static Statement DecompileSimultaneousStatement(JObject statementObject)
    {
        if (statementObject.SelectToken("$.body") is not JObject body)
            throw new Exception("Simultaneous Statement has no body");

        Node bodyStatement = Decompile(body);
        if (bodyStatement is not BlockStatement bodyBlock || bodyBlock.Body.Count == 0)
        {
            throw new Exception("Unexpected simultaneous body");
        }

        var arguments = bodyBlock.Body.Select<Statement, Expression>(arrowBody =>
        {
            var statements = NodeList.Create(new List<Statement> { arrowBody });

            StatementListItem arrowFuncBody = statements switch
            {
                [BlockStatement arrowFuncBlockStatement] => arrowFuncBlockStatement,
                [ExpressionStatement { Expression: { } arrowFuncExpression }] => arrowFuncExpression,
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

        return new ExpressionStatement(new CallExpression(
            new Identifier("__async"),
            NodeList.Create(arguments),
            false
        ));
    }

    private static Statement DecompileFreeStatement(JObject statementObject)
    {
        // Returns function `__free(() => { statementN; })`
        if (statementObject.SelectToken("$.stmt") is not JObject freeStatementObject)
            throw new Exception("Free Statement has no stmt");

        var freeStatements = NodeList.Create(new List<Statement> { Decompile(freeStatementObject) });
        StatementListItem body = freeStatements switch
        {
            [BlockStatement arrowFuncBlockStatement] => arrowFuncBlockStatement,
            [ExpressionStatement { Expression: Expression arrowFuncExpression }] => arrowFuncExpression,
            [IfStatement] => new BlockStatement(freeStatements),
            _ => throw new Exception("Unexpected arrow body")
        };

        var arrowFunctionExpression = new ArrowFunctionExpression(
            new NodeList<Node>(),
            body,
            true,
            false,
            false
        );

        return new ExpressionStatement(new CallExpression(
            new Identifier("__free"),
            NodeList.Create(new List<Expression> { arrowFunctionExpression }),
            false
        ));
    }

    private static Statement DecompileReturnStatement(JObject statementObject)
    {
        if (statementObject["value"] is JValue rawValue && $"{rawValue}" == "null")
            return new ReturnStatement(null);

        if (statementObject["value"] is not JObject valueObject)
            throw new Exception($"Unexpected return value type: {statementObject["value"].Type}");

        return new ReturnStatement(MistConverter.ToExpression(valueObject));
    }

    private static Statement DecompileResolveStatement(JObject statementObject)
    {
        if (statementObject.SelectToken("$.stmts") is not JObject statementsObject)
            throw new Exception("Resolve statement must come with child statements");

        var statements = NodeList.Create(new List<Statement> { Decompile(statementsObject) });

        StatementListItem arrowFuncBody = statements switch
        {
            [BlockStatement arrowFuncBlockStatement] => arrowFuncBlockStatement,
            [ExpressionStatement { Expression: { } arrowFuncExpression }] => arrowFuncExpression,
            [IfStatement] => new BlockStatement(statements),
            _ => throw new Exception("Unexpected arrow body")
        };

        var arrowFunctionExpression = new ArrowFunctionExpression(
            new NodeList<Node>(),
            arrowFuncBody,
            false,
            false,
            false
        );

        return new ReturnStatement(
            new CallExpression(
                new Identifier("__resolve"),
                NodeList.Create(new List<Expression> { arrowFunctionExpression }),
                false
            )
        );
    }
}