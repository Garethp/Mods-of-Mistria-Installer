using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils
{
    public class Toml
    {
        public Toml() { }
        private static HashSet<string> manifest = new HashSet<string>();
        // TODO: Is it worth it to get the file from the zip instead? Would be somewhat slower, but that way absolutely nothing should actually be able to go wrong.
        public static void BackupToml(string path)
        {
            //TODO: Make a backup, if we haven't already done that. Idk how to do a "proper" install manifest. This'll have to do.
            if (!File.Exists(path.Replace("assets", "assets_backup")))
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(path.Replace("assets", "assets_backup"))!);
                File.Copy(path, path.Replace("assets", "assets_backup"));
            }
        }
        

        // TODO: Check if path actually exists beforehand and try catch.
        public static void SaveToml(TomlTable data, string path)
        {
            // Backup file if it's not been already
            bool backUp = manifest.Add(path);
            if (backUp && File.Exists(path))
                BackupToml(path);
            File.WriteAllText(path, TomlSerializer.Serialize(data));
            
        }


        // TODO: Check if path actually exists beforehand and try catch.
        public static TomlTable LoadToml(string path)
        {
            return TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path));
        }
    }
}
