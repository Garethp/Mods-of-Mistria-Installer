using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public static class ExpressionStatementCompiler
{
    public static JObject Compile(ExpressionStatement statement)
    {
        return statement.Expression switch
        {
            CallExpression callExpression => JsCompiler.Compile(callExpression),
            AssignmentExpression assignmentExpression => JsCompiler.Compile(assignmentExpression),
            _ => throw new Exception($"Expression type is not implemented: {statement.Expression.Type}")
        };
    }
}