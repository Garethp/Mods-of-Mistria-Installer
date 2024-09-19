using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaInstallerLib;
using Garethp.ModsOfMistriaInstallerLib.Generator;

namespace Garethp.ModsOfMistriaGUI.App.Models;

public partial class ModModel: ObservableObject
{
    public Mod mod;

    public string? CanInstall;
    private bool _enabled = true;

    public bool Enabled
    {
        get => !InError && _enabled;
        set => _enabled = value;
    }

    public Validation validation => mod.validation;
    
    public bool InWarning => mod.validation.Status == ValidationStatus.Warning;
    public bool InError => mod.validation.Status == ValidationStatus.Invalid;
    
    public string Warnings => String.Join("\r\n", mod.validation.Warnings.Select(warning => warning.Message).ToList());
    
    public string Errors => String.Join("\r\n", mod.validation.Errors.Select(warning => warning.Message).ToList());

    
    public string Full => $"{mod.Author} by {mod.Name}";
}