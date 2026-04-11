using System.Reflection;

namespace ModsOfMistriaInstallerLibTests.Utils;

public class FixtureHandler
{
    public static string GetFixturePath(string relativePath)
    {
        var currentPath = Assembly.GetExecutingAssembly().Location;
        var projectFile = "ModsOfMistriaInstallerLibTests.csproj";
        string finalPath = null;

        for (var i = 0; i < 7; i++)
        {
            currentPath = Path.GetDirectoryName(currentPath);
            if (currentPath is null) break;
            if (!Path.Exists(Path.Combine(currentPath, projectFile))) continue;
            
            finalPath = currentPath;
            break;
        }

        if (finalPath is null) 
            throw new Exception("Could not locate fixtures folder");
        
        return Path.Combine(finalPath, "Fixtures", relativePath);
    }
}