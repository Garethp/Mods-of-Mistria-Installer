using Garethp.ModsOfMistriaInstallerLib.Generator;

namespace ModsOfMistriaInstallerLibTests;

public class ValidationComparer: IEqualityComparer<Validation>
{
    public bool Equals(Validation? x, Validation? y)
    {
        if (x.Warnings.Count != y.Warnings.Count) return false;
        if (x.Errors.Count != y.Errors.Count) return false;

        for (var i = 0; i < x.Warnings.Count; i++)
        {
            if (!CompareValidationMessages(x.Warnings[i], y.Warnings[i])) return false;
        }

        
        for (var i = 0; i < x.Errors.Count; i++)
        {
            if (!CompareValidationMessages(x.Errors[i], y.Errors[i])) return false;
        }
        
        return true;
    }

    private bool CompareValidationMessages(ValidationMessage x, ValidationMessage y)
    {
        return x.FileName == y.FileName && x.Message == y.Message;
    }

    public int GetHashCode(Validation obj)
    {
        throw new NotImplementedException();
    }
}