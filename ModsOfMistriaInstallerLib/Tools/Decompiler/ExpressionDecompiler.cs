using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public class ExpressionDecompiler
{
    public static Expression Decompile(JObject expressionObject)
    {
        if (!expressionObject.ContainsKey("expr_type"))
            throw new Exception($"Unexpected expression");
        
        var expressionType = expressionObject["expr_type"]!.ToString();
        switch (expressionType)
        {
            case "Literal":
            {
                string token_type = expressionObject.SelectToken("$.value.token_type", true).ToString();

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
                    var value = expressionObject.SelectToken("$.value.Value", true);
                    return new Literal((double)value, value.ToString());
                }
                else if (token_type == "String")
                {
                    var value = expressionObject.SelectToken("$.value.value", true);
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
                string ident = expressionObject.SelectToken("$.name.value", true).ToString();
                return new Identifier(ident);
            }
            case "Unary":
            {
                string operator_name = expressionObject.SelectToken("$.operator.token_type", true).ToString();
                JObject right = (JObject)expressionObject.SelectToken("$.right");

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

                var right_expr = Decompile(right);
                return new UnaryExpression(op, right_expr);
            }
            case "Binary":
            {
                JObject left = (JObject)expressionObject.SelectToken("$.left");
                string operator_name = expressionObject.SelectToken("$.operator.token_type", true).ToString();
                JObject right = (JObject)expressionObject.SelectToken("$.right");

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

                var left_expr = Decompile(left);
                var right_expr = Decompile(right);
                return new BinaryExpression(op, left_expr, right_expr);
            }
            case "Logical":
            {
                JObject left = (JObject)expressionObject.SelectToken("$.left");
                string operator_name = expressionObject.SelectToken("$.operator.token_type", true).ToString();
                JObject right = (JObject)expressionObject.SelectToken("$.right");

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

                var left_expr = Decompile(left);
                var right_expr = Decompile(right);
                return new LogicalExpression(op, left_expr, right_expr);
            }
            case "Assign":
            {
                JObject name = (JObject)expressionObject.SelectToken("$.name");
                JObject value = (JObject)expressionObject.SelectToken("$.value");
                var name_expr = Decompile(name);
                var value_expr = Decompile(value);
                return new AssignmentExpression(AssignmentOperator.Assign, name_expr, value_expr);
            }
            case "Call":
            {
                Expression callee = Decompile((JObject)expressionObject.SelectToken("$.call", true));
                JArray args = (JArray)expressionObject.SelectToken("$.args", true);
                List<Expression> args_list = args.Select(arg => Decompile((JObject)arg)).ToList();
                return new CallExpression(callee, NodeList.Create(args_list), false);
            }
            case "Grouping":
            {
                JObject expr = (JObject)expressionObject.SelectToken("$.expr");
                // Esprima does not have explicit grouping in its AST, however, it appears to render it out properly.

                return new CallExpression(
                    new Identifier("__group"),
                    NodeList.Create(new List<Expression>() { Decompile(expr) }),
                     false
                );
            }
            default:
                throw new Exception($"Unhandled Expression Type: {expressionType}");
        }
    }
}