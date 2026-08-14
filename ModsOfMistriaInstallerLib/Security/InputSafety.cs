using System.Net;

namespace Garethp.ModsOfMistriaInstallerLib.Security;

internal static class InputSafety
{
    public static string ResolveUnderRoot(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Mod path must be relative.");

        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Mod path escapes the mod directory.");

        return fullPath;
    }

    public static string AssetPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Asset path must be relative.");

        var normalized = relativePath.Replace('\\', '/');
        if (normalized.Split('/').Any(part => part is "" or "." or "..") || normalized.Contains(':'))
            throw new InvalidDataException("Asset path contains an unsafe segment.");

        return Path.Combine("assets", normalized.Replace('/', Path.DirectorySeparatorChar));
    }

    public static bool IsSafeExternalUri(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (IPAddress.TryParse(uri.Host, out var address))
            return IsPublic(address);

        try
        {
            return Dns.GetHostAddresses(uri.Host).All(IsPublic);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPublic(IPAddress address) =>
        !IPAddress.IsLoopback(address) &&
        !address.Equals(IPAddress.Any) &&
        !address.Equals(IPAddress.IPv6Any) &&
        !address.IsIPv6LinkLocal &&
        !address.IsIPv6SiteLocal &&
        !IsPrivateV4(address);

    private static bool IsPrivateV4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
            (bytes[0] == 10 ||
             (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
             (bytes[0] == 192 && bytes[1] == 168) ||
             (bytes[0] == 169 && bytes[1] == 254));
    }
}
