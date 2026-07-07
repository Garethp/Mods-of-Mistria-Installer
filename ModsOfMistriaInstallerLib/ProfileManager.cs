using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

// Manages named profiles stored in {modsLocation}/momi_profiles.json.
// Each profile records which mods are enabled and the user's preferred install order.
public class ProfileManager
{
    private readonly string _path;
    private JObject _data;

    private const string DefaultProfileName = "Default";

    public ProfileManager(string modsLocation)
    {
        _path = Path.Combine(modsLocation, "momi_profiles.json");
        _data = Load();
    }

    // ── Profile list ─────────────────────────────────────────────────────────────

    public string CurrentProfileName
    {
        get => _data["currentProfile"]?.ToString() ?? DefaultProfileName;
        private set => _data["currentProfile"] = value;
    }

    public List<string> GetProfileNames()
    {
        var profiles = _data["profiles"] as JObject ?? [];
        return [.. profiles.Properties().Select(p => p.Name)];
    }

    // ── Profile data access ───────────────────────────────────────────────────────

    public (List<string> EnabledMods, List<string> LoadOrder) GetProfile(string name)
    {
        var profile = GetProfileObject(name);
        var enabled   = (profile["enabledMods"] as JArray ?? []).Select(t => t.ToString()).ToList();
        var loadOrder = (profile["loadOrder"] as JArray ?? []).Select(t => t.ToString()).ToList();
        return (enabled, loadOrder);
    }

    public (List<string> EnabledMods, List<string> LoadOrder) GetCurrentProfile() =>
        GetProfile(CurrentProfileName);

    // ── Mutating operations ───────────────────────────────────────────────────────

    public void SaveProfile(string name, List<string> enabledMods, List<string> loadOrder)
    {
        EnsureProfilesObject();
        var profiles = (JObject)_data["profiles"]!;
        profiles[name] = new JObject
        {
            ["enabledMods"] = new JArray(enabledMods),
            ["loadOrder"]   = new JArray(loadOrder),
        };
        Save();
    }

    public void SaveCurrentProfile(List<string> enabledMods, List<string> loadOrder) =>
        SaveProfile(CurrentProfileName, enabledMods, loadOrder);

    public void SwitchProfile(string name)
    {
        EnsureProfileExists(name);
        CurrentProfileName = name;
        Save();
    }

    public void CreateProfile(string name)
    {
        EnsureProfilesObject();
        var profiles = (JObject)_data["profiles"]!;
        if (!profiles.ContainsKey(name))
            profiles[name] = new JObject
            {
                ["enabledMods"] = new JArray(),
                ["loadOrder"]   = new JArray(),
            };
        Save();
    }

    public void DeleteProfile(string name)
    {
        if (name == DefaultProfileName) return; // can't delete default
        var profiles = _data["profiles"] as JObject;
        profiles?.Remove(name);
        if (CurrentProfileName == name)
            CurrentProfileName = DefaultProfileName;
        Save();
    }

    // ── Dependency resolution ─────────────────────────────────────────────────────

    // Given the user's requested enabled set, expand it to also include all transitive
    // requirements. Returns the expanded list (order preserved, new deps appended).
    public static List<string> ResolveEnabledWithDeps(List<IMod> allMods, List<string> requestedEnabled)
    {
        var byId = new Dictionary<string, IMod>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in allMods) byId.TryAdd(m.GetId(), m);
        var result  = new LinkedList<string>(requestedEnabled);
        var inSet   = new HashSet<string>(requestedEnabled, StringComparer.OrdinalIgnoreCase);
        var queue   = new Queue<string>(requestedEnabled);

        while (queue.TryDequeue(out var id))
        {
            if (!byId.TryGetValue(id, out var mod)) continue;
            foreach (var req in mod.GetRequirements())
            {
                var reqId = req.GetId();
                if (inSet.Add(reqId))
                {
                    result.AddFirst(reqId); // deps go before the mod that needs them
                    queue.Enqueue(reqId);
                }
            }
        }

        return [.. result];
    }

    // Sort allMods so that items in preferredOrder come first (in that order),
    // then remaining mods in their original order. Hard dependency ordering is
    // enforced: if A depends on B, B is moved before A regardless of user preference.
    public static List<IMod> SortByLoadOrder(List<IMod> allMods, List<string> preferredOrder)
    {
        if (preferredOrder.Count == 0) return allMods;

        var byId = new Dictionary<string, IMod>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in allMods) byId.TryAdd(m.GetId(), m);
        var result = new List<IMod>();
        var placed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Place(string id, HashSet<string> ancestors)
        {
            if (!byId.TryGetValue(id, out var mod)) return;
            if (placed.Contains(id)) return;
            if (!ancestors.Add(id)) return; // cycle — skip

            foreach (var req in mod.GetRequirements())
                Place(req.GetId(), [..ancestors]);

            if (placed.Add(id))
                result.Add(mod);
        }

        // First pass: place in preferred order (with deps pulled in before each)
        foreach (var id in preferredOrder)
            Place(id, []);

        // Second pass: any mods not yet placed
        foreach (var mod in allMods)
        {
            if (!placed.Contains(mod.GetId()))
                Place(mod.GetId(), []);
        }

        return result;
    }

    // ── Persistence ───────────────────────────────────────────────────────────────

    private void Save()
    {
        File.WriteAllText(_path, _data.ToString(Formatting.Indented));
    }

    private JObject Load()
    {
        if (!File.Exists(_path))
            return CreateDefault();

        try
        {
            var obj = JObject.Parse(File.ReadAllText(_path));
            EnsureDefaultProfile(obj);
            return obj;
        }
        catch
        {
            return CreateDefault();
        }
    }

    private static JObject CreateDefault()
    {
        var obj = new JObject
        {
            ["currentProfile"] = DefaultProfileName,
            ["profiles"] = new JObject
            {
                [DefaultProfileName] = new JObject
                {
                    ["enabledMods"] = new JArray(),
                    ["loadOrder"]   = new JArray(),
                }
            }
        };
        return obj;
    }

    private static void EnsureDefaultProfile(JObject data)
    {
        data["profiles"] ??= new JObject();
        var profiles = (JObject)data["profiles"]!;
        if (!profiles.ContainsKey(DefaultProfileName))
            profiles[DefaultProfileName] = new JObject
            {
                ["enabledMods"] = new JArray(),
                ["loadOrder"]   = new JArray(),
            };
    }

    private void EnsureProfilesObject()
    {
        _data["profiles"] ??= new JObject();
    }

    private void EnsureProfileExists(string name)
    {
        EnsureProfilesObject();
        var profiles = (JObject)_data["profiles"]!;
        if (!profiles.ContainsKey(name))
            profiles[name] = new JObject
            {
                ["enabledMods"] = new JArray(),
                ["loadOrder"]   = new JArray(),
            };
    }

    private JObject GetProfileObject(string name)
    {
        EnsureProfilesObject();
        var profiles = (JObject)_data["profiles"]!;
        if (profiles[name] is JObject existing) return existing;
        var fresh = new JObject
        {
            ["enabledMods"] = new JArray(),
            ["loadOrder"]   = new JArray(),
        };
        profiles[name] = fresh;
        return fresh;
    }
}
