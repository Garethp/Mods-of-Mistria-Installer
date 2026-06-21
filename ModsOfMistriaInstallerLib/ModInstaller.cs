using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tomlyn;
using Tomlyn.Model;
using System.Text.Json;
using System.Text.Json.Nodes;



namespace Garethp.ModsOfMistriaInstallerLib;

public class ModInstaller(string fieldsOfMistriaLocation, string modsLocation)
{
    private string fieldsOfMistriaLocationFolder = "";
    
    private const int ATLAS_SIZE = 4096;
    private sealed class AtlasState
    {
        public Atlas Current = null!;
        public TomlTable AtlasData = null!;
        public Image<Rgba32> AtlasImage = null!;
        public ShelfPacker Packer = null!;
        public TomlTableArray Animations = null!;
    }

    

    public static List<Tuple<string, string>> filenameIdMapping = new List<Tuple<string, string>>();

   

   
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
        var atlases = Atlas.GetExistingAtlases();

        var atlasStates = new Dictionary<string, AtlasState>();

        foreach (var file in Directory.GetFiles(importDir, "*.meta.toml"))
        {
            var png = file.Replace(".meta.toml", ".png");

            if (!File.Exists(png))
                continue;

            var anim = Toml.LoadToml(file);
            if (!anim.TryGetValue("asset_properties", out var assetObj))
                continue;

            var asset = (TomlTable)assetObj;

            if (!asset.TryGetValue("atlas", out var atlasNameObj))
                continue;

            var prefix = atlasNameObj.ToString();

            if (!atlasStates.TryGetValue(prefix, out var state))
            {
                var current = atlases
                    .Where(a => a.Type == prefix)
                    .OrderBy(a => a.Number)
                    .LastOrDefault();
                if (current != null)
                {
                    var atData = Toml.LoadToml(current.MetaPath);
                    /*if (!File.Exists(current.MetaPath.Replace("assets", "assets_backup")))
                    {
                        Directory.CreateDirectory(
                            Path.GetDirectoryName(current.MetaPath.Replace("assets", "assets_backup"))!);
                        File.WriteAllText(current.MetaPath.Replace("assets", "assets_backup"), TomlSerializer.Serialize(atData));
                        File.Copy(current.MetaPath.Replace(".meta.toml", ".png"), current.MetaPath.Replace(".meta.toml", ".png").Replace("assets", "assets_backup"), overwrite: true);
                    }*/
                }
                else
                {
                    current = new Atlas(prefix, 0);

                    atlases.Add(current);
                }

                var atlasData = Toml.LoadToml(current.MetaPath);
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

            var id = IDManager.GenerateUniqueId();

            ((TomlTable)anim["meta_properties"])["id"] = id;
            
            Toml.SaveToml(anim, file);
            var stripped = string.Join("_", file.Replace(".meta.toml", "").Split("_")[1..]);
            filenameIdMapping.Add(Tuple.Create(stripped, id));

            for (int i = 0; i < frameLen; i++)
            {
                var pos = state.Packer.FindPosition(fw, fh);

                if (pos == null)
                {
                    state.AtlasImage.Save(state.Current.PngPath);
                    Toml.SaveToml(
                        state.AtlasData,
                        state.Current.MetaPath);

                    state.AtlasImage.Dispose();

                    state.Current = new Atlas(
                        prefix,
                        state.Current.Number + 1
                        );

                    atlases.Add(state.Current);

                    state.AtlasData =
                        Toml.LoadToml(state.Current.MetaPath);

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

            Toml.SaveToml(
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
        Uninstall();
        foreach (var mod in mods)
        {
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


    private static void MergeJsonFile(
        string sourceFile,
        string destinationFile)
    {
        

        JsonNode? source =
            JsonNode.Parse(File.ReadAllText(sourceFile));

        JsonNode? destination =
            File.Exists(destinationFile)
                ? JsonNode.Parse(File.ReadAllText(destinationFile))
                : null;

        if (source is null)
            throw new InvalidOperationException(
                $"Failed to parse source json '{sourceFile}'.");

        if (destination is JsonObject destinationObject &&
            source is JsonObject sourceObject)
        {
            MergeObjects(destinationObject, sourceObject);

            File.WriteAllText(
                destinationFile,
                sourceObject.ToJsonString(
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
        }
        else
        {
            File.Copy(sourceFile, destinationFile, true);
        }
    }
    private static void MergeObjects(
    JsonObject destination,
    JsonObject source)
    {
        foreach (var property in destination)
        {
            if (property.Value is null)
                continue;

            if (!source.TryGetPropertyValue(
                    property.Key,
                    out var sourceValue))
            {
                source[property.Key] = property.Value.DeepClone();
                continue;
            }

            switch (property.Value)
            {
                case JsonObject destinationObject
                    when sourceValue is JsonObject sourceObject:

                    MergeObjects(
                        destinationObject,
                        sourceObject);
                    break;

                case JsonArray destinationArray
                    when sourceValue is JsonArray sourceArray:

                    MergeArrays(
                        destinationArray,
                        sourceArray);
                    break;

                default:

                    source[property.Key] =
                        property.Value.DeepClone();
                    break;
            }
        }
    }

    private static void MergeArrays(
    JsonArray destination,
    JsonArray source)
    {
        foreach (var item in destination)
        {
            source.Add(item?.DeepClone());
        }
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
                        var polyId = IDManager.GenerateUniqueId();

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
                            assetId = IDManager.GenerateUniqueId();

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

                //ApplyMomiOperations(destinationToml, sourceToml);

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
                if (File.Exists(destination) && !File.Exists(destination.Replace("assets", "assets_backup")))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(destination.Replace("assets", "assets_backup"))))
                        Directory.CreateDirectory(Path.GetDirectoryName(destination.Replace("assets", "assets_backup")));
                    File.Copy(destination, destination.Replace("assets","assets_backup"));
                }
                //MergeJsonFile(file, destination);

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

    private const string IdentifyKey = "MOMIidentify";

    private static void MergeTableArrays(
    TomlTableArray destination,
    TomlTableArray source)
    {
        foreach (var sourceTable in source)
        {
            if (sourceTable.TryGetValue("MOMIidentify", out var identifyObj) &&
                identifyObj is TomlTable identifyTable)
            {
                var match = destination.FirstOrDefault(x => MatchesIdentifier(x, identifyTable));

                if (match != null)
                {
                    var action = sourceTable.TryGetValue("MOMIaction", out var act)
                        ? act?.ToString()
                        : "merge";

                    if (action == "remove" || action == "delete")
                    {
                        destination.Remove(match);
                        continue;
                    }

                    var copy = CloneTable(sourceTable);
                    copy.Remove("MOMIidentify");
                    copy.Remove("MOMIaction");

                    MergeTables(match, copy);
                    continue;
                }
            }

            destination.Add(CloneTable(sourceTable));
        }
    }

    private static bool MatchesIdentifier(
    TomlTable candidate,
    TomlTable identifier)
    {
        foreach (var (key, expectedValue) in identifier)
        {
            if (!candidate.TryGetValue(key, out var actualValue))
                return false;

            if (!TomlValuesEqual(actualValue, expectedValue))
                return false;
        }

        return true;
    }

    private static bool TomlValuesEqual(
    object? left,
    object? right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        if (left is TomlTable leftTable &&
            right is TomlTable rightTable)
        {
            return MatchesIdentifier(leftTable, rightTable) &&
                   MatchesIdentifier(rightTable, leftTable);
        }

        return Equals(left, right);
    }

    private static void MergeTables(
    TomlTable destination,
    TomlTable source)
    {
        foreach (var (key, sourceValue) in source)
        {
            // 1. Handle REMOVE TABLE
            if (sourceValue is TomlTable sourceTable)
        {
            // 1. REMOVE
            if (IsRemoveCommand(sourceTable))
            {
                destination.Remove(key);
                continue;
            }

            // 2. MERGE COMMAND (custom behavior override)
            if (IsMergeCommand(sourceTable))
            {
                if (destination.TryGetValue(key, out var destValue) &&
                    destValue is TomlTable destTable)
                {
                    var copy = CloneTable(sourceTable);

                    copy.Remove("MOMIidentify");
                    copy.Remove("MOMIaction");

                    MergeTables(destTable, copy);
                }

                continue;
            }
        }

            if (!destination.TryGetValue(key, out var destinationValue))
            {
                destination[key] = CloneValue(sourceValue);
                continue;
            }

            if (sourceValue is TomlTable sourceTable2 &&
                destinationValue is TomlTable destinationTable)
            {
                MergeTables(destinationTable, sourceTable2);
            }
            else if (sourceValue is TomlTableArray sourceArray &&
                     destinationValue is TomlTableArray destinationArray)
            {
                MergeTableArrays(destinationArray, sourceArray);
            }
            else
            {
                destination[key] = CloneValue(sourceValue);
            }
        }
    }

    private static bool IsRemoveCommand(TomlTable table)
    {
        if (table.Count == 0)
            return false;

        if (table.TryGetValue("MOMIaction", out var action) &&
            action?.ToString() == "remove")
            return true;

        if (table.TryGetValue("MOMI", out var momi) &&
            momi is TomlTable inner &&
            inner.TryGetValue("op", out var op) &&
            op?.ToString() == "remove")
            return true;

        return false;
    }

    private static bool IsMergeCommand(TomlTable table)
    {
        if (table.Count == 0)
            return false;

        if (table.TryGetValue("MOMIaction", out var action) &&
            action?.ToString() == "merge")
            return true;

        if (table.TryGetValue("MOMI", out var momi) &&
            momi is TomlTable inner &&
            inner.TryGetValue("op", out var op) &&
            op?.ToString() == "merge")
            return true;

        return false;
    }

    private static TomlTable CloneTable(
    TomlTable table)
    {
        var clone = new TomlTable();

        foreach (var (key, value) in table)
        {
            clone[key] = CloneValue(value);
        }

        return clone;
    }
    private static object? CloneValue(object? value)
    {
        return value switch
        {
            TomlTable table => CloneTable(table),
            _ => value
        };
    }


    public void Uninstall()
    {
        string sourceFolder = Path.Combine(
            fieldsOfMistriaLocation,
            "assets_backup");

        string destinationFolder = Path.Combine(
            fieldsOfMistriaLocation,
            "assets");
        Directory.CreateDirectory(sourceFolder);
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