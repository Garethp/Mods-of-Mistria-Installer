using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public class ExpressionEncoder
{
    public static JObject Encode(Expression expression)
    {
        switch (expression)
        {
            case Literal literal:
                return EncodeLiteral(literal);
            case Identifier identifier:
                return EncodeIdentifier(identifier);
            case LogicalExpression logicalExpression:
                return EncodeLogicalExpression(logicalExpression);
            case BinaryExpression binaryExpression:
                return EncodeBinaryExpression(binaryExpression);
            case UnaryExpression unaryExpression:
                return EncodeUnaryExpression(unaryExpression);
            case AssignmentExpression assignmentExpression:
                return EncodeAssignmentExpression(assignmentExpression);
            case CallExpression callExpression:
                return EncodeCallExpression(callExpression);
        }

        throw new NotImplementedException();
    }

    private static JObject EncodeLiteral(Literal literal)
    {
        switch (literal.TokenType.ToString())
        {
            case "NumericLiteral":
                return EncodeLiteralNumber(literal);
            case "StringLiteral":
                return EncodeLiteralString(literal);
            case "BooleanLiteral":
                return EncodeLiteralBoolean(literal);
        }

        throw new NotImplementedException();
    }

    private static JObject EncodeLiteralString(Literal literal)
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

    private static JObject EncodeLiteralNumber(Literal number)
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

    private static JObject EncodeLiteralBoolean(Literal boolean)
    {
        return new JObject
        {
            { "expr_type", "Literal" },
            {
                "value", new JObject
                {
                    { "token_type", (bool)boolean.BooleanValue ? "True" : "False" },
                }
            }
        };
    }

    private static JObject EncodeIdentifier(Identifier identifier)
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

    private static JObject EncodeBinaryExpression(BinaryExpression binaryExpression)
    {
        if (binaryExpression is LogicalExpression logicalExpression)
            return EncodeLogicalExpression(logicalExpression);

        var operatorString = "";

        switch (binaryExpression.Operator.ToString())
        {
            case "Equal":
                operatorString = "DoubleEqual";
                break;
            case "NotEqual":
                operatorString = "BangEqual";
                break;
            case "Plus":
                operatorString = "Plus";
                break;
            case "Minus":
                operatorString = "Minus";
                break;
            case "Times":
                operatorString = "Star";
                break;
            case "Divide":
                operatorString = "Slash";
                break;
            case "Less":
                operatorString = "Less";
                break;
            case "Greater":
                operatorString = "Greater";
                break;
            case "LessOrEqual":
                operatorString = "LessEqual";
                break;
            case "GreaterOrEqual":
                operatorString = "GreaterEqual";
                break;
            default:
                throw new NotImplementedException($"Binary operator {binaryExpression.Operator} not implemented");
        }

        var right = Encode(binaryExpression.Right);
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
            { "left", Encoder.CleanCallExpression(Encode(binaryExpression.Left)) },
            {
                "operator", new JObject
                {
                    { "token_type", operatorString },
                }
            },
            { "right", Encoder.CleanCallExpression(right) }
        };
    }

    private static JObject EncodeUnaryExpression(UnaryExpression unaryExpression)
    {
        var operatorString = "";

        switch (unaryExpression.Operator.ToString())
        {
            case "Minus":
                operatorString = "Minus";
                break;
            case "LogicalNot":
                operatorString = "Bang";
                break;
            default:
                throw new NotImplementedException($"Unary operator {unaryExpression.Operator} not implemented");
        }

        return new JObject
        {
            { "expr_type", "Unary" },
            {
                "operator", new JObject
                {
                    { "token_type", operatorString }
                }
            },
            { "right", Encoder.CleanCallExpression(Encode(unaryExpression.Argument)) }
        };
    }

    private static JObject EncodeLogicalExpression(LogicalExpression logicalExpression)
    {
        var operatorString = "";

        switch (logicalExpression.Operator.ToString())
        {
            case "LogicalAnd":
                operatorString = "And";
                break;
            case "LogicalOr":
                operatorString = "Or";
                break;
            default:
                throw new NotImplementedException($"Logical operator {logicalExpression.Operator} not implemented");
        }

        return new JObject
        {
            { "expr_type", "Logical" },
            { "left", Encoder.CleanCallExpression(Encode(logicalExpression.Left)) },
            {
                "operator", new JObject
                {
                    { "token_type", operatorString },
                }
            },
            { "right", Encoder.CleanCallExpression(Encode(logicalExpression.Right)) }
        };
    }

    private static JObject EncodeCallExpression(CallExpression callExpression)
    {
        if (callExpression.Callee is not Identifier callee)
        {
            throw new Exception("Unexpected call token type");
        }

        switch (callee.Name)
        {
            case "__async":
                return EncodeSimultaneousExpression(callExpression);
            case "__free":
                return EncodeFreeExpression(callExpression);
            case "__group":
                return EncodeGroupingExpression(callExpression);
            case "__resolve":
                return EncodeResolveExpression(callExpression);
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
                                    .Select(arg => Encoder.CleanCallExpression(Encode(arg))))
                            }
                        }
                    }
                };
        }
    }

    private static JObject EncodeResolveExpression(CallExpression callExpression)
    {
        if (callExpression.Arguments.Count != 1)
            throw new NotImplementedException($"Unexpected __resolve argument count: {callExpression.Arguments.Count}");

        if (callExpression.Arguments.First() is not ArrowFunctionExpression arrowFunction)
            throw new NotImplementedException(
                $"Unexpected __resolve argument type: {callExpression.Arguments.First().Type}");

        return new JObject
        {
            { "stmt_type", "Resolve" },
            { "stmts", Encoder.EncodeJS(arrowFunction.Body) }
        };
    }

    private static JObject EncodeSimultaneousExpression(CallExpression callExpression)
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
                            
                            return Encoder.EncodeJS(arrowFunction.Body);
                        }))
                    },
                }
            }
        };
    }

    private static JObject EncodeFreeExpression(CallExpression callExpression)
    {
        if (callExpression.Arguments.Count != 1)
            throw new NotImplementedException($"Unexpected __resolve argument count: {callExpression.Arguments.Count}");

        if (callExpression.Arguments.First() is not ArrowFunctionExpression arrowFunction)
            throw new NotImplementedException(
                $"Unexpected __resolve argument type: {callExpression.Arguments.First().Type}");

        return new JObject
        {
            { "stmt_type", "Free" },
            { "stmt", Encoder.EncodeJS(arrowFunction.Body) }
        };
    }

    private static JObject EncodeGroupingExpression(CallExpression callExpression)
    {
        return new JObject
        {
            { "expr_type", "Grouping" },
            { "expr", Encoder.EncodeJS(callExpression.Arguments.First()) }
        };
    }

    private static JObject EncodeAssignmentExpression(AssignmentExpression assignmentExpression)
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
                    { "value", Encoder.CleanCallExpression(Encode(assignmentExpression.Right)) }
                }
            }
        };
    }
}