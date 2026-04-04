using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public class ExpressionStatementEncoder
{
    public static JObject Encode(ExpressionStatement statement)
    {
        switch (statement.Expression)
        {
            case CallExpression callExpression:
                return Encoder.EncodeJS(callExpression);
            case AssignmentExpression assignmentExpression:
                return Encoder.EncodeJS(assignmentExpression);
        }

        throw new Exception($"Expression type is not implemented: {statement.Expression.Type}");
    }
}