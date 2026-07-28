using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;
using Tomlyn;
using Tomlyn.Model;

namespace ModsOfMistriaInstallerLibTests.TestUtils;

public class ContainsTomlConstraint(TomlTable expected): Constraint
{
    private readonly TomlTable _object = expected;
    
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        TomlTable actualToml;
        
        switch (actual)
        {
            case string actualString:
                try
                {
                    actualToml = TomlSerializer.Deserialize<TomlTable>(actualString);
                    if (actualToml is null)
                    {
                        return new ConstraintResult(this, actual, false);
                    }
                }
                catch (Exception)
                {
                    return new ConstraintResult(this, actual, false);
                }

                break;
            case TomlTable actualObject:
                actualToml = actualObject;
                break;
            default:
                return new ConstraintResult(this, actual, false);
        }

        var equals = TomlContains(actualToml, _object);
        
        return new ConstraintResult(this, actual, equals);
    }

    private static bool TomlContains(object complete, object partial)
    {
        switch (complete)
        {
            case TomlTable completeObject:
            {
                if (partial is not TomlTable partialObject) return false;
                if (partialObject.Keys.Any(property => !completeObject.ContainsKey(property))) return false;

                return partialObject.Keys.All(property => TomlContains(completeObject[property], partialObject[property]));
            }
            case TomlArray completeArray:
                if (partial is not TomlArray partialArray) return false;
                
                return partialArray.All(partialValue => completeArray.Contains(partialValue));
            case string completeString:
                if (partial is not string partialString) return false;

                return completeString == partialString;
            case int completeInt:
                if (partial is not int partialInt) return false;
                
                return completeInt == partialInt;
            case long completeLong:
                if (partial is not long partialLong) return false;
                
                return completeLong == partialLong;
            case double completeDouble:
                if (partial is not double partialDouble) return false;
                
                return Math.Abs(completeDouble - partialDouble) < 0.0001;
            case bool completeBool:
                if (partial is not bool partialBool) return false;
                
                return completeBool == partialBool;
            default:
                throw new NotImplementedException();
        }
    }
    
    public override string Description => $"contains json {_object}";
}