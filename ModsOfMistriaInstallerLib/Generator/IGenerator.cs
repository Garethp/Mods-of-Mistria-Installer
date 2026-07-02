using Garethp.ModsOfMistriaInstallerLib.ModTypes;

namespace Garethp.ModsOfMistriaInstallerLib.Generator;

public interface IGenerator
{
    public GeneratedInformation Generate(IMod mod);
    
    public bool CanGenerate(IMod mod);
    
    public Validation Validate(IMod mod);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InformationGenerator(int manifestVersion) : Attribute
{
    public int ManifestVersion { get; } = manifestVersion;
}