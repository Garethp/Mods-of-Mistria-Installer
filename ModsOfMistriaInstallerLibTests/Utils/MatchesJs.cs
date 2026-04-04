using Esprima;
using Esprima.Utils;
using NUnit.Framework.Constraints;

namespace ModsOfMistriaInstallerLibTests.Utils;

public class MatchesJs: Constraint
{
    private string _expected;
    
    public MatchesJs(string expected)
    {
        _expected = expected;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var parser = new JavaScriptParser();
        
        var actualJs = parser.ParseScript(actual.ToString()).ToJavaScriptString(true);
        var expectedJs = parser.ParseScript(_expected).ToJavaScriptString(true);
        
        var equals = actualJs.Equals(expectedJs);
        return new ConstraintResult(this, actual, equals);
    }

    public override string Description => $"matches js {_expected}";
}