namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public interface IGenerator
{
    public GeneratedInformation Generate(Mod mod);
    
    public bool CanGenerate(Mod mod);
    
    public Validation Validate(Mod mod);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InformationGenerator(int manifestVersion) : Attribute
{
    public int ManifestVersion { get; } = manifestVersion;
}