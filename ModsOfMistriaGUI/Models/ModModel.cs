using CommunityToolkit.Mvvm.ComponentModel;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaGUI.Models;

public class ModModel: ObservableObject
{
    public readonly IMod Mod;

    public readonly string? CanInstall;
    private bool _enabled = true;

    public ModModel(IMod mod)
    {
        Mod = mod;
        CanInstall = mod.CanInstall();
        _enabled = mod.IsInstalled();
    }

    public ModModel()
    {
        Mod = new FolderMod();
        CanInstall = Mod.CanInstall();
    }

    public bool Enabled
    {
        get => !InError && _enabled;
        set => _enabled = value;
    }
    
    public bool InWarning => Mod.GetValidation().Status == ValidationStatus.Warning;
    public bool InError => Mod.GetValidation().Status == ValidationStatus.Invalid;
    
    public bool IsValid => Mod.GetValidation().Status == ValidationStatus.Valid;
    
    public string Warnings => string.Join("\r\n", Mod.GetValidation().Warnings.Select(warning => warning.Message).ToList());
    
    public string Errors => string.Join("\r\n", Mod.GetValidation().Errors.Select(warning => warning.Message).ToList());
    
    public string Full => string.Format(Resources.GUIModByAuthor, Mod.GetName(), Mod.GetAuthor());
}