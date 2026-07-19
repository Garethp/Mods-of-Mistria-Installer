using CommandLine;

namespace Garethp.ModsOfMistriaCommandLine
{

    [Verb("version", HelpText = "Display version information.")]
    class VersionOptions
    {

    }

    [Verb("help", HelpText = "Display this help message.")]
    class HelpOptions
    {

    }

    [Verb("install", HelpText = "Install all the mods found in the mods folder.")]
    class InstallOptions
    {

    }

    [Verb("uninstall", HelpText = "Uninstall all the mods.")]
    class UninstallOptions
    {

    }

    [Verb("list", HelpText = "List all the mods found in the mods folder.")]
    class ListOptions
    {

    }
}
