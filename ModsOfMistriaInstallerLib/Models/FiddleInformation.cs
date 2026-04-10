using Newtonsoft.Json.Linq;

namespace Garethp.ModsOfMistriaInstallerLib.Models;

public class FiddleInformation
{
    public readonly JObject FiddleObject;

    public readonly MergeArrayHandling MergeArrayHandling;

    public FiddleInformation(JObject fiddleObject, MergeArrayHandling mergeArrayHandling = MergeArrayHandling.Merge)
    {
        FiddleObject = fiddleObject;
        MergeArrayHandling = mergeArrayHandling;

        if (fiddleObject["__arrayMergeSetting"] is JValue arrayMergeSetting)
        {
            MergeArrayHandling = $"{arrayMergeSetting}" switch
            {
                "Add" => MergeArrayHandling.Concat,
                "Merge" => MergeArrayHandling.Merge,
                "Replace" => MergeArrayHandling.Replace,
                _ => throw new Exception($"Expected arrayMergeSetting {arrayMergeSetting}")
            };
            
            fiddleObject.Remove("__arrayMergeSetting");
        }
    }
}