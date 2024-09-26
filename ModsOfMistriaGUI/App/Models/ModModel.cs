using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using ModsOfMistriaGUI.App.Lang;

namespace Garethp.ModsOfMistriaGUI.App.Models;

public partial class ModModel: ObservableObject
{
    public IMod mod;

    public string? CanInstall;
    private bool _enabled = true;

    public bool Enabled
    {
        get => !InError && _enabled;
        set => _enabled = value;
    }

    public Validation validation => mod.GetValidation();
    
    public bool InWarning => mod.GetValidation().Status == ValidationStatus.Warning;
    public bool InError => mod.GetValidation().Status == ValidationStatus.Invalid;
    
    public bool IsValid => mod.GetValidation().Status == ValidationStatus.Valid;
    
    public string Warnings => String.Join("\r\n", mod.GetValidation().Warnings.Select(warning => warning.Message).ToList());
    
    public string Errors => String.Join("\r\n", mod.GetValidation().Errors.Select(warning => warning.Message).ToList());
    
    public string Full => string.Format(Resources.ModByAuthor, mod.GetName(), mod.GetAuthor());
}