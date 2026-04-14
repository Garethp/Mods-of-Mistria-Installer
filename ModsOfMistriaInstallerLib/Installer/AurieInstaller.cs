using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using PeNet;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;

class FileToEnsure
{
    public string Path;
    public string Repository;
    public string Artifact;
    public string Release = "latest";
    public bool ShouldUpdate = true;
}

[InformationInstaller(1)]
public class AurieInstaller : IModuleInstaller, IPreinstallInfo, IPreUninstallInfo
{
    private IFileModifier _fileModifier = new FileModifier();

    public void SetFileModifier(IFileModifier fileModifier) => _fileModifier = fileModifier;
    
    private static readonly string IFEORegistryKey =
        "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options";

    public void Install(
        string fieldsOfMistriaLocation,
        string modsLocation,
        GeneratedInformation information,
        Action<string, string> reportStatus
    )
    {
        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe")))
        {
            throw new FileNotFoundException("Could not find FieldsOfMistria.exe in Fields of Mistria folder");
        }

        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe")))
        {
            File.Copy(
                Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe"),
                Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe")
            );
        }
        
        File.Delete(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe"));
        File.Copy(
            Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe"),
            Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe")
        );
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            modsLocation != Path.Combine(fieldsOfMistriaLocation, "mods"))
        {
            var requiredAurieLocation = Path.Combine(fieldsOfMistriaLocation, "mods");
            if (!Directory.Exists(requiredAurieLocation))
            {
                File.CreateSymbolicLink(requiredAurieLocation, modsLocation);
            }
            
            modsLocation = requiredAurieLocation;
        }
        
        // @TODO: Remove this some time in 2026, when we're sure people have migrated
        TearDownRegistry();
        
        if (information.AurieMods.Count == 0)
        {
            return;
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
                Repository = "AurieFramework/Aurie",
                Artifact = "AuriePatcher.exe",
                ShouldUpdate = false
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
            }
        ];
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            filesToEnsure[2].Release = "267229495";
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Aurie");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

        filesToEnsure.ForEach(ensure =>
        {
            if (File.Exists(ensure.Path))
            {
                var needsUpdate = false;
                if (!ensure.ShouldUpdate) return;
                var peFile = new PeFile(ensure.Path);
                
                var versionString = peFile.Resources?.VsVersionInfo?.StringFileInfo.StringTable.FirstOrDefault()?.FileVersion;

                if (versionString == null)
                {
                    needsUpdate = true;
                }
                else
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var apiUrl = $"https://api.github.com/repos/{ensure.Repository}/releases/{ensure.Release}";

                            var response = await client.GetStringAsync(apiUrl);
                            var json = JObject.Parse(response);
                            var latestVersion = json["tag_name"]?.ToString().Replace("v", "");

                            if (latestVersion == null) return;

                            latestVersion = Regex.Replace(latestVersion, @"[a-zA-Z]", "");
                            var latestVersionItem = new Version(latestVersion);
                            if (latestVersion.Split('.').Length == 3 && versionString.Split('.').Length > 3)
                            {
                                var versionArray = versionString.Split('.').Take(3);
                                versionString = String.Join('.', versionArray);
                            }
                            
                            if (latestVersionItem.CompareTo(new Version(versionString)) != 0)
                            {
                                needsUpdate = true;
                            }
                        }
                        catch (Exception e)
                        {
                            // Ignored
                        }
                    }).Wait();
                }

                if (!needsUpdate) return;
                File.Delete(ensure.Path);
            }

            var task = Task.Run(async () =>
            {
                var apiUrl = $"https://api.github.com/repos/{ensure.Repository}/releases/{ensure.Release}";

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
        
        PatchAurie(fieldsOfMistriaLocation, modsLocation);
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
        TearDownRegistry();

        if (!File.Exists(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe"))) return;
        
        File.Delete(Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe"));
        File.Copy(
            Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.bak.exe"),
            Path.Combine(fieldsOfMistriaLocation, "FieldsOfMistria.exe")
        );
    }

    private bool IsInstalled()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
        return Registry.LocalMachine.OpenSubKey(IFEORegistryKey)?.OpenSubKey("FieldsOfMistria.exe") is not null;
    }

    public List<string> GetPreinstallInformation(GeneratedInformation information)
    {
        return [];
    }

    public List<string> GetPreUninstallInformation()
    {
        if (IsInstalled()) return [Resources.CorePreinstallWillRemoveAurie];

        return [];
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