using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils
{
    /*
    <summary>
        Represents the atlas and meta.toml pair found in assets/atlases.
    </summary>
    */
    public class Atlas
    {
        public int ATLAS_SIZE = 4096;
        /*
        <summary>
        </summary>
        */
        /*
        <summary>
            The type of the atlas, it's the same as the prefix of the atlas.
            AnimalsAtlas_0.png => Type = "Animals"
            NpcAtlas_3.png => Type = "Npc"
        </summary>
        */
        public readonly string Type = "";
        /*
        <summary>
            The how manyth atlas of a type it is.
            AnimalsAtlas_0.png => Number = 0
            AnimalsAtlas_1.png => Number = 1
            NpcAtlas_0.png => Number = 0
        </summary>
        */
        public readonly int Number;
        /*
        <summary>
            The absolute path to the meta.toml associated with the atlas image.
            The meta.toml holds information regarding the location and size of an image inside the atlas,
            as well as the associated ID of the image and the frame of an animation it represents.
            If an image is not an animation it will still be annotated as ID::0 (0th frame).
        </summary>
        */
        public readonly string MetaPath = "";
        /*
        <summary>
            The absolute path to the atlas image.
            The atlases seem to be grid packed?
        </summary>
        */
        public readonly string PngPath = "";
        /*
        <summary>
            I only do GetExistingAtlases once and save the atlases in here.
        </summary>
        */
        private static List<Atlas> atlases = new();

        /*
        <summary>
            The width of the atlas image.
        </summary>
        */
        public readonly int Width = 4096;

        /*
        <summary>
            The height of the atlas image.
        </summary>
        */
        public readonly int Height = 4096;

        private static string atlasDirectory = MistriaLocator.GetMistriaLocation() + "\\assets\\atlases";

        /*
        <summary>
            Returns the absolute path as a string to an atlases' .meta.toml file. This does not check if it actually exists or not.
        </summary>
        */
        public static string BuildAtlasMetaName(string type, int number)
        {
            return Path.Combine(atlasDirectory, type + "Atlas_" + number.ToString() + ".meta.toml");
        }
        /*
        <summary>
            Returns the absolute path as a string to an atlases' image (.png) file. This does not check if it actually exists or not.
        </summary>
        */
        public static string BuildAtlasPngName(string type, int number)
        {
            return Path.Combine(atlasDirectory, type + "Atlas_" + number.ToString() + ".png");
        }

        private bool CreateAtlasImageFile(string path, int width = 4096, int height = 4096)
        {
            var img = new Image<Rgba32>(width, height);
            try
            {
                img.Save(path);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        private bool CreateAtlasMetaTomlFile(string path, int width = 4096, int height = 4096)
        {
            var data = new TomlTable
            {
                ["meta_properties"] = new TomlTable
                {
                    ["id"] = IDManager.GenerateUniqueId(),
                    ["asset_kind"] = "TextureAtlas"
                },
                ["asset_properties"] = new TomlTable
                {
                    ["dimensions"] = new TomlArray { width, height },
                    ["filter_kind"] = "Nearest",
                    ["texture_wrap"] = "Repeat",
                    ["mipmap_filter_kind"] = "Nearest",
                    ["srgb"] = true,
                    ["animations"] = new TomlArray(),
                    ["atlas"] = this.Type,
                }
            };


            Toml.SaveToml(data, path);
            return true;
        }

        /*
        <summary>
            Creates a new image and meta.toml file if they dont already exist and registers them in the atlasDirectory List.
        </summary>
        */
        public Atlas(string type, int number, int width = 4096, int height = 4096)
        {
            Type = type;
            Number = number;
            // If the atlases has already been loaded.
            if (!atlases.Contains(this))
            {
                MetaPath = BuildAtlasMetaName(type, number);
                PngPath = BuildAtlasPngName(type, number);
                atlases.Add(this);
                Width = width;
                Height = height;
                // If either the image or its meta file doesn't exist we're already screwed. Might as well overwrite it?
                if (!File.Exists(PngPath) || !File.Exists(MetaPath))
                {
                    CreateAtlasImageFile(PngPath, Width, Height);
                    CreateAtlasMetaTomlFile(MetaPath, Width, Height);
                }
                else
                {
                    var info = Image.Identify(PngPath);
                    Width = info.Width;
                    Height = info.Height;
                }
            }
        }

        /*
        <summary>
            An atlas is uniquely identified by its type and number.
        </summary>
        */
        public override bool Equals(object? obj)
        {
            if (obj is not Atlas other)
                return false;

            return Type == other.Type &&
                   Number == other.Number;
        }
        /*
        <summary>
            An atlas is uniquely identified by its type and number. The hash should reflect this.
        </summary>
        */
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Number);
        }

        public static List<Atlas> GetExistingAtlases(bool reread = false)
        {
            if (atlases.Count != 0 && !reread)
            {
                return atlases;
            }
            foreach (var metaPath in Directory.GetFiles(atlasDirectory, "*.meta.toml"))
            {
                var data = TomlSerializer.Deserialize<TomlTable>(
                    File.ReadAllText(metaPath));


                var fileName = Path.GetFileNameWithoutExtension(metaPath);
                // DefaultAtlas_0.meta

                fileName = Path.GetFileNameWithoutExtension(fileName);
                // DefaultAtlas_0

                // expects: PrefixAtlas_N
                var parts = fileName.Split('_');
                if (parts.Length < 2)
                    continue;

                if (!int.TryParse(parts[^1], out int index))
                    continue;

                var prefix = parts[0].ToString().Replace("Atlas", "");

                // The image width and height will be identified in the constructor. I think it's fine having it there and not here.
                atlases.Add(new Atlas(
                    prefix,
                    index
                ));
            }

            atlases = atlases
                .OrderBy(a => a.Type)
                .ThenBy(a => a.Number)
                .ToList();

            return atlases;
        }
    }
}
