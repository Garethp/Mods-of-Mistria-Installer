namespace Garethp.ModsOfMistriaGUI.Models;

internal static class ExternalUrl
{
    public static bool IsAllowed(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps;
}
