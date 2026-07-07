using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using System.Security.Cryptography;

namespace Garethp.ModsOfMistriaInstallerLib.Installers
{
    public class Installer
    {
        Dictionary<string, string> fileNameUIDMapping = new Dictionary<string, string>();

        private static readonly HashSet<string> allUsedIds = new();

        string FieldsOfMistriaLocation = "";

        public Installer(string fieldsOfMistriaLocation) { this.FieldsOfMistriaLocation = fieldsOfMistriaLocation; }
        public bool Dirty(string path) {
            return true;
        }
        public bool Restore()
        {
            return true;
        }
        public string GenerateUID(string path)
        {
            // Have I already generated a UID for this file?
            if (fileNameUIDMapping.ContainsKey(path))
            {
                return fileNameUIDMapping[path];
            }
            // Otherwise generate a new String ID that hasn't been used.
            while (true)
            {
                var id = Convert.ToHexString(
                    SHA256.HashData(Guid.NewGuid().ToByteArray())
                )[..16].ToLowerInvariant();

                if (allUsedIds.Add(id))
                    return id;
            }
        }
    }
}
