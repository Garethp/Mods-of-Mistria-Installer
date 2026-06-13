using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using System.Text;
using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Installer;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tomlyn;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Model;



namespace Garethp.ModsOfMistriaInstallerLib;

public class ModInstaller(string fieldsOfMistriaLocation, string modsLocation)
{
    private string fieldsOfMistriaLocationFolder = "";
    
    private const int ATLAS_SIZE = 4096;
    private sealed class AtlasState
    {
        public AtlasRef Current = null!;
        public TomlTable AtlasData = null!;
        public Image<Rgba32> AtlasImage = null!;
        public ShelfPacker Packer = null!;
        public TomlTableArray Animations = null!;
    }

    public static HashSet<string> allUsedIds = new HashSet<string>();
    private readonly List<List<string>> _filesToBackup =
    [
        ["__fiddle__.json"]
    ];

    public void ValidateMods(List<IMod> mods)
    {
        var desiredGenerators = GetGenerators();
        mods.ForEach(mod =>
        {
            desiredGenerators.ForEach(generator => { mod.GetValidation().Merge(generator.Validate(mod)); });
        });
    }

    public List<string> PreinstallInformation(List<IMod> mods)
    {
        var information = new List<string>();

        var generatedInformation = new GeneratedInformation();

        var desiredGenerators = GetGenerators();
        var desiredInstallers = GetInstallers();

        foreach (var mod in mods)
        {
            foreach (var generator in desiredGenerators.Where(generator => generator.CanGenerate(mod)))
            {
                generatedInformation.Merge(generator.Generate(mod));
            }
        }

        foreach (var installer in desiredInstallers)
        {
            if (installer is not IPreinstallInfo preinstallChecker) continue;

            information.AddRange(preinstallChecker.GetPreinstallInformation(generatedInformation));
        }

        return information;
    }

    public List<string> PreUninstallInformation()
    {
        var information = new List<string>();

        var desiredInstallers = GetInstallers();

        foreach (var installer in desiredInstallers)
        {
            if (installer is not IPreUninstallInfo preUninstallInfo) continue;

            information.AddRange(preUninstallInfo.GetPreUninstallInformation());
        }

        return information;
    }

    public static List<Tuple<string, string>> filenameIdMapping = new List<Tuple<string, string>>();

    private static string GenerateUniqueId(HashSet<string> existing)
    {
        while (true)
        {
            var id = Convert.ToHexString(
                SHA256.HashData(Guid.NewGuid().ToByteArray())
            )[..16].ToLowerInvariant();

            if (existing.Add(id))
                return id;
        }
    }

    private static TomlTable LoadToml(string path)
        => TomlSerializer.Deserialize<TomlTable>(File.ReadAllText(path));

    private static void SaveToml(TomlTable data, string path)
    {
        
        File.WriteAllText(path, TomlSerializer.Serialize(data));
    }

    public class AtlasRef
    {
        public string Prefix;
        public int Number;
        public string MetaPath = "";
        public string PngPath = "";
    }

    public static List<AtlasRef> GetExistingAtlases(string atlasDir)
    {
        var result = new List<AtlasRef>();

        foreach (var metaPath in Directory.GetFiles(atlasDir, "*.meta.toml"))
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
            

            result.Add(new AtlasRef
            {
                Prefix = prefix,
                Number = index,
                MetaPath = metaPath,
                PngPath = Path.Combine(
                    atlasDir,
                    $"{fileName}.png"
                )
            });
        }

        return result
            .OrderBy(a => a.Prefix)
            .ThenBy(a => a.Number)
            .ToList();
    }

    public static HashSet<string> CollectUsedIds(List<AtlasRef> atlases)
    {

        foreach (var atlas in atlases)
        {
            var data = LoadToml(atlas.MetaPath);

            if (!data.TryGetValue("asset_properties", out var assetObj))
                continue;

            var asset = (TomlTable)assetObj;

            if (!asset.TryGetValue("animations", out var animObj))
                continue;

            foreach (TomlTable anim in (TomlTableArray)animObj)
            {
                if (!anim.TryGetValue("texture_ids", out var texObj))
                    continue;

                foreach (var tex in (TomlArray)texObj)
                {
                    var str = tex.ToString();
                    var id = str.Split("::")[0];
                    allUsedIds.Add(id);
                }
            }
        }

        return allUsedIds;
    }

    public class ShelfPacker
    {
        private readonly int width;
        private readonly int height;
        private readonly List<(int x, int y, int w, int h)> used = new();

        public ShelfPacker(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void Add(int x, int y, int w, int h)
            => used.Add((x, y, w, h));

        public (int x, int y)? FindPosition(int w, int h)
        {
            if (used.Count == 0)
                return (1, 1);

            int x = 1;
            int y = 1;

            foreach (var (rx, ry, rw, rh) in used.OrderBy(r => r.y).ThenBy(r => r.x))
            {
                if (x + w <= width)
                {
                    bool overlap = used.Any(o =>
                        x < o.x + o.w &&
                        x + w > o.x &&
                        y < o.y + o.h &&
                        y + h > o.y);

                    if (!overlap)
                        return (x, y);
                }

                x = rx + rw;
                y = ry;
            }

            int maxY = used.Max(r => r.y + r.h);

            x = 1;
            y = maxY + 1;

            if (y + h > height)
                return null;

            return (x, y);
        }
    }

    public static AtlasRef CreateNewAtlas(string dir, string prefix, int number, HashSet<string> usedIds)
    {
        var pngPath = Path.Combine(dir, $"{prefix}Atlas_{number}.png");
        var metaPath = Path.Combine(dir, $"{prefix}Atlas_{number}.meta.toml");

        using (var img = new Image<Rgba32>(ATLAS_SIZE, ATLAS_SIZE))
            img.Save(pngPath);

        var data = new TomlTable
        {
            ["meta_properties"] = new TomlTable
            {
                ["id"] = GenerateUniqueId(usedIds),
                ["asset_kind"] = "TextureAtlas"
            },
            ["asset_properties"] = new TomlTable
            {
                ["dimensions"] = new TomlArray { ATLAS_SIZE, ATLAS_SIZE },
                ["filter_kind"] = "Nearest",
                ["texture_wrap"] = "Repeat",
                ["mipmap_filter_kind"] = "Nearest",
                ["srgb"] = true,
                ["animations"] = new TomlArray(),
                ["atlas"] = prefix,
            }
        };


        SaveToml(data, metaPath);

        return new AtlasRef
        {
            Number = number,
            MetaPath = metaPath,
            PngPath = pngPath
        };
    }

    private static ShelfPacker BuildPacker(TomlTable atlasData)
    {
        var packer = new ShelfPacker(
            ATLAS_SIZE,
            ATLAS_SIZE);

        if (!atlasData.TryGetValue("asset_properties", out var assetObj))
            return packer;

        if (assetObj is not TomlTable assetProperties)
            return packer;

        if (!assetProperties.TryGetValue("animations", out var animationsObj))
            return packer;

        if (animationsObj is not TomlTableArray animations)
            return packer;

        foreach (TomlTable animation in animations)
        {
            if (!animation.TryGetValue(
                    "top_left_dimensions",
                    out var dimensionsObj))
                continue;

            if (dimensionsObj is not TomlArray dimensions)
                continue;

            if (dimensions.Count < 4)
                continue;

            packer.Add(
                Convert.ToInt32(dimensions[0]),
                Convert.ToInt32(dimensions[1]),
                Convert.ToInt32(dimensions[2]),
                Convert.ToInt32(dimensions[3]));
        }

        return packer;
    }

    public static void ImportAnimation(
    string atlasDir,
    string importDir)
    {
        var atlases = GetExistingAtlases(atlasDir);

        var usedIds = CollectUsedIds(atlases);

        var atlasStates = new Dictionary<string, AtlasState>();

        foreach (var file in Directory.GetFiles(importDir, "*.meta.toml"))
        {
            var png = file.Replace(".meta.toml", ".png");

            if (!File.Exists(png))
                continue;

            var anim = LoadToml(file);
            if (!File.Exists(file.Replace("assets", "assets_backup")))
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(file.Replace("assets", "assets_backup"))!);
                File.WriteAllText(file.Replace("assets", "assets_backup"), TomlSerializer.Serialize(anim));
            }
            if (!anim.TryGetValue("asset_properties", out var assetObj))
                continue;

            var asset = (TomlTable)assetObj;

            if (!asset.TryGetValue("atlas", out var atlasNameObj))
                continue;

            var prefix = atlasNameObj.ToString();

            if (!atlasStates.TryGetValue(prefix, out var state))
            {
                var current = atlases
                    .Where(a => a.Prefix == prefix)
                    .OrderBy(a => a.Number)
                    .LastOrDefault();
                if (current != null)
                {
                    var atData = LoadToml(current.MetaPath);
                    if (!File.Exists(current.MetaPath.Replace("assets", "assets_backup")))
                    {
                        Directory.CreateDirectory(
                            Path.GetDirectoryName(current.MetaPath.Replace("assets", "assets_backup"))!);
                        File.WriteAllText(current.MetaPath.Replace("assets", "assets_backup"), TomlSerializer.Serialize(atData));
                        File.Copy(current.MetaPath.Replace(".meta.toml", ".png"), current.MetaPath.Replace(".meta.toml", ".png").Replace("assets", "assets_backup"), overwrite: true);
                    }
                }
                else
                {
                    current = CreateNewAtlas(
                        atlasDir,
                        prefix,
                        0,
                        usedIds);

                    atlases.Add(current);
                }

                var atlasData = LoadToml(current.MetaPath);
                var atlasImage = Image.Load<Rgba32>(current.PngPath);

                var animations =
                    (TomlTableArray)((TomlTable)atlasData["asset_properties"])["animations"];

                state = new AtlasState
                {
                    Current = current,
                    AtlasData = atlasData,
                    AtlasImage = atlasImage,
                    Packer = BuildPacker(atlasData),
                    Animations = animations
                };

                atlasStates[prefix] = state;
            }

            var frameSize = (TomlArray)asset["frame_size"];
            var frameLen = Convert.ToInt32(asset["frame_len"]);

            int fw = Convert.ToInt32(frameSize[0]);
            int fh = Convert.ToInt32(frameSize[1]);

            using var animImg = Image.Load<Rgba32>(png);

            var id = GenerateUniqueId(usedIds);

            ((TomlTable)anim["meta_properties"])["id"] = id;
            
            SaveToml(anim, file);
            var stripped = string.Join("_", file.Replace(".meta.toml", "").Split("_")[1..]);
            filenameIdMapping.Add(Tuple.Create(stripped, id));

            for (int i = 0; i < frameLen; i++)
            {
                var pos = state.Packer.FindPosition(fw, fh);

                if (pos == null)
                {
                    state.AtlasImage.Save(state.Current.PngPath);
                    SaveToml(
                        state.AtlasData,
                        state.Current.MetaPath);

                    state.AtlasImage.Dispose();

                    state.Current = CreateNewAtlas(
                        atlasDir,
                        prefix,
                        state.Current.Number + 1,
                        usedIds);

                    atlases.Add(state.Current);

                    state.AtlasData =
                        LoadToml(state.Current.MetaPath);

                    state.AtlasImage =
                        Image.Load<Rgba32>(state.Current.PngPath);

                    state.Packer =
                        BuildPacker(state.AtlasData);

                    state.Animations =
                        (TomlTableArray)((TomlTable)state.AtlasData["asset_properties"])["animations"];

                    pos = state.Packer.FindPosition(fw, fh);

                    if (pos == null)
                        throw new Exception(
                            $"Frame ({fw}x{fh}) does not fit into empty atlas.");
                }

                var (x, y) = pos.Value;

                using var frame = animImg.Clone(ctx =>
                    ctx.Crop(
                        new Rectangle(
                            i * fw,
                            0,
                            fw,
                            fh)));

                state.AtlasImage.Mutate(ctx =>
                    ctx.DrawImage(
                        frame,
                        new Point(x, y),
                        1f));

                state.Packer.Add(
                    x,
                    y,
                    fw,
                    fh);

                state.Animations.Add(
                    new TomlTable
                    {
                        ["texture_ids"] =
                            new TomlArray
                            {
                            $"{id}::{i}"
                            },

                        ["top_left_dimensions"] =
                            new TomlArray
                            {
                            x,
                            y,
                            fw,
                            fh
                            }
                    });
            }
        }

        foreach (var state in atlasStates.Values)
        {
            state.AtlasImage.Save(
                state.Current.PngPath);

            SaveToml(
                state.AtlasData,
                state.Current.MetaPath);

            state.AtlasImage.Dispose();
        }
    }

    public void InstallMods(List<IMod> mods, Action<string, string> reportStatus)
    {
        var totalTime = new Stopwatch();
        totalTime.Start();
        if (!Directory.Exists(fieldsOfMistriaLocation))
        {
            throw new DirectoryNotFoundException(Resources.CoreMistriaLocationDoesNotExist);
        }

        if (IsFreshInstall())
        {
            var path = fieldsOfMistriaLocation + "\\assets.zip";
            if (!File.Exists(path)) return;
            reportStatus("Fresh install, unzipping assets.zip. This will take some time.", "");
            ZipFile.ExtractToDirectory(path, fieldsOfMistriaLocation);
            System.IO.File.Move(Path.Combine(fieldsOfMistriaLocation, "assets.zip"), Path.Combine(fieldsOfMistriaLocation,"assets_backup.zip"));
            reportStatus("Assets have been unzipped.", "");
            return;
        }


        var timer = new Stopwatch();
        foreach (var mod in mods)
        {
            Uninstall();
            string installStatus = "Installing " + mod.GetName() + " " + mod.GetVersion() + " by " + mod.GetAuthor();
            string installTimeTaken = "Time taken to install " + mod.GetName() + ": ";
            reportStatus(installStatus , "");
            //Thread.Sleep(1000);
            timer.Start();
            Install(fieldsOfMistriaLocation , mod.GetLocation(), reportStatus);
            //Thread.Sleep(1000);
            reportStatus(installTimeTaken + timer.ToString(), "");
            //Thread.Sleep(1000);
            timer.Restart();
            timer.Stop();
        }

        totalTime.Stop();

        reportStatus(Resources.CoreInstallCompleted, totalTime.ToString());
    }

    private bool IsFreshInstall()
    {
        return !Directory.Exists(Path.Combine(fieldsOfMistriaLocation, "assets"));
    }

    private static List<string> GetDirectoryPrefixes(string root)
    {
        var dirs = Directory
            .GetDirectories(root, "*", SearchOption.AllDirectories)
            .ToList();

        var result = new HashSet<string>();

        foreach (var dir in dirs)
        {
            var current = dir;

            while (current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(current);
                current = Path.GetDirectoryName(current);

                if (string.IsNullOrEmpty(current))
                    break;
            }
        }

        return result.ToList();
    }

    private void Install(string fieldsOfMistriaLocation, string modsLocation, Action<string, string> reportStatus)
    {

        string assetsLocation = fieldsOfMistriaLocation + "\\assets";



        reportStatus(assetsLocation + "\\atlases", "");
        //Thread.Sleep(2000);
        foreach (var dir in GetDirectoryPrefixes(modsLocation))
        {
            ImportAnimation(assetsLocation + "\\atlases", dir);
        }
        foreach (var file in Directory.EnumerateFiles(
                     modsLocation,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(
                modsLocation,
                file);

            var destination = Path.Combine(
                assetsLocation,
                relativePath);

            Directory.CreateDirectory(
                Path.GetDirectoryName(destination)!);


            if (File.Exists(destination) &&
                Path.GetExtension(file)
                .Equals(".toml", StringComparison.OrdinalIgnoreCase))
            {
                var sourceToml =
                    TomlSerializer.Deserialize<TomlTable>(
                        File.ReadAllText(file));
                if (Path.GetExtension(file)
                    .Equals(".meta.toml", StringComparison.OrdinalIgnoreCase))
                {
                    if (!sourceToml.TryGetValue("meta_properties", out var metaObj) ||
                        metaObj is not TomlTable metaProperties)
                    {
                        metaProperties = new TomlTable();
                        sourceToml["meta_properties"] = metaProperties;
                    }

                    var fileName = Path.GetFileName(file);

                    var nameWithoutExtension =
                        fileName.Replace(".meta.toml", "", StringComparison.OrdinalIgnoreCase);

                    var split = nameWithoutExtension.Split('_');

                    var prefix = split.Length > 0
                        ? split[0]
                        : "";

                    var lookupName = split.Length > 1
                        ? string.Join("_", split[1..])
                        : nameWithoutExtension;

                    var existingMapping = filenameIdMapping
                        .FirstOrDefault(x =>
                            string.Equals(
                                x.Item1,
                                lookupName,
                                StringComparison.OrdinalIgnoreCase));

                    if (string.Equals(prefix, "poly", StringComparison.OrdinalIgnoreCase))
                    {
                        var polyId = GenerateUniqueId(allUsedIds);

                        metaProperties["id"] = polyId;

                        if (existingMapping != null)
                        {
                            metaProperties["required_assets"] =
                                new TomlArray
                                {
                                    existingMapping.Item2
                                };
                        }
                    }
                    else
                    {
                        string assetId;

                        if (existingMapping != null)
                        {
                            assetId = existingMapping.Item2;
                        }
                        else
                        {
                            assetId = GenerateUniqueId(allUsedIds);

                            filenameIdMapping.Add(
                                Tuple.Create(
                                    lookupName,
                                    assetId));
                        }

                        metaProperties["id"] = assetId;
                    }
                }

                var destinationToml =
                    TomlSerializer.Deserialize<TomlTable>(
                        File.ReadAllText(destination));

                //TODO: ApplyMomiOperations(destinationToml, sourceToml);

                string tomdestination = MergeTomlFile(
                    sourceToml,
                    destinationToml,
                    destination);

                reportStatus(
                    "Toml merge to: " + tomdestination,
                    "");
            }
            else
            {
                File.Copy(
                    file,
                    destination,
                    overwrite: true);

                reportStatus(
                    "File copied normally.",
                    "");
            }
        }
    }

    private static string MergeTomlFile(
    TomlTable sourceToml,
    TomlTable destinationToml,
    string destinationFile)
    {

        if (!File.Exists(destinationFile.Replace("assets", "assets_backup")))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destinationFile.Replace("assets", "assets_backup"))!);
            File.WriteAllText(destinationFile.Replace("assets", "assets_backup"), TomlSerializer.Serialize(destinationToml));
        }
        MergeTables(destinationToml, sourceToml);

        File.WriteAllText(
            destinationFile,
            TomlSerializer.Serialize(destinationToml));

        return destinationFile;
    }

    private static string MergeTomlFile(
        string sourceFile,
        string destinationFile)
    {
        if (!File.Exists(destinationFile))
        {
            File.Copy(
                sourceFile,
                destinationFile);

            return destinationFile;
        }
        
        var sourceToml =
            TomlSerializer.Deserialize<TomlTable>(
                File.ReadAllText(sourceFile));

        var destinationToml =
            TomlSerializer.Deserialize<TomlTable>(
                File.ReadAllText(destinationFile));

        MergeTables(
            destinationToml,
            sourceToml);
        if (File.Exists(destinationFile))
            File.WriteAllText(destinationFile, TomlSerializer.Serialize(destinationToml));
        File.WriteAllText(destinationFile,TomlSerializer.Serialize(destinationToml));
        return destinationFile;
    }

    private static void MergeTables(
        TomlTable destination,
        TomlTable source)
    {
        foreach (var (key, value) in source)
        {
            if (value is TomlTable sourceTable)
            {
                destination[key] = CloneTable(sourceTable);
            }
            else
            {
                destination[key] = value;
            }
        }
    }

    private static TomlTable CloneTable(
        TomlTable table)
    {
        var clone = new TomlTable();

        foreach (var (key, value) in table)
        {
            clone[key] = value switch
            {
                TomlTable child => CloneTable(child),
                _ => value
            };
        }

        return clone;
    }

    private List<IGenerator> GetGenerators()
    {
        return (from app in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
            from type in app.GetTypes()
            where type.GetInterface(nameof(IGenerator)) is not null && !type.IsAbstract
            let attributes = type.GetCustomAttributes(typeof(InformationGenerator), true)
            where attributes is { Length: > 0 } &&
                  attributes.Any(attribute => (InformationGenerator)attribute is { ManifestVersion: 1 })
            select Activator.CreateInstance(type) as IGenerator).ToList();
    }

    private List<IModuleInstaller> GetInstallers()
    {
        return (from app in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
            from type in app.GetTypes()
            where type.GetInterface(nameof(IModuleInstaller)) is not null && !type.IsAbstract
            let attributes = type.GetCustomAttributes(typeof(InformationInstaller), true)
            where attributes is { Length: > 0 } &&
                  attributes.Any(attribute => (InformationInstaller)attribute is { ManifestVersion: 1 })
            select Activator.CreateInstance(type) as IModuleInstaller).ToList();
    }

    public void Uninstall()
    {
        string sourceFolder = Path.Combine(
            fieldsOfMistriaLocation,
            "assets_backup");

        string destinationFolder = Path.Combine(
            fieldsOfMistriaLocation,
            "assets");

        foreach (var directory in Directory.GetDirectories(
                     sourceFolder,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(
                sourceFolder,
                directory);

            Directory.CreateDirectory(
                Path.Combine(
                    destinationFolder,
                    relativePath));
        }

        foreach (var file in Directory.GetFiles(
                     sourceFolder,
                     "*",
                     SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(
                sourceFolder,
                file);

            var destinationFile = Path.Combine(
                destinationFolder,
                relativePath);

            Directory.CreateDirectory(
                Path.GetDirectoryName(destinationFile)!);

            File.Move(
                file,
                destinationFile,
                overwrite: true);
        }
        using var zip = ZipFile.OpenRead(fieldsOfMistriaLocation + "\\assets_backup.zip");

        var zipFiles = new HashSet<string>(
            zip.Entries
                .Where(e => !string.IsNullOrEmpty(e.Name))
                .Select(e =>
                {
                    var path = e.FullName.Replace('/', Path.DirectorySeparatorChar);

                    if (path.StartsWith("assets" + Path.DirectorySeparatorChar))
                    {
                        path = path.Substring(("assets" + Path.DirectorySeparatorChar).Length);
                    }

                    return path;
                }),
            StringComparer.OrdinalIgnoreCase);

        string assetsFolder = fieldsOfMistriaLocation + "\\assets";
        var assetFiles = Directory.GetFiles(
            assetsFolder,
            "*",
            SearchOption.AllDirectories);

        foreach (var file in assetFiles)
        {
            var relative = Path.GetRelativePath(assetsFolder, file);

            if (!zipFiles.Contains(relative))
            {
                File.Delete(file);
            }
        }
    }
}