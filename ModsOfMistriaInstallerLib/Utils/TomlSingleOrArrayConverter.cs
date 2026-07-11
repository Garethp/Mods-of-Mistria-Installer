using System.Runtime.CompilerServices;
using Tomlyn.Serialization;

namespace Garethp.ModsOfMistriaInstallerLib.Utils;

// Fields of Mistria has some fields (SpriteMetaFileAssetProperties::Duration) that can be either a single value or a
// list however if we then write that single value back into a list with a single value to FoM it'll cause errors reading
// them, so we need a custom converter to make sure that if a single value came in, a single value will go back out.
public class TomlSingleOrArrayConverter: TomlConverter
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(double)) return true;
        if (typeToConvert == typeof(List<double>)) return true;
        
        return false;
    }

    public override object? Read(TomlReader reader, Type typeToConvert)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.Float:
                return new List<double>()
                {
                    reader.GetDouble()
                };
            case TomlTokenType.StartArray:
                reader.Read();

                var list = new List<double>();
                while (reader.TokenType != TomlTokenType.EndArray)
                {
                    if (reader.TokenType != TomlTokenType.Float)
                        throw new Exception($"Unexpected token {reader.TokenType}");
                    
                    list.Add(reader.GetDouble());
                    reader.Read();
                }

                reader.Read();
                return list;
            default:
                throw new Exception($"Unexpected token {reader.TokenType}");
        }
    }

    public override void Write(TomlWriter writer, object? value)
    {
        if (value is not List<double> list)
        {
            return;
        }

        if (list.Count == 1)
        {
            writer.WriteFloatValue(list.First());
            return;
        }
        
        writer.WriteStartArray();
        list.ForEach(writer.WriteFloatValue);
        writer.WriteEndArray();
    }
}