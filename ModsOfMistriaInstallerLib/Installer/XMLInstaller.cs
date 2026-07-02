using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Garethp.ModsOfMistriaInstallerLib.Utils;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Garethp.ModsOfMistriaInstallerLib.Installer;


// Parses each json file in the points folder, and for each object in the files:
//    1. Finds the map file that corresponds to the object name (this should correspond to the name of a room with a .tmx file)
//    2. Finds the TrellisPoints object group in the .tmx file (creating it if it doesn't exist yet)
//    3. Inserts the trellis point, replacing any property placeholders with the ones defined in the json file
public class XMLInstaller(
    string fomLocation,
    InstallManifest manifest,
    Dictionary<string, string> fileNameUidMapping,
    IFileModifier fileModifier)
    : Installer(fomLocation, manifest, fileNameUidMapping)
{
    private const string PointsFolder = "points";
    private const string IndentUnit = " ";
    private const string GroupIndent = IndentUnit;
    private const string ObjectIndent = GroupIndent + IndentUnit;

    public override void Install(IMod mod, Action<string, string> reportStatus)
    {
        if (!mod.FolderExists(PointsFolder))
            return;

        var tiledDir = DestinationPath("tiled");

        var jsonFilePaths = mod
            .GetFilesInFolder(PointsFolder)
            .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (jsonFilePaths.Count == 0)
            throw new FileNotFoundException($"No .json files found in the mod's '{PointsFolder}' folder.");

        foreach (var jsonFilePath in jsonFilePaths)
        {
            var jsonText = mod.ReadFile(jsonFilePath);

            using var document = JsonDocument.Parse(jsonText);

            foreach (var mapEntry in document.RootElement.EnumerateObject())
            {
                InsertPointsForMap(mapEntry, tiledDir, reportStatus);
            }
        }
    }

    private void InsertPointsForMap(JsonProperty mapEntry, string tiledDir, Action<string, string> reportStatus)
    {
        string mapName = mapEntry.Name;
        List<JsonProperty> pointEntries = mapEntry.Value.EnumerateObject().ToList();
        if (pointEntries.Count == 0)
            return;

        string tmxPath = FindMapFile(tiledDir, mapName);
        string tmxText = fileModifier.Read(tmxPath);
        string newline = tmxText.Contains("\r\n") ? "\r\n" : "\n";
        string templatesMetaDir = Path.Combine(tiledDir, "templates", "meta");
        string tmxDir = Path.GetDirectoryName(tmxPath)!;
        string relative = Path.GetRelativePath(tmxDir, templatesMetaDir).Replace(Path.DirectorySeparatorChar, '/');

        Match groupOpenMatch = Regex.Match(tmxText, "<objectgroup\\b[^>]*\\bname=\"TrellisPoints\"[^>]*>");
        bool groupExists = groupOpenMatch.Success;

        int nextId = FindHighestId(tmxText) + 1;

        int newGroupId = 0;
        if (!groupExists)
        {
            newGroupId = nextId;
            nextId++;
        }

        var objectBlocks = new List<string>();
        foreach (var pointEntry in pointEntries)
        {
            objectBlocks.Add(BuildObjectXml(pointEntry.Name, pointEntry.Value, nextId, relative, newline));
            nextId++;
        }

        string updatedTmxText = InsertObjectsIntoGroup(
            tmxText, groupExists, groupOpenMatch, newGroupId, objectBlocks, newline);

        Dirty(tmxPath);
        // @TODO: Check if we need the UTF8 encoding or not
        fileModifier.Write(tmxPath, updatedTmxText);
        // File.WriteAllText(tmxPath, updatedTmxText, new System.Text.UTF8Encoding(false));

        reportStatus(mapName, $"Inserted {objectBlocks.Count} trellis point(s) into '{mapName}'");
    }

    // Finds the .tmx file in the tiled folder
    private string FindMapFile(string tiledDir, string mapName)
    {
        var matches = fileModifier.FileFiles(tiledDir, "rm_" + mapName + ".tmx");

        if (matches.Length == 0)
            throw new FileNotFoundException($"No .tmx file found for '{mapName}'.");

        return matches[0];
    }

    // Scans the .tmx file to find the current highest ID used
    private static int FindHighestId(string tmxText)
    {
        int highest = 0;
        foreach (Match match in Regex.Matches(tmxText, "\\bid=\"(\\d+)\""))
        {
            int value = int.Parse(match.Groups[1].Value);
            if (value > highest)
                highest = value;
        }
        return highest;
    }

    // Builds the TrellisPoint object to match the formatting of the tmx file.
    // NOTE: Currently only takes into account the properties for "direction" and "prop_name"
    // May need to add additional ones if they are relevant.
    private static string BuildObjectXml(string pointName, JsonElement point, int id, string relative, string newline)
    {
        string pointType = point.GetProperty("point_type").GetString()
            ?? throw new InvalidOperationException($"Missing or null 'point_type' for point '{pointName}'.");
        JsonElement[] position = point.GetProperty("position").EnumerateArray().ToArray();

        string x = position[0].GetRawText();
        string y = position[1].GetRawText();

        bool hasDirection = point.TryGetProperty("direction", out JsonElement directionElement);
        bool hasPropName = point.TryGetProperty("prop_name", out JsonElement propNameElement);

        string template = $"{relative}/obj_trellis_point_{pointType}.tx";
        string type = $"obj_trellis_point_{pointType}";
        string openTagAttributes = $"id=\"{id}\" template=\"{template}\" name=\"{pointName}\" type=\"{type}\" x=\"{x}\" y=\"{y}\"";

        if (!hasDirection && !hasPropName)
            return $"{ObjectIndent}<object {openTagAttributes}/>";

        string propertiesIndent = ObjectIndent + IndentUnit;
        string propertyIndent = propertiesIndent + IndentUnit;

        var lines = new List<string>
        {
            $"{ObjectIndent}<object {openTagAttributes}>",
            $"{propertiesIndent}<properties>"
        };

        if (hasDirection)
        {
            int direction = directionElement.GetInt32();
            lines.Add($"{propertyIndent}<property name=\"direction\" type=\"int\" propertytype=\"Cardinal\" value=\"{direction}\"/>");
        }

        if (hasPropName)
        {
            string propName = propNameElement.GetString()
                ?? throw new InvalidOperationException($"Missing or null 'prop_name' for point '{pointName}'.");
            lines.Add($"{propertyIndent}<property name=\"prop_name\" value=\"{propName}\"/>");
        }

        lines.Add($"{propertiesIndent}</properties>");
        lines.Add($"{ObjectIndent}</object>");

        return string.Join(newline, lines);
    }

    
    // Adds the built objects into the TrellisPoints group as the last objects in the group.
    // If no TrellisPoints group exists in the map, creates it
    private static string InsertObjectsIntoGroup(
        string tmxText,
        bool groupExists,
        Match groupOpenMatch,
        int newGroupId,
        List<string> objectBlocks,
        string newline)
    {
        string insertedObjects = string.Join(newline, objectBlocks);

        if (!groupExists)
        {
            int closeMapIndex = tmxText.IndexOf("</map>", StringComparison.Ordinal);
            if (closeMapIndex == -1)
                throw new InvalidOperationException("Could not find a closing </map> tag.");

            int closeMapLineStart = GetLineStart(tmxText, closeMapIndex);

            var groupLines = new List<string>
            {
                $"{GroupIndent}<objectgroup draworder=\"index\" id=\"{newGroupId}\" name=\"TrellisPoints\" visible=\"0\">",
                insertedObjects,
                $"{GroupIndent}</objectgroup>"
            };

            string newGroupBlock = string.Join(newline, groupLines);
            return tmxText.Insert(closeMapLineStart, newGroupBlock + newline);
        }

        string matchedTag = groupOpenMatch.Value;

        if (matchedTag.EndsWith("/>"))
        {
            string openTag = matchedTag.Substring(0, matchedTag.LastIndexOf("/>")) + ">";
            string replacement = openTag + newline + insertedObjects + newline + GroupIndent + "</objectgroup>";
            return tmxText.Remove(groupOpenMatch.Index, matchedTag.Length).Insert(groupOpenMatch.Index, replacement);
        }

        int searchStart = groupOpenMatch.Index + matchedTag.Length;
        int closeIndex = tmxText.IndexOf("</objectgroup>", searchStart, StringComparison.Ordinal);

        int closeLineStart = GetLineStart(tmxText, closeIndex);
        return tmxText.Insert(closeLineStart, insertedObjects + newline);
    }

    private static int GetLineStart(string tmxText, int index)
    {
        int searchFrom = Math.Max(0, index - 1);
        int newlineIndex = tmxText.LastIndexOf('\n', searchFrom);
        return newlineIndex + 1;
    }
}