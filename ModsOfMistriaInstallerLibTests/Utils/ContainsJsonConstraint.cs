using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;

namespace ModsOfMistriaInstallerLibTests.Utils;

public class ContainsJsonConstraint(JObject expected): Constraint
{
    private readonly JObject _object = expected;
    
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        JObject actualJson;
        
        switch (actual)
        {
            case string actualString:
                try
                {
                    actualJson = JObject.Parse(actualString);
                }
                catch (Exception)
                {
                    return new ConstraintResult(this, actual, false);
                }

                break;
            case JObject actualObject:
                actualJson = actualObject;
                break;
            default:
                return new ConstraintResult(this, actual, false);
        }

        var equals = JsonContains(actualJson, _object);
        
        return new ConstraintResult(this, actual, equals);
    }

    private static bool JsonContains(JToken complete, JToken partial)
    {
        switch (complete)
        {
            case JValue completeValue:
            {
                return partial is JValue partialValue && partialValue.Type == completeValue.Type && partialValue.Equals(completeValue);
            }
            case JArray completeArray:
                return partial is JArray partialArray && partialArray.All(partialValue => completeArray.Values().Contains(partialValue));
            case JObject completeObject:
            {
                if (partial is not JObject partialObject) return false;
                if (partialObject.Properties().Any(property => completeObject.Property(property.Name) is null)) return false;

                foreach (var property in partialObject.Properties())
                {
                    if (!JsonContains(completeObject.Property(property.Name)!.Value, property.Value)) return false;
                }

                return true;
            }
            default:
                throw new NotImplementedException();
        }
    }
    
    public override string Description => "contains json";
}