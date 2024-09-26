using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public enum ValidationStatus
{
    Valid,
    Warning,
    Invalid,
}

public class Validation
{
    public ValidationStatus Status
    {
        get
        {
            if (Errors.Count != 0) return ValidationStatus.Invalid;
            if (Warnings.Count != 0) return ValidationStatus.Warning;
            
            return ValidationStatus.Valid;
        }
    }

    public List<ValidationMessage> Errors { get; } = [];

    public List<ValidationMessage> Warnings { get; } = [];
    
    public void AddError(IMod mod, string fileName, string message)
    {
        Errors.Add(new ValidationMessage(mod, fileName, message));
    }
    
    public void AddWarning(IMod mod, string fileName, string message)
    {
        Warnings.Add(new ValidationMessage(mod, fileName, message));
    }

    public void Merge(Validation other)
    {
        Warnings.AddRange(other.Warnings);
        Errors.AddRange(other.Errors);
    }
}

public class ValidationMessage(IMod mod, string fileName, string message)
{
    public IMod Mod = mod;

    public string FileName = fileName;
    
    public string Message = message;
}