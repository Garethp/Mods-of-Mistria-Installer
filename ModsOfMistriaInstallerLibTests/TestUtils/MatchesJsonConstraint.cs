using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

public class MatchesJsonConstraint(JToken expected) : Constraint
{
    private readonly JToken _object = expected;

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        JToken actualJson;
        
        if (actual is string actualString)
        {
            try
            {
                actualJson = JToken.Parse(actualString);
            }
            catch (Exception)
            {
                return new ConstraintResult(this, actual, false);
            }
        }
        else if (actual is JToken actualObject)
        {
            actualJson = actualObject;
        }
        else
        {
            return new ConstraintResult(this, actual, false);
        }
        
        var equals= JToken.EqualityComparer.Equals(_object, actualJson);
        
        return new ConstraintResult(this, actualJson.ToString(), equals);
    }

    public override string Description => $"matches the {_object}";
}