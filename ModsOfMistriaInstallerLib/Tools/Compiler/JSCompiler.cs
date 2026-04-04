using Esprima.Ast;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Tools.Compiler;

public static class JsCompiler
{
    public static JObject Compile(Node statement)
    {
        return statement switch
        {
            ExpressionStatement expressionStatement => ExpressionStatementCompiler.Compile(expressionStatement),
            VariableDeclaration variableDeclaration => CompileVariableDeclaration(variableDeclaration),
            BlockStatement blockStatement => CompileBlockStatement(blockStatement),
            IfStatement ifStatement => CompileIfStatement(ifStatement),
            FunctionDeclaration functionDeclaration => CompileFunctionDeclaration(functionDeclaration),
            ReturnStatement returnStatement => CompileReturnStatement(returnStatement),
            Expression expression => ExpressionCompiler.Compile(expression),
            _ => throw new Exception($"Type not implemented: {statement.Type}")
        };
    }

    private static JObject CompileVariableDeclaration(VariableDeclaration declarations)
    {
        if (declarations.Kind != VariableDeclarationKind.Var)
            throw new NotImplementedException($"Variable type {declarations.Kind} not implemented");

        if (declarations.Declarations.Count != 1)
            throw new NotImplementedException("Multiple declarations are not supported");

        var declaration = declarations.Declarations[0];

        if (declaration.Id is not Identifier declarationId)
            throw new NotImplementedException($"Identifier type {declaration.Id} not implemented");

        if (declaration.Init is null)
            throw new NotImplementedException("Empty variable initializers not supported");

        return new JObject
        {
            { "stmt_type", "Var" },
            {
                "name", new JObject
                {
                    { "token_type", "Identifier" },
                    { "value", declarationId.Name }
                }
            },
            { "initializer", CleanCallExpression(ExpressionCompiler.Compile(declaration.Init)) }
        };
    }

    private static JObject CompileBlockStatement(BlockStatement statement)
    {
        return new JObject
        {
            { "stmt_type", "Block" },
            { "stmts", new JArray(statement.Body.Select(Compile)) }
        };
    }

    private static JObject CompileIfStatement(IfStatement ifStatement)
    {
        var ifObject = new JObject
        {
            { "stmt_type", "If" },
            { "condition", CleanCallExpression(ExpressionCompiler.Compile(ifStatement.Test)) },
            { "then_branch", Compile(ifStatement.Consequent) },
            { "else_branch", ifStatement.Alternate is not null ? Compile(ifStatement.Alternate) : "null" }
        };
        
        return ifObject;
    }

    private static JObject CompileFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        var bodyStatements = functionDeclaration.Body.Body.Select(Compile);

        var resolve = bodyStatements
            .Where(statement => statement["stmt_type"].ToString() == "Resolve")
            .FirstOrDefault();
        bodyStatements = bodyStatements
            .Where(statement => statement["stmt_type"].ToString() != "Resolve")
            .ToList();

        var functionObj = new JObject
        {
            { "stmt_type", "Function" },
            {
                "name", new JObject
                {
                    { "token_type", "Identifier" },
                    { "value", functionDeclaration.Id.Name }
                }
            },
            {
                "params", new JArray(functionDeclaration.Params.Select(param =>
                {
                    var paramObj = new JObject
                    {
                        { "token_type", "Identifier" }
                    };

                    if (param is AssignmentPattern assignmentParam)
                    {
                        if (assignmentParam.Left is not Identifier assignmentParamIdentifier)
                            throw new NotImplementedException(
                                $"Param Assignment Type Not Supported: {assignmentParam.Left.Type}");

                        paramObj.Add("value", assignmentParamIdentifier.Name);
                        paramObj.Add("default_value", ExpressionCompiler.Compile(assignmentParam.Right));
                    }
                    else if (param is Identifier identifierParam)
                    {
                        paramObj.Add("value", identifierParam.Name);
                        paramObj.Add("default_value", "null");
                    }

                    return paramObj;
                }))
            },
            {
                "body", new JObject
                {
                    { "stmt_type", "Block" },
                    { "stmts", new JArray(bodyStatements) }
                }
            },
            { "resolve", resolve is not null ? resolve["stmts"] : "null" }
        };

        return functionObj;
    }

    private static JObject CompileReturnStatement(ReturnStatement returnStatement)
    {
        
        if (returnStatement.Argument is null)
        {
            return new JObject
            {
                { "stmt_type", "Return" },
                { "value", "null" }
            };
        }
        
        if (returnStatement.Argument is CallExpression { Callee: Identifier { Name: "__resolve" } })
            return Compile(returnStatement.Argument);

        return new JObject
        {
            { "stmt_type", "Return" },
            { "value", CleanCallExpression(ExpressionCompiler.Compile(returnStatement.Argument)) }
        };
    }

    public static JObject CleanCallExpression(JObject input)
    {
        if (
            input.ContainsKey("stmt_type") &&
            input.ContainsKey("expr") &&
            input["stmt_type"]!.ToString() == "Expr" &&
            input["expr"] is JObject expr)
            return expr;
        
        return input;
    }
}