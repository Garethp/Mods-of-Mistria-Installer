using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Parsed from a single momi/furniture/*.toml file.
// Each [section] defines one furniture item; the section name becomes the item and object ID.
// Required fields: sprite, frame_size, size.
public class FurnitureDefinition
{
    public string Id          { get; init; } = "";
    public string Name        { get; init; } = "";
    public string Description { get; init; } = "";

    // Full sprite base name: spr_furniture_<...> (without _spring/_mask/_shadow suffix).
    public string Sprite      { get; init; } = "";

    // frame_size == dimensions (always 1 frame for furniture).
    public int FrameWidth     { get; init; }
    public int FrameHeight    { get; init; }

    // Animation meta sprite anchor offset. Default [0.0, 0.0].
    public double OffsetH     { get; init; } = 0.0;
    public double OffsetV     { get; init; } = 0.0;

    // Object prototype south.offset (pixel render offset). Optional — defaults to OffsetH/V as int.
    public int? SouthOffsetX  { get; init; }
    public int? SouthOffsetY  { get; init; }
    public int ResolvedSouthOffsetX => SouthOffsetX ?? (int)OffsetH;
    public int ResolvedSouthOffsetY => SouthOffsetY ?? (int)OffsetV;

    // Object grid size in tiles. Required.
    public int[]  Size        { get; init; } = [2, 2];

    // Shape meta fields for spring/mask/shadow. Default: offset [0,0], size = frame_size.
    public int[]? ShapeOffset { get; init; }   // e.g. [-16, -46]
    public int[]? ShapeSize   { get; init; }   // e.g. [33, 39]

    // Optional object prototype fields.
    public string? RuleGrid    { get; init; }
    public TomlArray? WindowTiles { get; init; }

    // Optional formation check: array of [dx, dy] grid-cell offsets from this piece's
    // top-left that must also be occupied by the same object type before any interaction fires.
    // Example: [[0,0],[2,0],[4,0]] requires three pieces in a horizontal row.
    public TomlArray? Formation { get; init; }

    // Optional item definition fields.
    public int?   StoreValue  { get; init; }
    public string? RecipeKey  { get; init; }
    public TomlArray? Tags    { get; init; }
    public int?   CraftingLevelRequirement { get; init; }
    public TomlArray? Recipe  { get; init; }

    // Tracks which compact file this came from (used for the item set filename).
    public string SourceFileName { get; init; } = "generated";

    // ── Derived sprite names ──────────────────────────────────────────────────

    // "spr_furniture_..." → "furniture_..." (strips the spr_ prefix)
    public string SpriteBase    => Sprite.StartsWith("spr_", StringComparison.OrdinalIgnoreCase) ? Sprite[4..] : Sprite;
    public string SpringSprite  => $"{Sprite}_spring";
    public string MaskSprite    => $"{Sprite}_mask";
    public string ShadowSprite  => $"{Sprite}_shadow";
    public string IconSprite    => $"spr_ui_item_{SpriteBase}";
    public string OutlineSprite => $"spr_ui_item_{SpriteBase}_outline";

    // ── Parsing ───────────────────────────────────────────────────────────────

    public static Dictionary<string, FurnitureDefinition> ParseAll(string tomlContent, string sourceFileName)
    {
        var result = new Dictionary<string, FurnitureDefinition>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var root = Tomlyn.TomlSerializer.Deserialize<TomlTable>(tomlContent);
            foreach (var (key, value) in root)
            {
                if (value is not TomlTable section) continue;
                var def = ParseSection(key, section, sourceFileName);
                if (def is not null)
                    result[key] = def;
            }
        }
        catch { }
        return result;
    }

    private static FurnitureDefinition? ParseSection(string id, TomlTable t, string sourceFileName)
    {
        try
        {
            if (!t.TryGetValue("sprite", out var spriteObj) || spriteObj is null)
                return null;

            if (!t.TryGetValue("frame_size", out var fsObj) || fsObj is not TomlArray fs || fs.Count < 2)
                return null;

            if (!t.TryGetValue("size", out var sizeObj) || sizeObj is not TomlArray sa || sa.Count < 2)
                return null;

            double offsetH = 0.0, offsetV = 0.0;
            if (t.TryGetValue("offset", out var offObj) && offObj is TomlArray off && off.Count >= 2)
            {
                offsetH = Convert.ToDouble(off[0]);
                offsetV = Convert.ToDouble(off[1]);
            }

            int? storeValue = null;
            if (t.TryGetValue("value", out var valObj) && valObj is not null)
                storeValue = Convert.ToInt32(valObj);

            return new FurnitureDefinition
            {
                Id          = id,
                Name        = t.TryGetValue("name",        out var nm)   ? nm?.ToString()   ?? "" : "",
                Description = t.TryGetValue("description", out var desc) ? desc?.ToString() ?? "" : "",
                Sprite      = spriteObj.ToString()!,
                FrameWidth  = Convert.ToInt32(fs[0]),
                FrameHeight = Convert.ToInt32(fs[1]),
                OffsetH     = offsetH,
                OffsetV     = offsetV,
                Size        = [Convert.ToInt32(sa[0]), Convert.ToInt32(sa[1])],
                ShapeOffset  = t.TryGetValue("shape_offset", out var so) && so is TomlArray soArr && soArr.Count >= 2
                    ? [Convert.ToInt32(soArr[0]), Convert.ToInt32(soArr[1])] : null,
                ShapeSize    = t.TryGetValue("shape_size", out var ss) && ss is TomlArray ssArr && ssArr.Count >= 2
                    ? [Convert.ToInt32(ssArr[0]), Convert.ToInt32(ssArr[1])] : null,
                SouthOffsetX = t.TryGetValue("south_offset", out var southArr) && southArr is TomlArray southArrT && southArrT.Count >= 2
                    ? Convert.ToInt32(southArrT[0]) : null,
                SouthOffsetY = southArr is TomlArray southArrY && southArrY.Count >= 2
                    ? Convert.ToInt32(southArrY[1]) : null,
                RuleGrid    = t.TryGetValue("rule_grid", out var rg)  ? rg?.ToString()  : null,
                WindowTiles = t.TryGetValue("window_tiles", out var wt) && wt is TomlArray wtArr ? wtArr : null,
                StoreValue  = storeValue,
                RecipeKey   = t.TryGetValue("recipe_key", out var rk) ? rk?.ToString()  : null,
                Tags        = t.TryGetValue("tags",       out var tg)  && tg is TomlArray ta ? ta : null,
                CraftingLevelRequirement =
                    t.TryGetValue("crafting_level_requirement", out var cl) && cl is not null
                        ? Convert.ToInt32(cl) : null,
                Recipe      = t.TryGetValue("recipe", out var rec) && rec is TomlArray ra ? ra : null,
                Formation   = t.TryGetValue("formation", out var form) && form is TomlArray formArr ? formArr : null,
                SourceFileName = sourceFileName,
            };
        }
        catch
        {
            return null;
        }
    }
}
