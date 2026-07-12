namespace Garethp.ModsOfMistriaInstallerLib.ModTypes;

public record GmlPatchDefinition(
    string Id,
    string Target,
    string Operation,
    string AnchorFile,
    string ContentFile,
    int ExpectedMatches = 1
);
