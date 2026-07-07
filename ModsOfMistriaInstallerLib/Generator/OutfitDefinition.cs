using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

// Parsed from a single momi/outfit/*.toml file.
// Each [section] in the file defines one outfit; the section name becomes the ID.
// A single file can contain multiple outfits.
public class OutfitDefinition
{
    public string Id            { get; init; } = "";
    public string Name          { get; init; } = "";
    public string UiSlot        { get; init; } = "";
    public string UiSubCategory { get; init; } = "";
    public bool   DefaultUnlocked { get; init; } = true;

    // Optional sprite overrides — derived from Id + UiSlot by default.
    // "lut" and "lut_sprite" are aliases; "lut" wins if both are set.
    public string? OutfitSprite   { get; init; }
    public string? LutSprite      { get; init; }
    public string? IconSprite     { get; init; }
    public string? OutlineSprite  { get; init; }

    // Optional frame size overrides — fall back to SlotConfig defaults if absent.
    public int? FrameWidth  { get; init; }
    public int? FrameHeight { get; init; }

    // Optional — written to player_assets.toml when present.
    public int? PriceOverride { get; init; }

    // Resolved sprite names
    public string ResolvedOutfitSprite  => OutfitSprite  ?? $"spr_player_{Id}_{UiSlot}";
    public string ResolvedLutSprite     => LutSprite     ?? $"spr_player_{Id}_lut";
    public string ResolvedIconSprite    => IconSprite    ?? $"spr_ui_item_wearable_{Id}";
    public string ResolvedOutlineSprite => OutlineSprite ?? $"spr_ui_item_wearable_{Id}_outline";

    // Parses all outfit definitions from a TOML file.
    // Each top-level [section] defines one outfit; the key becomes the ID.
    public static Dictionary<string, OutfitDefinition> ParseAll(string tomlContent)
    {
        var result = new Dictionary<string, OutfitDefinition>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var root = Tomlyn.TomlSerializer.Deserialize<TomlTable>(tomlContent);
            foreach (var (key, value) in root)
            {
                if (value is not TomlTable section) continue;
                var def = ParseSection(key, section);
                if (def is not null && !string.IsNullOrEmpty(def.UiSlot))
                    result[key] = def;
            }
        }
        catch { }
        return result;
    }

    private static OutfitDefinition? ParseSection(string id, TomlTable t)
    {
        try
        {
            // Short aliases win over long forms when both are present
            string? outfitSprite = null;
            if (t.TryGetValue("outfit",        out var oa)) outfitSprite = oa?.ToString();
            if (t.TryGetValue("outfit_sprite", out var ob) && outfitSprite is null) outfitSprite = ob?.ToString();

            string? lutSprite = null;
            if (t.TryGetValue("lut",           out var lv)) lutSprite = lv?.ToString();
            if (t.TryGetValue("lut_sprite",    out var ls) && lutSprite is null) lutSprite = ls?.ToString();

            string? iconSprite = null;
            if (t.TryGetValue("icon",          out var ia)) iconSprite = ia?.ToString();
            if (t.TryGetValue("icon_sprite",   out var ib) && iconSprite is null) iconSprite = ib?.ToString();

            string? outlineSprite = null;
            if (t.TryGetValue("outline",         out var oa2)) outlineSprite = oa2?.ToString();
            if (t.TryGetValue("outline_sprite",  out var ob2) && outlineSprite is null) outlineSprite = ob2?.ToString();

            int? priceOverride = null;
            if (t.TryGetValue("price_override", out var pv) && pv is not null)
                priceOverride = Convert.ToInt32(pv);

            int? frameWidth = null;
            int? frameHeight = null;

            // frame_size = [w, h] wins over the individual frame_width / frame_height fields
            if (t.TryGetValue("frame_size", out var fsObj) && fsObj is TomlArray fs && fs.Count >= 2)
            {
                frameWidth  = Convert.ToInt32(fs[0]);
                frameHeight = Convert.ToInt32(fs[1]);
            }
            else
            {
                if (t.TryGetValue("frame_width",  out var fw) && fw is not null) frameWidth  = Convert.ToInt32(fw);
                if (t.TryGetValue("frame_height", out var fh) && fh is not null) frameHeight = Convert.ToInt32(fh);
            }

            return new OutfitDefinition
            {
                Id              = id,
                Name            = t.TryGetValue("name",             out var nm) ? nm?.ToString() ?? "" : "",
                UiSlot          = t.TryGetValue("ui_slot",          out var sl) ? sl?.ToString() ?? "" : "",
                UiSubCategory   = t.TryGetValue("ui_sub_category",  out var sc) ? sc?.ToString() ?? "" : "",
                DefaultUnlocked = t.TryGetValue("default_unlocked", out var du) && du is bool b && b,
                OutfitSprite    = outfitSprite,
                LutSprite       = lutSprite,
                IconSprite      = iconSprite,
                OutlineSprite   = outlineSprite,
                PriceOverride   = priceOverride,
                FrameWidth      = frameWidth,
                FrameHeight     = frameHeight,
            };
        }
        catch
        {
            return null;
        }
    }
}
