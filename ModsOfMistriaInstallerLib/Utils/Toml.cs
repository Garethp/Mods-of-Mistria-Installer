using Tomlyn;
using Tomlyn.Model;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

public static class Toml
{
    public static TomlTable ParseToml(string content) =>
        TomlSerializer.Deserialize<TomlTable>(content);
    
    public static bool Compare(object aObject, object bObject)
    {
        // This is to account for the behavior of `[TomlSingleOrArray]`
        if (aObject is TomlArray aArray1 && bObject is not TomlArray && aArray1.Count == 1)
            return Compare(aArray1.First(), bObject);
        if (bObject is TomlArray bArray1 && aObject is not TomlArray && bArray1.Count == 1)
            return Compare(aObject, bArray1.First());
        
        switch (aObject)
        {
            case TomlTable aTable:
            {
                if (bObject is not TomlTable bTable)
                {
                    return false;
                }

                if (aTable.Count != bTable.Count)
                {
                    return false;
                }

                foreach (var key in aTable.Keys)
                {
                    if (!bTable.ContainsKey(key))
                        return false;

                    if (!Compare(aTable[key], bTable[key]))
                    {
                        return false;
                    }
                }

                break;
            }
            case string aString:
            {
                if (bObject is not string bString)
                    return false;

                return aString == bString;
            }
            case TomlArray aArray:
            {
                if (bObject is not TomlArray bArray)
                    return false;

                if (aArray.Count != bArray.Count)
                    return false;
                
                for (var i = 0; i < aArray.Count; i++)
                {
                    if (!Compare(aArray[i], bArray[i]))
                        return false;
                }

                return true;
            }
            case long aLong:
                if (bObject is not long bLong)
                    return false;

                return aLong == bLong;
            case double aDouble:
                if (bObject is not double bDouble)
                    return false;
                
                return aDouble == bDouble;
            default:
                return false;
        }

        return true;
    }
}
