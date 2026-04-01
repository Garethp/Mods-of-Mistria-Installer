using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace ModsOfMistriaInstallerLibTests.Utils;

public class MatchesJsonConstraint(JObject expected) : Constraint
{
    private readonly JObject _object = expected;

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        JObject actualJson;
        
        if (actual is string actualString)
        {
            try
            {
                actualJson = JObject.Parse(actualString);
            }
            catch (Exception)
            {
                return new ConstraintResult(this, actual, false);
            }
        }
        else if (actual is JObject actualObject)
        {
            actualJson = actualObject;
        }
        else
        {
            return new ConstraintResult(this, actual, false);
        }
        
        var equals= JToken.EqualityComparer.Equals(_object, actualJson);
        
        return new ConstraintResult(this, actual, equals);
    }

    public override string Description => $"matches the {_object}";
}