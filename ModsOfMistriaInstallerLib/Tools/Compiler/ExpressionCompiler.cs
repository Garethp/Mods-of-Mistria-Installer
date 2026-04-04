using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public static class ExpressionCompiler
{
    public static JObject Compile(Expression expression)
    {
        return expression switch
        {
            Literal literal => CompileLiteral(literal),
            Identifier identifier => CompileIdentifier(identifier),
            LogicalExpression logicalExpression => CompileLogicalExpression(logicalExpression),
            BinaryExpression binaryExpression => CompileBinaryExpression(binaryExpression),
            UnaryExpression unaryExpression => CompileUnaryExpression(unaryExpression),
            AssignmentExpression assignmentExpression => CompileAssignmentExpression(assignmentExpression),
            CallExpression callExpression => CompileCallExpression(callExpression),
            _ => throw new NotImplementedException()
        };
    }

    private static JObject CompileLiteral(Literal literal)
    {
        return literal.TokenType.ToString() switch
        {
            "NumericLiteral" => CompileLiteralNumber(literal),
            "StringLiteral" => CompileLiteralString(literal),
            "BooleanLiteral" => CompileLiteralBoolean(literal),
            _ => throw new NotImplementedException()
        };
    }

    private static JObject CompileLiteralString(Literal literal)
    {
        return new JObject
        {
            { "expr_type", "Literal" },
            {
                "value", new JObject
                {
                    { "token_type", "String" },
                    { "value", literal.Value!.ToString() }
                }
            }
        };
    }

    private static JObject CompileLiteralNumber(Literal number)
    {
        return new JObject
        {
            { "expr_type", "Literal" },
            {
                "value", new JObject
                {
                    { "token_type", "Number" },
                    { "Value", new JValue(number.Value) }
                }
            }
        };
    }

    private static JObject CompileLiteralBoolean(Literal boolean)
    {
        return new JObject
        {
            { "expr_type", "Literal" },
            {
                "value", new JObject
                {
                    { "token_type", (bool) boolean.BooleanValue! ? "True" : "False" }
                }
            }
        };
    }

    private static JObject CompileIdentifier(Identifier identifier)
    {
        var name = identifier.Name == "throwFunc" ? "throw" : identifier.Name;

        return new JObject
        {
            { "expr_type", "Named" },
            {
                "name", new JObject
                {
                    { "token_type", "Identifier" },
                    { "value", name }
                }
            }
        };
    }

    private static JObject CompileBinaryExpression(BinaryExpression binaryExpression)
    {
        if (binaryExpression is LogicalExpression logicalExpression)
            return CompileLogicalExpression(logicalExpression);

        var operatorString = binaryExpression.Operator.ToString() switch
        {
            "Equal" => "DoubleEqual",
            "NotEqual" => "BangEqual",
            "Plus" => "Plus",
            "Minus" => "Minus",
            "Times" => "Star",
            "Divide" => "Slash",
            "Less" => "Less",
            "Greater" => "Greater",
            "LessOrEqual" => "LessEqual",
            "GreaterOrEqual" => "GreaterEqual",
            _ => throw new NotImplementedException($"Binary operator {binaryExpression.Operator} not implemented")
        };

        var right = Compile(binaryExpression.Right);
        if (binaryExpression.Right is BinaryExpression &&
            (binaryExpression.Operator.ToString() == "Times" ||
             binaryExpression.Operator.ToString() == "Divide"))
        {
            right = new JObject
            {
                { "expr_type", "Grouping" },
                { "expr", right }
            };
        }

        return new JObject
        {
            { "expr_type", "Binary" },
            { "left", JsCompiler.CleanCallExpression(Compile(binaryExpression.Left)) },
            {
                "operator", new JObject
                {
                    { "token_type", operatorString }
                }
            },
            { "right", JsCompiler.CleanCallExpression(right) }
        };
    }

    private static JObject CompileUnaryExpression(UnaryExpression unaryExpression)
    {
        var operatorString = unaryExpression.Operator.ToString() switch
        {
            "Minus" => "Minus",
            "LogicalNot" => "Bang",
            _ => throw new NotImplementedException($"Unary operator {unaryExpression.Operator} not implemented")
        };

        return new JObject
        {
            { "expr_type", "Unary" },
            {
                "operator", new JObject
                {
                    { "token_type", operatorString }
                }
            },
            { "right", JsCompiler.CleanCallExpression(Compile(unaryExpression.Argument)) }
        };
    }

    private static JObject CompileLogicalExpression(LogicalExpression logicalExpression)
    {
        var operatorString = logicalExpression.Operator.ToString() switch
        {
            "LogicalAnd" => "And",
            "LogicalOr" => "Or",
            _ => throw new NotImplementedException($"Logical operator {logicalExpression.Operator} not implemented")
        };

        return new JObject
        {
            { "expr_type", "Logical" },
            { "left", JsCompiler.CleanCallExpression(Compile(logicalExpression.Left)) },
            {
                "operator", new JObject
                {
                    { "token_type", operatorString }
                }
            },
            { "right", JsCompiler.CleanCallExpression(Compile(logicalExpression.Right)) }
        };
    }

    private static JObject CompileCallExpression(CallExpression callExpression)
    {
        if (callExpression.Callee is not Identifier callee)
        {
            throw new Exception("Unexpected call token type");
        }

        switch (callee.Name)
        {
            case "__async":
                return CompileSimultaneousExpression(callExpression);
            case "__free":
                return CompileFreeExpression(callExpression);
            case "__group":
                return CompileGroupingExpression(callExpression);
            case "__resolve":
                return CompileResolveExpression(callExpression);
            default:
                if (callee.ChildNodes.Count() != 0)
                    throw new NotImplementedException();

                return new JObject
                {
                    { "stmt_type", "Expr" },
                    {
                        "expr", new JObject
                        {
                            { "expr_type", "Call" },
                            {
                                "call", new JObject
                                {
                                    { "expr_type", "Named" },
                                    {
                                        "name", new JObject
                                        {
                                            { "token_type", "Identifier" },
                                            { "value", callee.Name }
                                        }
                                    }
                                }
                            },
                            {
                                "args",
                                new JArray(callExpression.Arguments.ToArray()
                                    .Select(arg => JsCompiler.CleanCallExpression(Compile(arg))))
                            }
                        }
                    }
                };
        }
    }

    private static JObject CompileResolveExpression(CallExpression callExpression)
    {
        if (callExpression.Arguments.Count != 1)
            throw new NotImplementedException($"Unexpected __resolve argument count: {callExpression.Arguments.Count}");

        if (callExpression.Arguments.First() is not ArrowFunctionExpression arrowFunction)
            throw new NotImplementedException(
                $"Unexpected __resolve argument type: {callExpression.Arguments.First().Type}");

        return new JObject
        {
            { "stmt_type", "Resolve" },
            { "stmts", JsCompiler.Compile(arrowFunction.Body) }
        };
    }

    private static JObject CompileSimultaneousExpression(CallExpression callExpression)
    {
        if (callExpression.Arguments.Count == 0)
            throw new NotImplementedException($"Unexpected __async argument count: {callExpression.Arguments.Count}");

        if (callExpression.Arguments.Any(arg => arg is not ArrowFunctionExpression))
            throw new NotImplementedException("Unexpected __async argument type");

        return new JObject
        {
            { "stmt_type", "Simultaneous" },
            {
                "body", new JObject
                {
                    { "stmt_type", "Block" },
                    {
                        "stmts",
                        new JArray(callExpression.Arguments.Select(arg =>
                        {
                            if (arg is not ArrowFunctionExpression arrowFunction)
                                throw new NotImplementedException("Unexpected __async argument type");
                            
                            return JsCompiler.Compile(arrowFunction.Body);
                        }))
                    }
                }
            }
        };
    }

    private static JObject CompileFreeExpression(CallExpression callExpression)
    {
        if (callExpression.Arguments.Count != 1)
            throw new NotImplementedException($"Unexpected __resolve argument count: {callExpression.Arguments.Count}");

        if (callExpression.Arguments.First() is not ArrowFunctionExpression arrowFunction)
            throw new NotImplementedException(
                $"Unexpected __resolve argument type: {callExpression.Arguments.First().Type}");

        return new JObject
        {
            { "stmt_type", "Free" },
            { "stmt", JsCompiler.Compile(arrowFunction.Body) }
        };
    }

    private static JObject CompileGroupingExpression(CallExpression callExpression)
    {
        return new JObject
        {
            { "expr_type", "Grouping" },
            { "expr", JsCompiler.Compile(callExpression.Arguments.First()) }
        };
    }

    private static JObject CompileAssignmentExpression(AssignmentExpression assignmentExpression)
    {
        if (assignmentExpression.Operator is not AssignmentOperator.Assign)
            throw new NotImplementedException(
                $"Assignment operator is not implemented: {assignmentExpression.Operator}");

        if (assignmentExpression.Left is not Identifier leftIdentifier)
            throw new NotImplementedException($"Assignment left is not implemented: {assignmentExpression.Left}");

        return new JObject
        {
            { "stmt_type", "Expr" },
            {
                "expr", new JObject
                {
                    { "expr_type", "Assign" },
                    {
                        "name", new JObject
                        {
                            { "expr_type", "Named" },
                            {
                                "name", new JObject
                                {
                                    { "token_type", "Identifier" },
                                    { "value", leftIdentifier.Name }
                                }
                            }
                        }
                    },
                    { "value", JsCompiler.CleanCallExpression(Compile(assignmentExpression.Right)) }
                }
            }
        };
    }
}