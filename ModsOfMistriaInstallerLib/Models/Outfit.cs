using Garethp.ModsOfMistriaInstallerLib.Generator;
using Garethp.ModsOfMistriaInstallerLib.Lang;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class Outfit
{
    public static Dictionary<string, List<string>> ValidSlots = new()
    {
        { "back", ["capes", "backpacks"] },
        { "facial_hair", ["facial_hair"] },
        { "top", ["dress", "robe", "top_misc", "suit", "long_sleeve", "sleeveless", "short_sleeve"] },
        { "eyes", ["eyes" ] },
        { "face_gear", ["face_accessory", "ear_accessory", "glasses"] },
        { "hair", ["medium_hair", "short_hair", "long_hair"] },
        { "head_gear", ["crown", "head_gear_misc", "hair_accessory", "hat", "helmet"] },
        { "bottom", ["pants", "bottom_misc", "shorts", "skirt"] },
        { "feet", ["boots", "feet_misc", "sandals", "shoes"] }
    };
    
    public string Name;

    public string Description;

    public string UiSlot;

    public bool DefaultUnlocked;

    public string UiSubCategory;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string LutFile;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string UiItem;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public string OutlineFile;

    [JsonProperty(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public Dictionary<string, string> AnimationFiles = new ();

    public Validation Validate(Validation validation, IMod mod, string file, string id)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            validation.AddError(mod, file, Resources.CoreErrorOutfitNoName);
        }
        
        if (string.IsNullOrWhiteSpace(Description))
        {
            validation.AddError(mod, file, Resources.CoreErrorOutfitNoDescription);
        }

        if (string.IsNullOrWhiteSpace(UiSlot))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorOutfitNoUiSlot, id));
        }
        else if (!ValidSlots.ContainsKey(UiSlot))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorOutfitUiSlotWrong, id, string.Join(", ", ValidSlots.Keys)));
        }

        if (string.IsNullOrWhiteSpace(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorOutfitNoSubCategory, id));
        } else if (!string.IsNullOrEmpty(UiSlot) && ValidSlots.ContainsKey(UiSlot) &&
                   !ValidSlots[UiSlot].Contains(UiSubCategory))
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorOutfitUiSubCategoryWrong, id, string.Join(", ", ValidSlots[UiSlot])));
        }

        if (ValidationTools.CheckSpriteFileExists(mod, $"Outfit {id}'s lutFile", LutFile) is { } lutError)
        {
            validation.AddError(mod, file, lutError);
        }

        if (ValidationTools.CheckSpriteFileExists(mod, $"Outfit {id}'s uiItem", UiItem) is { } uiItemError)
        {
            validation.AddError(mod, file, uiItemError);
        }

        if (ValidationTools.CheckSpriteFileExists(mod, $"Outfit {id}'s outlineFile", OutlineFile) is { } outlineError)
        {
            validation.AddError(mod, file, outlineError);
        }

        if (AnimationFiles.Count == 0)
        {
            validation.AddError(mod, file, string.Format(Resources.CoreErrorOutfitNoAnimation, id));
        }

        foreach (var animationType in AnimationFiles.Keys)
        {
            if (ValidationTools.CheckSpriteDirectoryExists(mod, $"Outfit {id}'s animation file {animationType}",
                    AnimationFiles[animationType]) is { } animationError)
            {
                validation.AddError(mod, file, animationError);
            }
        }

        return validation;
    }
}
