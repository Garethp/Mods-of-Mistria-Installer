﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

class FileToEnsure
{
    public string Path;
    public string Repository;
    public string Artifact;
}

[InformationInstaller(1)]
public class AurieInstaller : IModuleInstaller, IPreinstallInfo, IPreUninstallInfo
{
    private static readonly string IFEORegistryKey =
        "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options";
    
    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    )
    {
        if (information.AurieMods.Count == 0)
        {
            TearDownRegistry();
            return;
        }
        
        if (!Directory.Exists(Path.Combine(modsLocation, "Aurie", "MOMI")))
        {
            Directory.CreateDirectory(Path.Combine(modsLocation, "Aurie", "MOMI"));
        }

        if (!Directory.Exists(Path.Combine(modsLocation, "Native")))
        {
            Directory.CreateDirectory(Path.Combine(modsLocation, "Native"));
        }
        
        SetupRegistry(fieldsOfMistriaLocation, modsLocation);

        List<FileToEnsure> filesToEnsure =
        [
            new ()
            {
                Path = Path.Combine(modsLocation, "Aurie", "YYToolkit.dll"),
                Repository = "AurieFramework/YYToolkit",
                Artifact = "YYToolkit.dll"
            },
            new ()
            {
                Path = Path.Combine(modsLocation, "Native", "AurieCore.dll"),
                Repository = "AurieFramework/Aurie",
                Artifact = "AurieCore.dll"
            },
            new ()
            {
                Path = Path.Combine(modsLocation, "AurieLoader.exe"),
                Repository = "AurieFramework/Aurie",
                Artifact = "AurieLoader.exe"
            }
        ];

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Aurie");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

        filesToEnsure.ForEach(ensure =>
        {
            if (File.Exists(ensure.Path))
            {
                return;
            }

            var task = Task.Run(async () =>
            {
                var apiUrl = $"https://api.github.com/repos/{ensure.Repository}/releases/latest";

                var response = await client.GetStringAsync(apiUrl);
                var json = JObject.Parse(response);

                var asset = json["assets"]?.FirstOrDefault(asset => asset["name"]?.ToString() == ensure.Artifact);

                if (asset is null) throw new Exception("Aurie Asset Not Found");

                using var fileDownload = await client.GetAsync(asset["browser_download_url"]?.ToString());
                await using var newFile = new FileStream(ensure.Path, FileMode.CreateNew);

                await fileDownload.Content.CopyToAsync(newFile);
                
                newFile.Close();
            });

            task.Wait();
        });
        
        foreach (var file in Directory.GetFiles(Path.Combine(modsLocation, "Aurie", "MOMI")))
        {
            File.Delete(file);
        }
        
        foreach (var directory in Directory.GetDirectories(Path.Combine(modsLocation, "Aurie", "MOMI")))
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }
            
            Directory.Delete(directory);
        }

        information.AurieMods.ForEach(aurieMod =>
        {
            if (!Directory.Exists(Path.Combine(modsLocation, "Aurie", "MOMI", aurieMod.Mod.GetId())))
            {
                Directory.CreateDirectory(Path.Combine(modsLocation, "Aurie", "MOMI", aurieMod.Mod.GetId()));
            }

            using var newFile = new FileStream(Path.Combine(modsLocation, "Aurie", "MOMI", aurieMod.Mod.GetId(), Path.GetFileName(aurieMod.Location)), FileMode.CreateNew);

            aurieMod.Mod.ReadFileAsStream(aurieMod.Location).CopyTo(newFile);
            
            newFile.Close();
        });
    }

    public void Uninstall()
    {
        TearDownRegistry();
    }

    private bool IsInstalled()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
        return Registry.LocalMachine.OpenSubKey(IFEORegistryKey)?.OpenSubKey("FieldsOfMistria.exe") is not null;
    }

    public List<string> GetPreinstallInformation(GeneratedInformation information)
    {
        if (information.AurieMods.Count > 0 && !IsInstalled()) return [Resources.CorePreinstallWillInstallAurie];
        if (information.AurieMods.Count == 0 && IsInstalled()) return [Resources.CorePreinstallWillRemoveAurie];

        return [];
    }

    public List<string> GetPreUninstallInformation()
    {
        if (IsInstalled()) return [Resources.CorePreinstallWillRemoveAurie];

        return [];
    }
    
    private void SetupRegistry(string fieldsOfMistriaLocation, string modsLocation)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var ifeoKey = Registry.LocalMachine.OpenSubKey(IFEORegistryKey);
        if (ifeoKey is null) throw new Exception("IFEO Registry Not Found");

        var mistriaSubKey = Registry.LocalMachine.OpenSubKey(IFEORegistryKey)?.OpenSubKey("FieldsOfMistria.exe");

        if (mistriaSubKey is not null) return;

        var installFile = @$"Windows Registry Editor Version 5.00

[{ifeoKey.Name}\FieldsOfMistria.exe]
""IsAurieInstallerKey""=dword:00000001
""UseFilter""=dword:00000001

[{ifeoKey.Name}\FieldsOfMistria.exe\{Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe").Replace(Path.DirectorySeparatorChar, '_')}]
""Debugger""=""{Path.Combine(modsLocation, "AurieLoader.exe").Replace("\\", "\\\\")}""
""FilterFullPath""=""{Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe").Replace("\\", "\\\\")}""
";

        File.WriteAllText(Path.Combine(modsLocation, "Aurie", "install.reg"), installFile);

        var proc = new Process();
        proc.StartInfo.FileName = "regedit.exe";
        proc.StartInfo.ArgumentList.Add(Path.Combine(modsLocation, "Aurie", "install.reg"));
        proc.StartInfo.UseShellExecute = true;
        proc.StartInfo.Verb = "runas";
        proc.Start();

        proc.WaitForExit();

        File.Delete(Path.Combine(modsLocation, "Aurie", "install.reg"));
    }

    private void TearDownRegistry()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var mistriaSubKey = Registry.LocalMachine.OpenSubKey(IFEORegistryKey)?.OpenSubKey("FieldsOfMistria.exe");

        if (mistriaSubKey is null) return;

        var proc = new Process();
        proc.StartInfo.FileName = "reg";
        proc.StartInfo.ArgumentList.Add("delete");
        proc.StartInfo.ArgumentList.Add(mistriaSubKey.Name);
        proc.StartInfo.ArgumentList.Add("/f");
        proc.StartInfo.UseShellExecute = true;
        proc.StartInfo.Verb = "runas";
        proc.Start();
    }
}