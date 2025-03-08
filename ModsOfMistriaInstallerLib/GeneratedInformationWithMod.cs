using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib;

public class GeneratedInformationWithMod(IMod mod) : GeneratedInformation
{
    public IMod Mod { get; } = mod;
}