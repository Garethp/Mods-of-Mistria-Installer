using Garethp.ModsOfMistriaGUI.ViewModels;
using Garethp.ModsOfMistriaInstallerLib;

namespace Garethp.ModsOfMistriaGUI.Models;

public static class DesignData
{
    public static readonly ModlistPageViewModel ModlistPageViewModel = new(new Settings(MistriaLocator.GetMistriaLocation(), MistriaLocator.GetModsLocation(MistriaLocator.GetMistriaLocation())));

    public static readonly GettingStartedPageViewModel GettingStartedPage = new (new Settings());
}