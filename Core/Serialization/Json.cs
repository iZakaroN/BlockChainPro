using Newtonsoft.Json;
using System;
using System.Linq;

/// <summary>
/// Source: https://stackoverflow.com/questions/11829035/newton-soft-json-jsonserializersettings-for-object-with-property-as-byte-array
/// </summary>
namespace Core.Serialization
{
    public class BytesToHexConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var hex = serializer.Deserialize<string>(reader);
                if (!string.IsNullOrEmpty(hex))
                {
                    return Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
                }
            }
            return Enumerable.Empty<byte>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bytes = value as byte[];
            var @string = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
            serializer.Serialize(writer, @string);
        }
    }

    public sealed class HexStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(uint).Equals(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue($"0x{value:x16}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = reader.ReadAsString();
            if (str == null || !str.StartsWith("0x"))
                throw new JsonSerializationException();
            return Convert.ToUInt32(str);
        }
    }
}
