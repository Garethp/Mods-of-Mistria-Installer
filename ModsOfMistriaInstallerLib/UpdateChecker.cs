using System.Net.Http;
using System.Text.RegularExpressions;
using Garethp.ModsOfMistriaInstallerLib.ModTypes;
using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib;

public record UpdateInfo(string LatestVersion, string? DownloadUrl, bool IsNewer);

// Checks whether a newer version of a mod is available.
// Supports two sources:
//   • GitHub repo URL   → uses GitHub Releases API (latest release tag)
//   • Any other URL     → fetches JSON: { "version": "x.y.z", "download_url": "..." }
public static class UpdateChecker
{
    private static readonly HttpClient Http;

    static UpdateChecker()
    {
        Http = new HttpClient();
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("MOMI-Installer/1.0");
        Http.Timeout = TimeSpan.FromSeconds(10);
    }

    public static async Task<UpdateInfo?> CheckAsync(IMod mod, CancellationToken ct = default)
    {
        var updateUrl  = mod.GetUpdateUrl();
        var downloadUrl = mod.GetDownloadUrl();
        if (string.IsNullOrWhiteSpace(updateUrl)) return null;

        try
        {
            UpdateInfo? info;
            if (TryGetGitHubApiUrl(updateUrl, out var apiUrl))
                info = await CheckGitHub(apiUrl, downloadUrl, ct);
            else
                info = await CheckJson(updateUrl, downloadUrl, ct);

            if (info is null) return null;

            var isNewer = IsVersionNewer(mod.GetVersion(), info.LatestVersion);
            return info with { IsNewer = isNewer };
        }
        catch { return null; }
    }

    // ── Sources ───────────────────────────────────────────────────────────────────

    private static async Task<UpdateInfo?> CheckGitHub(string apiUrl, string? fallbackDownload, CancellationToken ct)
    {
        var response = await Http.GetStringAsync(apiUrl, ct);
        var json     = JObject.Parse(response);

        var tag         = json["tag_name"]?.ToString();
        var releaseUrl  = json["html_url"]?.ToString();
        if (string.IsNullOrWhiteSpace(tag)) return null;

        var version = tag.TrimStart('v', 'V');
        return new UpdateInfo(version, releaseUrl ?? fallbackDownload, false);
    }

    private static async Task<UpdateInfo?> CheckJson(string url, string? fallbackDownload, CancellationToken ct)
    {
        var response = await Http.GetStringAsync(url, ct);
        var json     = JObject.Parse(response);

        var version     = json["version"]?.ToString();
        var downloadUrl = json["download_url"]?.ToString() ?? fallbackDownload;
        if (string.IsNullOrWhiteSpace(version)) return null;

        return new UpdateInfo(version, downloadUrl, false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static readonly Regex GitHubRepoPattern =
        new(@"^https?://github\.com/([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+?)(?:\.git)?/?$",
            RegexOptions.IgnoreCase);

    private static bool TryGetGitHubApiUrl(string url, out string apiUrl)
    {
        var m = GitHubRepoPattern.Match(url);
        if (!m.Success) { apiUrl = ""; return false; }
        apiUrl = $"https://api.github.com/repos/{m.Groups[1].Value}/releases/latest";
        return true;
    }

    // Returns true when candidate is a higher version than installed.
    // Uses System.Version for dotted numeric versions; falls back to string compare.
    internal static bool IsVersionNewer(string installed, string candidate)
    {
        if (string.IsNullOrWhiteSpace(installed) || string.IsNullOrWhiteSpace(candidate))
            return false;

        if (TryParseVersion(candidate, out var cv) && TryParseVersion(installed, out var iv))
            return cv > iv;

        // Normalise and do a simple string compare as fallback
        return !string.Equals(
            installed.Trim().TrimStart('v', 'V'),
            candidate.Trim().TrimStart('v', 'V'),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseVersion(string s, out Version version)
    {
        // Ensure at least two components for System.Version
        var clean = s.Trim().TrimStart('v', 'V');
        if (!clean.Contains('.')) clean += ".0";
        return Version.TryParse(clean, out version!);
    }
}
