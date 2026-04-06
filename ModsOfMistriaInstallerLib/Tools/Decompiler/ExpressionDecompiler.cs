using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Decompiler;

public static class ExpressionDecompiler
{
    public static Expression Decompile(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.expr_type") is not JValue expressionType)
            throw new Exception("Expression has no expression type");

        return $"{expressionType}" switch
        {
            "Literal" => DecompileLiteralExpression(expressionObject),
            "Named" => DecompileNamedExpression(expressionObject),
            "Unary" => DecompileUnaryExpression(expressionObject),
            "Binary" => DecompileBinaryExpression(expressionObject),
            "Logical" => DecompileLogicalExpression(expressionObject),
            "Assign" => DecompileAssignExpression(expressionObject),
            "Call" => DecompileCallExpression(expressionObject),
            "Grouping" => DecompileGroupingExpression(expressionObject),
            _ => throw new Exception($"Unhandled Expression Type: {expressionType}")
        };
    }

    private static Expression DecompileLiteralExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.value.token_type") is not JValue tokenType)
            throw new Exception("Literal Value has no token type");

        switch ($"{tokenType}")
        {
            case "True":
                return new Literal(true, "true");
            case "False":
                return new Literal(false, "false");
            case "Number":
            {
                if (expressionObject.SelectToken("$.value.Value") is not JValue value)
                    throw new Exception("Literal Number has no value");
                
                return new Literal((double) value, $"{value}");
            }
            case "String":
            {
                if (expressionObject.SelectToken("$.value.value") is not JValue value)
                    throw new Exception("Literal String has no value");
                
                // Esprima will use the raw to render the String literal, so we add back the double quotes.
                return new Literal($"{value}", $"\"{value}\"");
            }
            default:
                throw new Exception($"Unknown token type for a Literal Expression: {tokenType}");
        }
    }

    private static Expression DecompileNamedExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.name.value") is not JValue name)
            throw new Exception("Named expression has no name");

        return new Identifier($"{name}");
    }

    private static Expression DecompileUnaryExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.operator.token_type") is not JValue operatorName)
            throw new Exception("Unary Operator has no token type");
        
        if (expressionObject.SelectToken("$.right") is not JObject right)
            throw new Exception("Unary Expression has no right side");
        
        var unaryOperator = $"{operatorName}" switch
        {
            "Minus" => UnaryOperator.Minus,
            "Bang" => UnaryOperator.LogicalNot,
            _ => throw new Exception($"unknown binary operator: {operatorName}")
        };

        return new UnaryExpression(unaryOperator, Decompile(right));
    }

    private static Expression DecompileBinaryExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.left") is not JObject left)
            throw new Exception("Binary operator has no left side");
        
        if (expressionObject.SelectToken("$.operator.token_type") is not JValue operatorName)
            throw new Exception("Binary Operator has no token type");
        
        if (expressionObject.SelectToken("$.right") is not JObject right)
            throw new Exception("Binary Expression has no right side");
        
        var binaryOperator = $"{operatorName}" switch
        {
            "DoubleEqual" => BinaryOperator.Equal,
            "BangEqual" => BinaryOperator.NotEqual,
            "LessEqual" => BinaryOperator.LessOrEqual,
            "Less" => BinaryOperator.Less,
            "GreaterEqual" => BinaryOperator.GreaterOrEqual,
            "Greater" => BinaryOperator.Greater,
            "Plus" => BinaryOperator.Plus,
            "Minus" => BinaryOperator.Minus,
            "Star" => BinaryOperator.Times,
            "Slash" => BinaryOperator.Divide,
            _ => throw new Exception($"Unknown binary operator: {operatorName}")
        };
        
        return new BinaryExpression(binaryOperator, Decompile(left), Decompile(right));
    }

    private static Expression DecompileLogicalExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.left") is not JObject left)
            throw new Exception("Logical Expression has no left side");

        if (expressionObject.SelectToken("$.operator.token_type") is not JValue operatorName)
            throw new Exception("Logical Expression has no token type");
        
        if (expressionObject.SelectToken("$.right") is not JObject right)
            throw new Exception("Logical Expression has no right side");
        
        var binaryOperator = $"{operatorName}" switch
        {
            "And" => BinaryOperator.LogicalAnd,
            "Or" => BinaryOperator.LogicalOr,
            _ => throw new Exception($"unknown logical operator: {operatorName}")
        };

        return new LogicalExpression(binaryOperator, Decompile(left), Decompile(right));
    }

    private static Expression DecompileAssignExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.name") is not JObject name)
            throw new Exception("Assign expression has no name");
        
        if (expressionObject.SelectToken("$.value") is not JObject value)
            throw new Exception("Assign Expression has no value");
        
        return new AssignmentExpression(AssignmentOperator.Assign, Decompile(name), Decompile(value));
    }

    private static Expression DecompileCallExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.call") is not JObject call)
            throw new Exception("Call Expression has no call");
        
        if (expressionObject.SelectToken("$.args") is not JArray arguments)
            throw new Exception("Call Expression has no arguments");
        
        return new CallExpression(
            Decompile(call),
            NodeList.Create(arguments.Select(argumentToken =>
            {
                if (argumentToken is not JObject argument)
                    throw new Exception("Call Expression argument is not an object");
                
                return Decompile(argument);
            })), 
            false
        );
    }

    private static Expression DecompileGroupingExpression(JObject expressionObject)
    {
        if (expressionObject.SelectToken("$.expr") is not JObject expression)
            throw new Exception("Grouping Expression has no expr");

        return new CallExpression(
            new Identifier("__group"),
            NodeList.Create(new List<Expression> { Decompile(expression) }),
            false
        );
    }
}