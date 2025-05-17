using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
public class AurieInstaller : IModuleInstaller
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
        TearDownRegistry();
        
        if (information.AurieMods.Count == 0)
        {
            Uninstall(fieldsOfMistriaLocation);
            return;
        }

        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe")))
        {
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe"), 
                Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe")
            );
        }
        
        if (!Directory.Exists(Path.Combine(modsLocation, "aurie", "MOMI")))
        {
            Directory.CreateDirectory(Path.Combine(modsLocation, "aurie", "MOMI"));
        }

        if (!Directory.Exists(Path.Combine(modsLocation, "native")))
        {
            Directory.CreateDirectory(Path.Combine(modsLocation, "native"));
        }
        
        List<FileToEnsure> filesToEnsure =
        [
            new()
            {
                Path = Path.Combine(fieldsOfMistriaLocation, "AuriePatcher.exe"),
                Repository = "Garethp/AuriePatcher-Beta",
                Artifact = "AuriePatcher.exe"
            },
            new()
            {
                Path = Path.Combine(modsLocation, "aurie", "YYToolkit.dll"),
                Repository = "AurieFramework/YYToolkit",
                Artifact = "YYToolkit.dll"
            },
            new()
            {
                Path = Path.Combine(modsLocation, "native", "AurieCore.dll"),
                Repository = "AurieFramework/Aurie",
                Artifact = "AurieCore.dll"
            },
            new()
            {
                Path = Path.Combine(modsLocation, "AurieLoader.exe"),
                Repository = "AurieFramework/Aurie",
                Artifact = "AurieLoader.exe"
            }
        ];

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("aurie");
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
        
        PatchAurie(fieldsOfMistriaLocation, modsLocation);

        foreach (var file in Directory.GetFiles(Path.Combine(modsLocation, "aurie", "MOMI")))
        {
            File.Delete(file);
        }

        foreach (var directory in Directory.GetDirectories(Path.Combine(modsLocation, "aurie", "MOMI")))
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }

            Directory.Delete(directory);
        }

        information.AurieMods.ForEach(aurieMod =>
        {
            if (!Directory.Exists(Path.Combine(modsLocation, "aurie", "MOMI", aurieMod.Mod.GetId())))
            {
                Directory.CreateDirectory(Path.Combine(modsLocation, "aurie", "MOMI", aurieMod.Mod.GetId()));
            }

            using var newFile =
                new FileStream(
                    Path.Combine(modsLocation, "aurie", "MOMI", aurieMod.Mod.GetId(),
                        Path.GetFileName(aurieMod.Location)), FileMode.CreateNew);

            aurieMod.Mod.ReadFileAsStream(aurieMod.Location).CopyTo(newFile);

            newFile.Close();
        });
    }

    private void PatchAurie(string fieldsOfMistriaLocation, string modsLocation)
    {
        var patcherLocation = Path.Combine(fieldsOfMistriaLocation, "AuriePatcher.exe");
        var dllLocation = Path.Combine(modsLocation, "native", "AurieCore.dll");
        var exeLocation = Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe");

        var wineLocation = MistriaLocator.GetWineLocation();
        
        if (!File.Exists(patcherLocation) || !File.Exists(dllLocation))
        {
            return;
        }

        var proc = new Process();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            proc.StartInfo.FileName = patcherLocation;

        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (!File.Exists(wineLocation))
            {
                throw new Exception("Wine not found");
            }
            
            proc.StartInfo.FileName = wineLocation;
            proc.StartInfo.ArgumentList.Add(patcherLocation);
        }
        else
        {
            throw new Exception("Aurie cannot install on this system");
        }
        
        proc.StartInfo.ArgumentList.Add(exeLocation);
        proc.StartInfo.ArgumentList.Add(dllLocation);
        proc.StartInfo.ArgumentList.Add("install");
        proc.StartInfo.UseShellExecute = false;
        proc.StartInfo.WorkingDirectory = fieldsOfMistriaLocation;
        proc.StartInfo.RedirectStandardOutput = true;
        proc.StartInfo.Verb = "runas";
        proc.Start();

        proc.WaitForExit(TimeSpan.FromSeconds(10));
        
        var output = proc.StandardOutput.ReadToEnd();

        if (proc.ExitCode != 0)
        {
            throw new Exception(output);
        }
        
        return;
    }

    public void Uninstall(string fieldsOfMistriaLocation)
    {
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe"))) return;
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe"))) return;

        File.Delete(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe"));
        File.Copy(
            Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe"), 
            Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe")
        );
        
        TearDownRegistry();
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